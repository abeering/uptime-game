using System.Collections.Generic;
using UnityEngine;

namespace Uptime.Connections
{
    // Present only in the experimental Integrity scene. No player-facing state or commands.
    public sealed class HackerDirectives : MonoBehaviour
    {
        [SerializeField] private SimulationHost host;
        [SerializeField] private HackerDirective directive = HackerDirective.SPREAD;
        [SerializeField, Min(.02f)] private float commitmentSeconds = 60f;
        [SerializeField, Range(0, 1)] private float preferenceProbability = .7f;
        private HackerDirectiveSelector selector;
        private int seed;
        public HackerDirective Directive => directive;
        public float CommitmentSeconds => commitmentSeconds;
        public float PreferenceProbability => preferenceProbability;
        public HackerDirectiveSnapshot Snapshot => (selector?.Mode == directive ? selector.Snapshot() : null) ??
            new HackerDirectiveSnapshot { directive = directive.ToString() };
        public void ResetDirective(int runSeed) { seed = runSeed; selector = null; }
        public HackerTargetDecision Prefer(string activity, float time, NodeView ambient,
            IReadOnlyList<NodeView> legal, string unsupported = null, string connectionId = null)
        {
            if (host == null) host = GetComponent<SimulationHost>();
            if (!isActiveAndEnabled || host?.Integrity == null || host.RunEnded) return null;
            if (selector == null || selector.Mode != directive)
                selector = new HackerDirectiveSelector(host.Integrity, host.PhysicalLinks, directive,
                    seed, commitmentSeconds, preferenceProbability,
                    offensive => host.Recorder?.RecordHackerOffensive(offensive));
            HackerTargetDecision decision = selector.Choose(activity, time, ambient, legal, unsupported);
            decision.connectionId = connectionId;
            host.Recorder?.RecordHackerTarget(decision);
            return decision;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ConfigureForDevelopment(HackerDirective mode, float commitment = 60f, float preference = .7f)
        {
            directive = mode; commitmentSeconds = commitment; preferenceProbability = preference;
            selector = null;
        }
#endif
    }
}
