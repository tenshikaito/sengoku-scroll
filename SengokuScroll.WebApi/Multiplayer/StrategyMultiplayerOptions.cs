namespace SengokuScroll.WebApi.Multiplayer;

public sealed class StrategyMultiplayerOptions
{
    // Background browser tabs commonly delay timers. Allow recovery before AI takeover.
    public int ConnectionLeaseSeconds { get; set; } = 90;
    public bool PersistenceEnabled { get; set; } = true;
    public int IdleHibernateSeconds { get; set; } = 600;
    public string StoragePath { get; set; } = "App_Data/multiplayer-rooms";
}
