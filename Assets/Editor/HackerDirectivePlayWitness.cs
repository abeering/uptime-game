using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class HackerDirectivePlayWitness
    {
        private const string Pending = "Uptime.HackerDirectivePlayWitness";
        static HackerDirectivePlayWitness() => EditorApplication.update += Launch;
        public static void Run()
        {
            EditorSceneManager.OpenScene(IntegritySceneAuthoring.Scene);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void Launch()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Directive Play Mode witness").AddComponent<HackerDirectivePlayRunner>();
        }
    }
    public sealed class HackerDirectivePlayRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            int exit = 0;
            try { Check(); }
            catch (Exception error) { Debug.LogException(error); exit = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(exit);
            else EditorApplication.isPlaying = false;
        }
        private void Check()
        {
            var host = FindFirstObjectByType<SimulationHost>();
            var traffic = host.GetComponent<ConnectionTrafficDirector>();
            traffic.RandomizeSeed = false; traffic.Seed = 1701; traffic.AutoGenerationEnabled = true;
            host.enabled = false; // Fixed-step real simulation in Play Mode; no wall-clock stepping interleaves.
            var directive = host.GetComponent<HackerDirectives>();
            directive.ConfigureForDevelopment(HackerDirective.BREACH);
            host.ResetSimulation(); host.Recorder.BeginRun();
            string Advance()
            {
                for (int i = 0; i < 2250; i++) host.StepSimulation(.02f);
                return string.Join("\n", host.Recorder.CurrentLog.events.Where(e => e.type.StartsWith("hacker_", StringComparison.Ordinal))
                    .Select(e => JsonUtility.ToJson(e)));
            }
            string first = Advance();
            if (!directive.Snapshot.hasOffensive || first.Length == 0) throw new InvalidOperationException("No BREACH offensive in Play Mode.");
            var terminal = FindFirstObjectByType<Terminal>();
            var input = (TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
            input.onSubmit.Invoke("inspect web");
            var connection = host.RuntimeConnections.FirstOrDefault(c => c.IsActive || c.IsConnecting);
            if (connection != null) input.onSubmit.Invoke("inspect -v " + connection.ConnectionId);
            string visible = string.Join("\n", FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text));
            if (visible.Contains("BREACH") || visible.Contains("offensive_target") || visible.Contains("remainingCostAtSelection"))
                throw new InvalidOperationException("Hidden directive information reached active player text.");
            input.onSubmit.Invoke("restart");
            if (directive.Directive != HackerDirective.BREACH || directive.Snapshot.hasOffensive || host.RunEnded ||
                host.Integrity.Nodes.Any(n => !n.External && n.Value != 2))
                throw new InvalidOperationException("Terminal restart did not reset directive/Integrity state.");
            if (first != Advance()) throw new InvalidOperationException("Play Mode terminal restart changed seeded offensive decisions.");
            Directory.CreateDirectory(HackerDirectiveWitness.Evidence);
            const string report = "PASS actual Play Mode: BREACH 45s fixed-step generation; active node/connection inspect text contains no directive intent; terminal restart clears offensive and Integrity; exact seeded offensive/target-event replay.\nHuman keyboard focus and layout review not automated.\n";
            File.WriteAllText(HackerDirectiveWitness.Evidence + "/play-mode-checks.txt", report);
            Debug.Log("HackerDirectivePlayWitness " + report);
        }
    }
}
