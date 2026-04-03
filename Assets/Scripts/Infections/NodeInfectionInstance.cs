public abstract class NodeInfectionInstance
{
    public abstract InfectionType Type { get; }

    protected NodeView node;
    protected InfectionPayload payload;

    public InfectionPayload Payload => payload;

    public virtual void Initialize(NodeView nodeView, InfectionPayload infectionPayload)
    {
        node = nodeView;
        payload = infectionPayload;
    }

    public virtual void OnApplied() { }
    public virtual void OnRemoved() { }
    public virtual void OnTick(InfectionContext context) { }

    // Passive effects
    public virtual bool BlocksTraffic() => false;
}