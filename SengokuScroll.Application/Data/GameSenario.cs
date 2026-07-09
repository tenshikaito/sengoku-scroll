using SengokuScroll.Domain;

namespace SengokuScroll.Application.Data;

public sealed class GameSenario
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required GameMapMasterData GameMapMasterData { get; set; }

    public required GameMasterData GameMasterData { get; set; }

    public required GameMapData GameMapData { get; set; }

    public required GameData GameData { get; set; }

}
