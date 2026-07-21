namespace SengokuScroll.Domain;

/// <summary>
/// 地图运行时格点索引：实体位置、道路等可变格点数据。
/// 修改实体坐标须通过 <see cref="Actions.MapLocationActions"/>，禁止只改 <c>Location</c> 而不更新本索引。
/// 军事单位每格可为多 Id（同势力/同战共战方可叠）。
/// </summary>
public class GameMapData
{
    public required Dictionary<int, int> Characters { get; set; } = [];

    public required Dictionary<int, int> Strongholds { get; init; } = [];

    /// <summary>tileIndex → 该格军事单位 Id 列表（可空列表视为无）。</summary>
    public required Dictionary<int, List<int>> Units { get; init; } = [];

    /// <summary>tileIndex → 道路类型 Id（稀疏；无路格不在字典中）。</summary>
    public required Dictionary<int, byte> Roads { get; init; } = [];
}
