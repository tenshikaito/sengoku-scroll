namespace SengokuScroll.Domain.Definitions;

public sealed class RegionDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int ClimateId { get; set; }

    /// <summary>
    /// 台风频率
    /// </summary>
    public byte TyphoonRate { get; set; }

    /// <summary>
    /// 地震频率
    /// </summary>
    public byte EarthquakeRate { get; set; }

    /// <summary>
    /// 干旱频率
    /// </summary>
    public byte DroughtRate { get; set; }

    /// <summary>
    /// 寒潮频率
    /// </summary>
    public byte ColdWaveRate { get; set; }

    /// <summary>
    /// 暴雪频率
    /// </summary>
    public byte SnowstormRate { get; set; }

    /// <summary>
    /// 洪水频率
    /// </summary>
    public byte FloodRate { get; set; }

    /// <summary>
    /// 风暴频率
    /// </summary>
    public byte StormRate { get; set; }

    /// <summary>
    /// 蝗灾频率
    /// </summary>
    public byte LocustRate { get; set; }
}
