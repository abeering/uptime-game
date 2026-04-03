public static class InfectionFactory
{
    public static NodeInfectionInstance Create(InfectionType type)
    {
        return type switch
        {
            InfectionType.Blackout => new BlackoutInfectionInstance(),
            _ => null
        };
    }
}