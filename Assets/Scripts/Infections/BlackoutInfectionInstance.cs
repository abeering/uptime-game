using UnityEngine;

public class BlackoutInfectionInstance : NodeInfectionInstance
{
    public override InfectionType Type => InfectionType.Blackout;
    public override Color? GetNodeTintColor() => new Color(0.45f, 0.1f, 0.1f);

    public override bool BlocksTraffic() => true;
}