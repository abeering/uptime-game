using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // Binds authored card elements; ordinary UI is authored in the experimental scene/prefab.
    public sealed class IntegrityPresenter : MonoBehaviour
    {
        [Serializable] public sealed class NodeBinding
        {
            public NodeView node;
            public Image background, accent;
            public Outline border;
            public TMP_Text marker;
            [NonSerialized] public Color originalBackground, originalAccent, originalBorder;
        }
        [Serializable] public sealed class LinkBinding
        {
            public PhysicalLinkView link;
            public Graphic line;
            public TMP_Text label;
            [NonSerialized] public Color originalLine, originalLabel;
        }
        [SerializeField] private SimulationHost host;
        [SerializeField] private Terminal terminal;
        [SerializeField] private ConnectionsDuelOverlay duelOverlay;
        [SerializeField] private ConnectionsCleanOverlay cleanOverlay;
        [SerializeField] private NodeBinding[] nodes;
        [SerializeField] private LinkBinding[] links;
        [SerializeField] private GameObject breachPanel;
        [SerializeField] private TMP_Text breachPath;
        private bool ended;
        private static readonly Color Orange = new Color(1f, .63f, .25f);
        private static readonly Color Red = new Color(1f, .25f, .30f);

        private void OnEnable()
        {
            foreach (var binding in nodes)
            {
                binding.originalBackground = binding.background.color;
                binding.originalAccent = binding.accent.color;
                binding.originalBorder = binding.border.effectColor;
            }
            foreach (var binding in links)
            { binding.originalLine = binding.line.color; binding.originalLabel = binding.label.color; }
            if (host != null) host.IntegrityOccurred += OnIntegrity;
        }
        private void OnDisable() { if (host != null) host.IntegrityOccurred -= OnIntegrity; }
        private void OnIntegrity(IntegrityEvent entry)
        {
            if (entry.type != "integrity_damage" || !entry.visible) return;
            terminal?.AppendText(entry.after == 0 ? "COMPROMISED  " + entry.target :
                "−1 INTEGRITY  " + entry.target + (entry.revealed ? "  /  " + entry.after + "/2" : ""), Red);
        }
        private void LateUpdate() => RenderNow();
        public void RenderNow()
        {
            if (host?.Integrity == null) return;
            foreach (var binding in nodes)
            {
                IntegrityNode state = host.Integrity.For(host.FindNodeResources(binding.node.NodeId));
                bool compromised = state.Compromised;
                bool damaged = state.Revealed && state.Value == 1;
                binding.background.color = compromised ? new Color(.23f, .075f, .095f) : binding.originalBackground;
                binding.accent.color = compromised ? Red : damaged ? Orange : binding.originalAccent;
                binding.border.effectColor = compromised ? Red : damaged ? Orange : binding.originalBorder;
                binding.marker.text = state.External ? "UNTRUSTED" : state.Critical ? "CRITICAL" : "";
                binding.marker.color = state.External ? new Color(.72f, .63f, .82f) :
                    compromised ? Red : new Color(.91f, .83f, .55f);
            }
            foreach (var binding in links)
            {
                bool connected = host.Integrity.IsConnectedLink(binding.link.LinkId);
                bool winning = host.Integrity.Breach?.links.Contains(binding.link.LinkId) == true;
                binding.line.color = connected ? winning ? new Color(1f, .52f, .54f) : Red : binding.originalLine;
                binding.label.color = connected ? Red : binding.originalLabel;
                ((RectTransform)binding.line.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    winning ? 8f : connected ? 5f : 2f);
            }
            breachPanel.SetActive(host.RunEnded);
            if (host.RunEnded)
            {
                breachPanel.transform.SetAsLastSibling();
                breachPath.text = host.Integrity.Breach.criticalNode + "  /  CRITICAL NODE BREACHED\n\n" +
                    string.Join(" → ", host.Integrity.Breach.path) + "\n\nSIMULATION FROZEN  /  TYPE restart";
                if (!ended)
                {
                    duelOverlay?.Abort();
                    cleanOverlay?.AbortForBreach();
                    terminal?.SuspendKeyboard(false);
                    terminal?.AppendText("CORE BREACHED  " + string.Join(" → ", host.Integrity.Breach.path) +
                        "  /  type restart", Red);
                    terminal?.RestoreKeyboardFocus();
                }
            }
            ended = host.RunEnded;
        }
    }
}
