using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uptime.Connections
{
    [DefaultExecutionOrder(1000)]
    public sealed class EscapeCoordinator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float targetChangeProtectionSeconds = 0.15f;
        private readonly List<IEscapeTarget> targets = new();
        private IEscapeTarget currentTarget;
        private IEscapeTarget dismissingTarget;
        private double protectedUntil;
        private bool escapeHeld;

        public IEscapeTarget CurrentTarget => currentTarget;

        public void Register(IEscapeTarget target)
        {
            if (!Eligible(target) || targets.Contains(target)) return;
            target.SetEscapeTarget(false);
            targets.Add(target);
            Refresh(Time.unscaledTimeAsDouble, true);
        }

        public void Unregister(IEscapeTarget target)
        {
            if (!targets.Remove(target)) return;
            Refresh(Time.unscaledTimeAsDouble, !ReferenceEquals(target, dismissingTarget));
        }

        private static bool Alive(IEscapeTarget target) =>
            target is MonoBehaviour component && component != null;

        private static bool Eligible(IEscapeTarget target) =>
            target is MonoBehaviour component && component != null &&
            component.isActiveAndEnabled && target.CanDismissWithEscape;

        private void OnEnable()
        {
            // Re-enabling while Escape is held must not manufacture a new press.
            escapeHeld = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
            Refresh(Time.unscaledTimeAsDouble, true);
        }

        private void OnDisable()
        {
            if (Alive(currentTarget)) currentTarget.SetEscapeTarget(false);
            currentTarget = null;
        }

        private void OnDestroy()
        {
            if (Alive(currentTarget)) currentTarget.SetEscapeTarget(false);
            targets.Clear();
            currentTarget = null;
        }

        private void LateUpdate() => ProcessEscape(
            Keyboard.current != null && Keyboard.current.escapeKey.isPressed,
            Time.unscaledTimeAsDouble);

        private void Refresh(double now, bool protect)
        {
            targets.RemoveAll(target => !Eligible(target));
            IEscapeTarget next = isActiveAndEnabled && targets.Count > 0 ? targets[^1] : null;
            if (ReferenceEquals(next, currentTarget)) return;
            if (Alive(currentTarget)) currentTarget.SetEscapeTarget(false);
            currentTarget = next;
            if (Alive(currentTarget)) currentTarget.SetEscapeTarget(true);
            if (protect) protectedUntil = now + targetChangeProtectionSeconds;
        }

        private void ProcessEscape(bool pressed, double now)
        {
            Refresh(now, true);
            if (!pressed) { escapeHeld = false; return; }
            if (escapeHeld) return;
            // Consume even protected presses. Never fall through or replay them.
            escapeHeld = true;
            if (now < protectedUntil || currentTarget == null) return;
            IEscapeTarget target = currentTarget;
            dismissingTarget = target;
            try { target.DismissFromEscape(); }
            finally
            {
                dismissingTarget = null;
                Refresh(now, true);
            }
        }
    }
}
