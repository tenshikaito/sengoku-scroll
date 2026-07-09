using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 地形
/// </summary>
public partial class TerrainDefinition
{
    public TerrainType Type { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// 海拔
    /// </summary>
    /// <remarks>
    /// <para>表示该地形的海拔是多少、默认为0</para>
    /// <para>配合地图上的特殊海拔可以让角色的z坐标在特殊情况下也可以通行如穿山洞</para>
    /// </remarks>
    public required int Altitude { get; set; }

    /// <summary>
    /// 移动成本
    /// </summary>
    public int MovementCost { get; set; }
}
