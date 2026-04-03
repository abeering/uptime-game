public abstract class NodeInfectionInstance
{
    public abstract InfectionType Type { get; }

    protected NodeView node;

    public void Initialize(NodeView nodeView)
    {
        node = nodeView;
    }

    public virtual void OnApplied() { }
    public virtual void OnRemoved() { }

    // Passive effects
    public virtual bool BlocksTraffic() => false;
}