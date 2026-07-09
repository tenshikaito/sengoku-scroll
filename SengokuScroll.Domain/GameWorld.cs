namespace SengokuScroll.Domain;

public class GameWorld(string name)
{
    public string Name { get; set; } = name;

    public required GameMapMasterData GameMapMasterData { get; set; }

    public required GameMasterData GameMasterData { get; set; }

    public required GameMapData GameMapData { get; set; }

    public required GameData GameData { get; set; }
}
