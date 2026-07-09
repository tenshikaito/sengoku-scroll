using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

public sealed class Region
{
    public int RegionId { get; set; }

    public ClimateFactor CurrentSeasonClimateFactor { get; set; }

    /// <summary>
    /// 气候异常修正
    /// </summary>
    public byte ClimateOffset { get; set; }

    /// <summary>
    /// 是否已下雪
    /// </summary>
    /// <remarks>
    /// <para>用来表现下雪状态以及积雪</para>
    /// <para>土地被积雪覆盖的状态、会影响所有单位移动</para>
    /// <para>天气转暖会取消</para>
    /// <para>部分平地会转为泥地</para>
    /// </remarks>
    public bool IsSnowCovered { get; set; }
}
