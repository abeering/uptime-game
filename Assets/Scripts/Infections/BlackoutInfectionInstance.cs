public class BlackoutInfectionInstance : NodeInfectionInstance
{
    public override InfectionType Type => InfectionType.Blackout;

    public override bool BlocksTraffic() => true;
}