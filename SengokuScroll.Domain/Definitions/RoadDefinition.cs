namespace SengokuScroll.Domain.Definitions;

public sealed class RoadDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int SpeedBonus { get; set; }

    /// <summary>若指定，道路格移动力消耗固定为此值（否则为地形消耗 − SpeedBonus）。</summary>
    public int? MovementCostOverride { get; set; }

    /// <summary>
    /// 是否轨道
    /// </summary>
    /// <value>true=不可以离开路线</value>
    /// <value>false=可以离开路线</value>
    public bool IsRestrictedToPath { get; set; }

    /// <summary>
    /// 可以通行的地形ID
    /// </summary>
    /// <remarks>表示是否仅能做在特定的地形上例如设计为隧道桥梁</remarks>
    /// <value>
    /// <para>null=不限地形</para>
    /// <para>notnull=仅可在特定地形</para>
    /// </value>
    public int? PassableTerrainId { get; set; }
}
