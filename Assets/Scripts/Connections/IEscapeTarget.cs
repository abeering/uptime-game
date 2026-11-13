namespace Uptime.Connections
{
    // Dismissal ownership never implies ownership of keyboard typing.
    public interface IEscapeTarget
    {
        bool CanDismissWithEscape { get; }
        void SetEscapeTarget(bool targeted);
        void DismissFromEscape();
    }

    // The active Duel keeps Escape even when a passive call opens later.
    public interface IExclusiveEscapeTarget { }
}
