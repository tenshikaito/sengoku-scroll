namespace SengokuScroll.Domain;

/// <summary>
/// 地图格点 → 实体 Id 的空间索引（与 <see cref="GameData"/> 中实体坐标对应）。
/// 修改实体坐标须通过 <see cref="Actions.MapLocationActions"/>，禁止只改 <c>Location</c> 而不更新本索引。
/// 军事单位每格可为多 Id（同势力/同战共战方可叠）。
/// </summary>
public class GameMapData
{
    public required Dictionary<int, int> Characters { get; set; } = [];

    public required Dictionary<int, int> Strongholds { get; init; } = [];

    /// <summary>tileIndex → 该格军事单位 Id 列表（可空列表视为无）。</summary>
    public required Dictionary<int, List<int>> Units { get; init; } = [];
}
