using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uptime.Connections
{
    // A small authored node-configuration window. It owns input, never simulation time.
    public sealed class ConnectionsSshOverlay : MonoBehaviour, IEscapeTarget
    {
        [SerializeField] private EscapeCoordinator escapeCoordinator;
        [SerializeField] private ConnectionsWindowPlacement placement;
        [SerializeField] private ConnectionsDuelOverlay duelOverlay;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text currentPolicy;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private TMP_Text description;
        private bool configuringCache;
        private NodeView configuredNode;
        [SerializeField] private TMP_Text[] choices;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Color selectedColor = new(.64f, .94f, .81f);
        [SerializeField] private Color ordinaryColor = new(.47f, .64f, .62f);
        private SimulationHost host;
        private Terminal terminal;
        private IReadOnlyList<ConnectionTrafficProfile> catalog;
        private int selected;
        private bool opened, suspended, waitingForRelease, escapeTarget;
        public bool IsOpen => opened && gameObject.activeInHierarchy;
        public bool CanDismissWithEscape => IsOpen;
        public int SelectedIndex => selected;

        public bool Open(SimulationHost simulation, Terminal owner) => Open(simulation, owner, simulation?.ApiConfigurationNode);
        public bool Open(SimulationHost simulation, Terminal owner, NodeView node)
        {
            if (simulation == null || simulation.RunEnded || node == null) return false;
            if (opened) Close();
            host = simulation; terminal = owner; configuredNode = node; configuringCache = node == host.CacheConfigurationNode;
            catalog = configuringCache ? host.CachePurposeChoices : host.ApiPurposeChoices;
            if (catalog.Count + 1 > choices.Length || choices.Length != choiceButtons.Length) return false;
            selected = 0;
            for (int i = 0; i < catalog.Count; i++) if (catalog[i].id == SelectedPurpose) selected = i + 1;
            opened = true; suspended = false;
            host.ApiPolicyChanged += Render; host.CachePolicyChanged += Render; host.SimulationReset += Abort; host.RunFinished += Abort;
            gameObject.SetActive(true); title.text = "SSH / " + configuredNode.NodeId;
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i; choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => { selected = index; ApplySelected(); });
                choices[i].transform.parent.gameObject.SetActive(i <= catalog.Count);
            }
            terminal.SuspendKeyboard(true);
            waitingForRelease = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
            placement.PlaceSsh(configuredNode); transform.SetAsLastSibling();
            escapeCoordinator.Register(this); EventSystem.current?.SetSelectedGameObject(gameObject);
            Render(); return true;
        }
        public void SuspendKeyboard(bool value, bool guardHeldKeys = false)
        {
            suspended = value;
            if (guardHeldKeys) waitingForRelease = true;
            foreach (var button in choiceButtons) button.interactable = !value;
        }
        public void RestoreKeyboardFocus()
        {
            if (!IsOpen || host.RunEnded) return;
            terminal.SuspendKeyboard(true); EventSystem.current?.SetSelectedGameObject(gameObject);
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (host == null || host.RunEnded) { Abort(); return; }
            Render();
            if (suspended || duelOverlay != null && duelOverlay.IsOpen) return;
            var keyboard = Keyboard.current; if (keyboard == null) return;
            if (waitingForRelease) { if (!keyboard.anyKey.isPressed) waitingForRelease = false; return; }
            if (keyboard.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % (catalog.Count + 1);
            if (keyboard.upArrowKey.wasPressedThisFrame) selected = (selected + catalog.Count) % (catalog.Count + 1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) ApplySelected();
        }
        public void ApplySelected()
        {
            if (!IsOpen || suspended || duelOverlay != null && duelOverlay.IsOpen) return;
            string id = selected == 0 ? null : catalog[selected - 1].id;
            bool accepted = configuringCache ? host.TrySetCachePurpose(id, out string result) : host.TrySetApiPurpose(id, out result);
            if (accepted) terminal.AppendText(result, selectedColor);
            Render(); EventSystem.current?.SetSelectedGameObject(gameObject);
        }
        private string SelectedPurpose => configuringCache ? host.CachePurposeId : host.ApiFavoredPurposeId;
        private void Render()
        {
            if (host == null || catalog == null) return;
            currentPolicy.text = (configuringCache ? "CACHED: " : "FAVORED: ") + (string.IsNullOrEmpty(SelectedPurpose) ? "NONE" : SelectedPurpose) +
                (configuringCache ? "" : $"     WEIGHT ×{host.ApiAllocationWeight:G3}");
            if (description != null) description.text = configuringCache ? $"READ ONLY · WORK ×{host.CachedWorkMultiplier:G3} · LOAD ×{host.CachedLoadMultiplier:G3}" : "FAVOR ONE PURPOSE · TOTAL CAPACITY UNCHANGED";
            for (int i = 0; i <= catalog.Count; i++)
            {
                string id = i == 0 ? null : catalog[i - 1].id;
                bool active = id == (string.IsNullOrEmpty(SelectedPurpose) ? null : SelectedPurpose);
                choices[i].text = (i == selected ? "> " : "  ") + (active ? "[X] " : "[ ] ") +
                    (i == 0 ? (configuringCache ? "NONE / CLEAR CACHE" : "NONE / CLEAR PRIORITY") : catalog[i - 1].id + "  /  " + catalog[i - 1].claimedPurpose);
                choices[i].color = i == selected ? selectedColor : ordinaryColor;
            }
            hint.text = "↑↓ CHOOSE   ENTER APPLY" + (escapeTarget ? "   ESC CLOSE" : "");
        }
        public void SetEscapeTarget(bool targeted) { escapeTarget = targeted; if (IsOpen) Render(); }
        public void DismissFromEscape() => Close();
        public void Abort() => Close();
        public void Close()
        {
            if (!opened) return;
            opened = false;
            if (host != null) { host.ApiPolicyChanged -= Render; host.CachePolicyChanged -= Render; host.SimulationReset -= Abort; host.RunFinished -= Abort; }
            escapeCoordinator?.Unregister(this); gameObject.SetActive(false);
            if (duelOverlay == null || !duelOverlay.IsOpen)
            { terminal?.SuspendKeyboard(false, true); terminal?.RestoreKeyboardFocus(); }
        }
        private void OnDisable() { if (opened) Close(); }
    }
}
