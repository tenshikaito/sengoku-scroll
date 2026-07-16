using SengokuScroll.Domain;

namespace SengokuScroll.Application.Data;

/// <summary>剧本静态数据：地图主数据、势力开局与可变 <see cref="GameData"/>。</summary>
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
