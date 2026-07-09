using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Application;

public class GameSession(GamePlayer player, Character character)
{
    public GamePlayer Player { get; private set; } = player;

    public Character Character { get; private set; } = character;
}