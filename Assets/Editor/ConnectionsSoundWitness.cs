using System;
using System.Collections;
using System.Reflection;
using TMPro;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class ConnectionsSoundWitness
    {
        private const string Pending = "ConnectionsSoundWitness.pending";
        private static int frames;
        static ConnectionsSoundWitness() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Connections/Check Sound Bindings")]
        public static void CheckBindings()
        {
            foreach (string scene in new[] { "Connections", "ConnectionsAdvanced" })
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
                var sound = UnityEngine.Object.FindFirstObjectByType<ConnectionsSoundController>();
                var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
                var terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
                var messages = UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>();
                var call = UnityEngine.Object.FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
                Check(sound != null && sound.gameObject == host.gameObject &&
                    Field<SimulationHost>(sound, "simulationHost") == host &&
                    Field<Terminal>(sound, "terminal") == terminal &&
                    Field<TextCommunicationDirector>(sound, "textCommunications") == messages &&
                    Field<ConnectionsVideoCall>(sound, "videoCall") == call,
                    scene + " scene event references");
                Check(AssetDatabase.GetAssetPath(Field<AudioSource>(sound, "sourcePrefab")) ==
                    "Assets/Prefabs/SoundEffect.prefab", scene + " pooled source prefab");
                CheckCue(sound, "wormStatus", "Assets/Audio/Connections/worm_status.wav");
                CheckCue(sound, "statusPortscan", "Assets/Audio/Connections/status_portscan.wav");
                CheckCue(sound, "interceptSuccess", "Assets/Audio/Connections/intercept_success.wav");
                CheckCue(sound, "chatNotification", "Assets/Audio/SFX/chat_notification.wav");
                CheckCue(sound, "commandError", "Assets/Audio/SFX/command_error.wav");
                CheckCue(sound, "nodeInfected", "Assets/Audio/SFX/node_infected_2.wav");
                CheckCue(sound, "packetSpawn", "Assets/Audio/SFX/packet_spawn.wav");
                CheckCue(sound, "scanCompleteBenign", "Assets/Audio/SFX/scan_complete_benign.wav");
                CheckCue(sound, "probeSweepingLoop", "Assets/Audio/SFX/probe_sweeping_loop.wav");
                CheckCue(sound, "videoCallOpen", "Assets/Audio/SFX/video_call_open.wav");
            }
            Debug.Log("ConnectionsSoundWitness BINDINGS PASS");
        }

        private static void CheckCue(ConnectionsSoundController sound, string field, string path)
        {
            AudioCue cue = Field<AudioCue>(sound, field);
            Check(cue != null && cue.clips != null && cue.clips.Length == 1 &&
                AssetDatabase.GetAssetPath(cue.clips[0]) == path, field + " binding");
        }

        internal static T Field<T>(object owner, string name) => (T)owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(owner);
        internal static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("ConnectionsSoundWitness: " + name);
            Debug.Log("SOUND CHECK: " + name);
        }

        [MenuItem("Tools/Connections/Run Sound Witness (Play Mode)")]
        public static void Run()
        {
            CheckBindings();
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-witnessScene");
            string scene = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Connections";
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            SessionState.SetBool(Pending, true);
            frames = 0;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !Application.isPlaying || ++frames < 30)
                return;
            SessionState.SetBool(Pending, false);
            new GameObject("Connections sound checks").AddComponent<ConnectionsSoundChecks>().Begin();
        }
    }

    public sealed class ConnectionsSoundChecks : MonoBehaviour
    {
        private ConnectionsSoundController sound;
        private SimulationHost host;
        private Terminal terminal;
        private TextCommunicationDirector messages;
        private ConnectionsVideoCall call;
        private AudioSource[] sources;
        private AudioSource probe;
        private int commandErrors;
        private double deadline;

        public void Begin()
        {
            deadline = EditorApplication.timeSinceStartup + 45;
            StartCoroutine(Run());
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup > deadline)
                Finish(new TimeoutException("Connections sound witness timeout"));
        }

        private IEnumerator Run()
        {
            Exception failure = null;
            try
            {
                sound = FindFirstObjectByType<ConnectionsSoundController>();
                host = FindFirstObjectByType<SimulationHost>();
                terminal = FindFirstObjectByType<Terminal>();
                messages = FindFirstObjectByType<TextCommunicationDirector>();
                call = FindFirstObjectByType<ConnectionsVideoCall>(FindObjectsInactive.Include);
                sources = ConnectionsSoundWitness.Field<AudioSource[]>(sound, "sources");
                probe = ConnectionsSoundWitness.Field<AudioSource>(sound, "probeSource");
                ConnectionsSoundWitness.Check(sources?.Length == 8 && probe != null &&
                    !probe.playOnAwake && probe.loop && probe.spatialBlend == 0f,
                    "scene-owned 2D sources and dedicated Probe loop");
                terminal.CommandFailed += CountError;
                var traffic = FindFirstObjectByType<ConnectionTrafficDirector>();
                traffic.AutoGenerationEnabled = false;
                traffic.HostileGenerationEnabled = false;
                host.SetRunMode(ConnectionRunMode.Generated);
                int before = Before();
                ConnectionSpawnPlan arrival = traffic.QueueProfile("basic-threat");
                host.RegisterConnection(LogicalConnectionDefinition.FromPlan(arrival));
                Expect("packetSpawn", before, "registration once");
                before = Before();
                host.ApplyCondition(host.FindNodeResources("DB"),
                    new ConditionPayload(ConditionKind.PortScanned, 5f),
                    sourceWormBranchId: "w1");
                Expect("wormStatus", before, "Worm PortScanned only worm cue");
                before = Before();
                host.ApplyCondition(host.FindNodeResources("DB"),
                    new ConditionPayload(ConditionKind.PortScanned, 5f),
                    sourceConnectionId: "c1");
                Expect("statusPortscan", before, "recon refresh only portscan cue");
                before = Before();
                host.ApplyCondition(host.FindNodeResources("DB"),
                    new ConditionPayload(ConditionKind.ScanJammed, 5f),
                    sourceWormBranchId: "w2");
                Expect("wormStatus", before, "Worm jam cue");
                host.ResetSimulation();
                before = Before();
                ConnectionsSoundWitness.Check(host.StartScan("DB", "scan db", out _) ==
                    ScanStartResult.Started, "clean scan starts");
                host.StepSimulation(host.ScanDurationSeconds + 0.01f);
                Expect("scanCompleteBenign", before, "clean scan completion cue");
                ConnectionsSoundWitness.Check(host.TryInjectInfection("DB", InfectionKind.PhantomLoad,
                    2f, out _), "infection applied for scan witness");
                before = Before();
                ConnectionsSoundWitness.Check(host.StartScan("DB", "scan db", out _) ==
                    ScanStartResult.Started, "infected scan starts");
                host.StepSimulation(host.ScanDurationSeconds + 0.01f);
                Expect("nodeInfected", before, "infected scan completion cue");
                before = Before();
                var message = new TextCommunication { Sender = "Sound witness", Body = "Delivered." };
                ConnectionsSoundWitness.Check(messages.PushMessage(message), "message delivered");
                Expect("chatNotification", before, "delivered message cue");
                before = Before();
                messages.PushMessage(message);
                ConnectionsSoundWitness.Check(Next() == before, "message burst cooldown");
                before = Before();
                terminal.GetComponentInChildren<TMP_InputField>(true).onSubmit.Invoke(
                    "kill no-such-id no-such-id");
                Expect("commandError", before, "failed multi-target command cue");
                ConnectionsSoundWitness.Check(commandErrors == 1, "one error event per command");
                terminal.GetComponentInChildren<TMP_InputField>(true).onSubmit.Invoke("help");
                ConnectionsSoundWitness.Check(commandErrors == 1, "successful command silent");
                before = Before();
                ConnectionsSoundWitness.Check(call.TryStartSampleCall(), "sample call opens");
                Expect("videoCallOpen", before, "visible call opening cue");
                before = Before();
                ConnectionsSoundWitness.Check(!call.TryStartSampleCall() && Next() == before,
                    "duplicate call silent");
                call.EndCall();
                host.ResetSimulation();
                ConnectionsSoundWitness.Check(host.TryActivateProbe(
                    host.PhysicalLinks[0].View.LinkId, "probe", out _), "Probe activates");
                InvokeReconcile();
                ConnectionsSoundWitness.Check(probe.clip == Cue("probeSweepingLoop") && probe.loop,
                    "Probe uses dedicated loop");
                host.StepSimulation(host.ProbeActiveSeconds + 0.01f);
                InvokeReconcile();
                ConnectionsSoundWitness.Check(!probe.isPlaying, "Probe loop stops on expiry");
                host.ResetSimulation();
                ConnectionsSoundWitness.Check(host.TryActivateProbe(
                    host.PhysicalLinks[0].View.LinkId, "probe", out _), "Probe reactivates");
                InvokeReconcile();
                host.ResetSimulation();
                ConnectionsSoundWitness.Check(!probe.isPlaying, "Probe loop stops on reset");
                sound.enabled = false;
                ConnectionsSoundWitness.Check(!probe.isPlaying && Array.TrueForAll(sources,
                    source => !source.isPlaying), "disable stops every source");
            }
            catch (Exception error) { failure = error; }
            Finish(failure);
            yield break;
        }

        private void CountError() => commandErrors++;
        private int Before()
        {
            foreach (AudioSource source in sources) source.Stop();
            return Next();
        }
        private int Next() => ConnectionsSoundWitness.Field<int>(sound, "nextSource");
        private AudioClip Cue(string field) => ConnectionsSoundWitness.Field<AudioCue>(sound, field).clips[0];
        private void Expect(string field, int before, string name)
        {
            int next = Next();
            ConnectionsSoundWitness.Check(next == (before + 1) % sources.Length &&
                sources[before].clip == Cue(field), name);
        }
        private void InvokeReconcile() => typeof(ConnectionsSoundController)
            .GetMethod("ReconcileProbe", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(sound, null);
        private void Finish(Exception error)
        {
            enabled = false;
            if (terminal != null) terminal.CommandFailed -= CountError;
            if (error == null) Debug.Log("ConnectionsSoundWitness PLAY PASS");
            else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
