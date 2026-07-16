namespace SengokuScroll.Application;

/// <summary>玩家账号（联机/存档标识）。</summary>
public class GamePlayer(Guid id, string name)
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;
}