using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Application;

/// <summary>当前对局会话：绑定玩家账号与操控角色。</summary>
public class GameSession(GamePlayer player, Character character)
{
    public GamePlayer Player { get; private set; } = player;

    public Character Character { get; private set; } = character;
}