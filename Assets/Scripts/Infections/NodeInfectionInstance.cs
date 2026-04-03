using UnityEngine;

public abstract class NodeInfectionInstance
{
    public abstract InfectionType Type { get; }

    // for colorizing nodes on infection 
    public virtual Color? GetNodeTintColor() => null;
    public virtual float GetNodeTintStrength() => 0.5f;
    public virtual int GetVisualPriority() => 0;

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