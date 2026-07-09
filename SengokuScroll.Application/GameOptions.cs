using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application;

public class GameOptions
{
    public required GamePlayer GamePlayer { get; init; }

    public required GameWorld GameWorld { get; set; }

    public required GameMode GameMode { get; init; }

    public required int PlayerSelectedId { get; init; }

    public required IServiceProvider ServiceProvider { get; set; }
}

public enum GameMode
{
    /// <summary>
    /// 角色扮演模式、参考太阁立志传、仅单人
    /// </summary>
    RolePlaying,
    /// <summary>
    /// 战略模式、参考信长之野望、可单人玩也可联机
    /// </summary>
    GrandStrategy,
    /// <summary>
    /// 网游模式、日常角色扮演模式、国战时期战略模式
    /// </summary>
    PersistentOnline,
}
