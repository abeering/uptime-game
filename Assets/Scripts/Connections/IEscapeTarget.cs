namespace Uptime.Connections
{
    // Dismissal ownership never implies ownership of keyboard typing.
    public interface IEscapeTarget
    {
        bool CanDismissWithEscape { get; }
        void SetEscapeTarget(bool targeted);
        void DismissFromEscape();
    }
}
