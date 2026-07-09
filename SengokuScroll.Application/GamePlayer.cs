namespace SengokuScroll.Application;

public class GamePlayer(Guid id, string name)
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;
}