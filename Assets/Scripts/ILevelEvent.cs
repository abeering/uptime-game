public interface ILevelEvent
{
    int StartTick { get; }
    int EndTick { get; }

    bool IsActive(int globalTick);

    void OnTick(int globalTick, int localTick, LevelEventContext context);
}