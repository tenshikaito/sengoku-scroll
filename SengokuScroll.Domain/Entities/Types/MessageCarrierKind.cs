namespace SengokuScroll.Domain.Entities.Types;

/// <summary>
/// 文书载体类型：决定战争迷雾下的视野共享与地图可见规则。
/// UnitEscort＝单位编制护送（势力迷雾下贡献视野）；Character＝匿名角色信差（不共享视野）。
/// </summary>
public enum MessageCarrierKind
{
    UnitEscort,
    Character,
}
