namespace SengokuScroll.Domain.Entities.Types;

public enum TerrainType : byte
{
    /// <summary>
    /// 平地
    /// </summary>
    /// <remarks>需要温度适中湿度偏干</remarks>
    Plain,
    /// <summary>
    /// 丘陵
    /// </summary>
    /// <remarks>只能建造平山城</remarks>
    Hill,
    /// <summary>
    /// 山地
    /// </summary>
    /// <remarks>
    /// <para>影响所有单位移动</para>
    /// <para>严重影响骑兵移动</para>
    /// <para>器械完全无法移动</para>
    /// </remarks>
    Mountain,
    /// <summary>
    /// 山脉
    /// </summary>
    /// <remarks>完全无法通行</remarks>
    MountainRange,
    /// <summary>
    /// 荒地
    /// </summary>
    /// <remarks>
    /// <para>硬质地面或半沙化的土壤或盐碱地无法建造据点</para>
    /// <para>影响骑兵及器械的移动</para>
    /// <para>不毛之地完全无法补给</para>
    /// </remarks>
    Badlands,
    /// <summary>
    /// 沙漠
    /// </summary>
    /// <remarks>
    /// <para>无法建造据点种植作物</para>
    /// <para>会影响骆驼以外的所有单位移动</para>
    /// </remarks>
    Desert,
    /// <summary>
    /// 冰原
    /// </summary>
    /// <remarks>
    /// <para>土地冰冻状态、无法建立据点、会影响所有单位移动</para>
    /// <para>不毛之地完全无法补给</para>
    /// <para>只会出现在气温极低的平地上</para>
    /// </remarks>
    Permafrost,
    /// <summary>
    /// 河流
    /// </summary>
    /// <remarks>
    /// <para>淡水、在周边建设据点有农业及人口增长加成</para>
    /// <para>带有流向、顺水会减少行动力消耗、逆水会相反增加</para>
    /// </remarks>
    River,
    /// <summary>
    /// 湖泊
    /// </summary>
    /// <para>淡水、在周边建设据点有农业及人口增长加成</para>
    Lake,
    /// <summary>
    /// 浅海
    /// </summary>
    /// <remarks>咸水、没有淡水加成、仅可通过小型船只</remarks>
    ShallowSea,
    /// <summary>
    /// 深海
    /// </summary>
    /// <remarks>咸水、没有淡水加成、可通过大型船只</remarks>
    DeepSea,
}
