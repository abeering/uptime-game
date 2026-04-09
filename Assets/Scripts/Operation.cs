public abstract class Operation
{
    public int id;
    public string displayId;
    public bool isFinished;
    public int lingerTicksRemaining = 0;

    public abstract string OperationType { get; }

    public abstract void Tick(CommandDirector context);

    public abstract void OnPacketRemoved(string packetId, PacketRemovalReason reason, CommandDirector context);

    public abstract bool CanCancel();

    public abstract void Cancel(CommandDirector context);

    public abstract string GetOperationsLine();

    public virtual bool ShouldRemove()
    {
        return isFinished && lingerTicksRemaining <= 0;
    }

    public virtual void UpdateLinger(int ticks)
    {
        if (!isFinished)
            return;

        lingerTicksRemaining -= ticks;
    }
    
}