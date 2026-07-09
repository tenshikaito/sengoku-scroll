namespace SengokuScroll.Domain;

/// <summary>
/// 地图格点 → 实体 Id 的空间索引（与 <see cref="GameData"/> 中实体坐标对应）。
/// 修改实体坐标须通过 <see cref="Actions.MapLocationActions"/>，禁止只改 <c>Location</c> 而不更新本索引。
/// </summary>
public class GameMapData
{
    public required Dictionary<int, int> Characters { get; set; } = [];

    public required Dictionary<int, int> Strongholds { get; init; } = [];

    public required Dictionary<int, int> Units { get; init; } = [];
}
