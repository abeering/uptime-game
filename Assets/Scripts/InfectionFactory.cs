public static class InfectionFactory
{
    public static InfectionPayload CreateDefaultPayload(InfectionType type)
    {
        if (type == InfectionType.None)
            return null;

        InfectionPayload payload = new InfectionPayload(type);

        // Payload/rule/parameter classes already own the baseline defaults.
        // This is the intended extension point for future infection profile logic:
        // - profile IDs / variants (ex: spawner_fast, spawner_elite)
        // - keyword-based infection scalers
        // - attack-plan or level-driven infection adjustments
        //
        // Intended long-term precedence:
        // baseline payload defaults < profile/scaler adjustments < explicit spawn/debug overrides

        return payload;
    }

    public static NodeInfectionInstance Create(InfectionPayload payload)
    {
        if (payload == null)
            return null;

        return payload.type switch
        {
            InfectionType.Blackout => new BlackoutInfectionInstance(),
            InfectionType.Spawner => new SpawnerInfectionInstance(),
            InfectionType.Throttle => new ThrottleInfectionInstance(),
            _ => null
        };
    }

    public static NodeInfectionInstance Create(InfectionType type)
    {
        if (type == InfectionType.None)
            return null;

        return Create(CreateDefaultPayload(type));
    }
}