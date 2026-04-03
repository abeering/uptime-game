using System;

[Serializable]
public class InfectionApplicationRules
{
    public InfectionTargetRule targetRule = InfectionTargetRule.FirstReachedNode;
    public int nthNode = 1;
    public bool allowAlreadyInfectedNode = false;
}

public enum InfectionTargetRule
{
    FirstReachedNode,
    NthReachedNode,
    AnyReachedNode,
    DestinationNode
}