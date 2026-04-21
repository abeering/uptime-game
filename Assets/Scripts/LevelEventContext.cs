public class LevelEventContext
{
    public TrafficDirector Traffic { get; }
    public NotificationDirector Notifications { get; }

    public LevelEventContext(TrafficDirector traffic, NotificationDirector notifications)
    {
        Traffic = traffic;
        Notifications = notifications;
    }

    public void ApplyTrafficModifier(TrafficModifier modifier)
    {
        Traffic?.ApplyModifier(modifier);
    }

}