using System;
using System.Collections.Generic;
using System.Linq;
using Uptime.Connections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad]
    public static class CompanyFeedbackWitness
    {
        private const string PendingKey = "CompanyFeedbackWitness.pending";
        private static int frames;
        static CompanyFeedbackWitness() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Connections/Run Company Feedback Witness (Play Mode)")]
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-witnessScene");
            string scene = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Connections";
            EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            SessionState.SetBool(PendingKey, true);
            frames = 0;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !Application.isPlaying || ++frames < 10) return;
            SessionState.SetBool(PendingKey, false);
            Exception failure = null;
            try { Cases(); }
            catch (Exception exception) { failure = exception; Debug.LogException(exception); }
            if (failure == null) Debug.Log("CompanyFeedbackWitness PLAY PASS: " +
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            if (Application.isBatchMode) EditorApplication.Exit(failure == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("CompanyFeedbackWitness: " + name);
            Debug.Log("COMPANY FEEDBACK CHECK: " + name);
        }

        private static LogicalConnectionState Add(SimulationHost host, string id,
            string profile, bool hostile, float work = 1000f)
        {
            NodeView ext = host.Nodes.First(n => n.View.NodeId == "EXT1").View;
            NodeView api = host.Nodes.First(n => n.View.NodeId == "API").View;
            return host.RegisterConnection(LogicalConnectionDefinition.FromPlan(
                new ConnectionSpawnPlan(host.SimulationTimeSeconds, id,
                    Route(host, ext, api), profile, "Checkout Order Submission",
                    new[] { new OperationStage(OperationKind.Write, work, 1f, "Submitting order") },
                    hostile)));
        }

        private static NodeView[] Route(SimulationHost host, NodeView origin, NodeView target)
        {
            var paths = new Queue<List<NodeView>>();
            var visited = new HashSet<NodeView> { origin };
            paths.Enqueue(new List<NodeView> { origin });
            while (paths.Count > 0)
            {
                List<NodeView> path = paths.Dequeue();
                NodeView tail = path[path.Count - 1];
                foreach (PhysicalLinkReference link in host.PhysicalLinks)
                {
                    NodeView adjacent = link.View.NodeA == tail ? link.View.NodeB :
                        link.View.NodeB == tail ? link.View.NodeA : null;
                    if (adjacent == null || !visited.Add(adjacent)) continue;
                    var extended = new List<NodeView>(path) { adjacent };
                    if (adjacent == target) return extended.ToArray();
                    paths.Enqueue(extended);
                }
            }
            throw new InvalidOperationException("No physical route for company feedback witness.");
        }

        private static void Cases()
        {
            var host = UnityEngine.Object.FindFirstObjectByType<SimulationHost>();
            var feedback = UnityEngine.Object.FindFirstObjectByType<CompanyKillFeedback>();
            var director = UnityEngine.Object.FindFirstObjectByType<TextCommunicationDirector>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<ConnectionTrafficDirector>();
            Check(host != null && feedback != null && director != null && traffic != null,
                "scene hosts the feedback on the existing communication surface");
            Check(feedback.GetComponent<TextCommunicationDirector>() == director &&
                feedback.Company != null && feedback.Company.CompanyId == "northstar-market" &&
                feedback.Company.OrderSubmitKillResponses.Length > 0 &&
                feedback.Company.GenericBenignKillResponses.Length > 0,
                "shared company, speakers and response pools are authored");

            bool oldAuto = traffic.AutoGenerationEnabled;
            var messages = new List<TextCommunication>();
            director.MessageDelivered += messages.Add;
            traffic.AutoGenerationEnabled = false;
            try
            {
                host.SetRunMode(ConnectionRunMode.Generated);
                var order = Add(host, "order", "order-submit", false);
                Check(host.KillConnection(order.ConnectionId) && feedback.HasPendingReaction &&
                    messages.Count == 0, "benign order kill schedules delayed reaction");
                feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 0, "reaction waits for simulation time");
                host.StepSimulation(1.01f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 1 && messages[0].Sender.Contains("Customer Support") &&
                    feedback.Company.OrderSubmitKillResponses.Contains(messages[0].Body) &&
                    director.ActiveEntries.Count == 1,
                    "order kill delivers a purpose-specific IM through existing surface");
                Debug.Log("Company feedback order: expected 1 Support IM; observed " + messages.Count);

                host.ResetSimulation(); messages.Clear();
                var other = Add(host, "other", "inventory-sync", false);
                Check(host.KillConnection(other.ConnectionId), "other benign purpose killed");
                host.StepSimulation(1.01f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 1 && messages[0].Sender.Contains("Operations") &&
                    feedback.Company.GenericBenignKillResponses.Contains(messages[0].Body),
                    "other benign purpose uses generic fallback");

                host.ResetSimulation(); messages.Clear();
                var hostile = Add(host, "hostile", "order-submit", true);
                Check(host.KillConnection(hostile.ConnectionId), "hostile with matching claimed purpose killed");
                host.StepSimulation(2f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 0 && !feedback.HasPendingReaction,
                    "hostile kill never complains, regardless of claimed purpose");

                host.ResetSimulation(); messages.Clear();
                var completed = Add(host, "complete", "order-submit", false, 0.01f);
                host.StepSimulation(10f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(completed.Resolution == ConnectionResolution.Completed && messages.Count == 0 &&
                    !feedback.HasPendingReaction, "ordinary benign completion stays silent");

                host.ResetSimulation(); messages.Clear();
                for (int i = 0; i < 5; i++)
                    Check(host.KillConnection(Add(host, "rapid" + i, "order-submit", false).ConnectionId),
                        "rapid benign kill " + i);
                host.StepSimulation(1.01f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 1, "rapid kills produce only one pending IM");
                Check(host.KillConnection(Add(host, "cooldown", "inventory-sync", false).ConnectionId),
                    "benign kill during cooldown");
                host.StepSimulation(2f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 1, "cooldown suppresses later rapid kill");
                Debug.Log("Company feedback burst: expected 1 IM; observed " + messages.Count);

                host.ResetSimulation(); messages.Clear();
                Check(host.KillConnection(Add(host, "stale", "order-submit", false).ConnectionId) &&
                    feedback.HasPendingReaction, "reaction pending before reset");
                host.ResetSimulation(); host.StepSimulation(2f);
                feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 0 && !feedback.HasPendingReaction &&
                    director.ActiveEntries.Count == 0, "reset clears pending and displayed reactions");
                Check(host.KillConnection(Add(host, "fresh", "inventory-sync", false).ConnectionId),
                    "new run can react after reset");
                host.StepSimulation(1.01f); feedback.DeliverDue(host.SimulationTimeSeconds);
                Check(messages.Count == 1 && messages[0].Sender.Contains("Operations"),
                    "reset clears cooldown without delivering stale response");
            }
            finally
            {
                director.MessageDelivered -= messages.Add;
                traffic.AutoGenerationEnabled = oldAuto;
                host.ResetSimulation();
            }
        }
    }
}
