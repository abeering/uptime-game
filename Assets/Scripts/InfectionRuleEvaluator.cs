public static class InfectionRuleEvaluator
{
    public static bool CanApply(PacketView packet, NodeView node, InfectionPayload payload)
    {
        if (packet == null || node == null || payload == null)
            return false;

        if (payload.type == InfectionType.None)
            return false;

        InfectionApplicationRules rules = payload.rules ?? new InfectionApplicationRules();

        if (!rules.allowAlreadyInfectedNode && node.IsInfected)
            return false;

        switch (rules.targetRule)
        {
            case InfectionTargetRule.FirstReachedNode:
                return packet.nodesReachedCount == 1;

            case InfectionTargetRule.NthReachedNode:
                return packet.nodesReachedCount == rules.nthNode;

            case InfectionTargetRule.AnyReachedNode:
                return true;

            case InfectionTargetRule.DestinationNode:
                return packet.GetCurrentDestinationNode() == packet.GetDestination();

            default:
                return false;
        }
    }
}