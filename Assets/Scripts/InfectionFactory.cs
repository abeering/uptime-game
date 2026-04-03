public static class InfectionFactory
{
    public static NodeInfectionInstance Create(InfectionPayload payload)
    {
        if (payload == null)
            return null;

        return payload.type switch
        {
            InfectionType.Blackout => new BlackoutInfectionInstance(),
            _ => null
        };
    }

    public static NodeInfectionInstance Create(InfectionType type)
    {
        if (type == InfectionType.None)
            return null;

        return Create(new InfectionPayload(type));
    }
}