using System;

[Serializable]
public class InfectionPayload
{
    public InfectionType type = InfectionType.None;
    public InfectionApplicationRules rules = new();
    public InfectionParameters parameters = new();

    public InfectionPayload() { }

    public InfectionPayload(InfectionType type)
    {
        this.type = type;
        this.rules = new InfectionApplicationRules();
        this.parameters = new InfectionParameters();
    }
}