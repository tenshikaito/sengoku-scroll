using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 据点
/// </summary>
public class Stronghold : IHasForce
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 仅历史据点有描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public byte TypeId { get; set; }

    /// <summary>所属势力（官府）</summary>
    public int ForceId { get; set; }

    /// <summary>
    /// 领主角色 Id；0 表示当主直辖（展示势力当主名）。
    /// 领主居城须为本据点；任命/加载时同步角色驻留。
    /// </summary>
    public int LordId { get; set; }

    /// <summary>代官（执行政务；与领主可分离）。</summary>
    public int LeaderId { get; set; }

    /// <summary>
    /// 坐标
    /// </summary>
    public Point3 Location { get; set; }

    /// <summary>
    /// 统治力
    /// </summary>
    /// <remarks>
    /// <para>表示中央通过官僚系统对据点的控制程度、影响税收效率、政令执行的力量水平</para>
    /// <para>需要维持费用、其成本受政体与首都距离影响</para>
    /// <para>是会被敌对势力集中破坏的属性</para>
    /// </remarks>
    /// <value>
    /// <para>0=名义/羁縻统治：使得自治度快速上升并且无法下调压制、中央政令难以推行</para>
    /// <para>100=高度统治：政令能够顺利推行、自由调整自治度</para>
    /// </value>
    public byte Authority { get; set; }

    /// <summary>
    /// 自治度
    /// </summary>
    /// <remarks>地方势力可以自由制定政策的空间、如果统治力下降则会导致地方势力扩张权力、自治度自动扩大</remarks>
    /// <value>
    /// <para>0=高度统治：地方势力高度受中央制定政策的约束</para>
    /// <para>100=名义/羁縻统治：地方商会、寺社或领主可自由制定政策</para>
    /// </value>
    public byte Autonomy { get; set; }

    /// <summary>
    /// 行政损耗
    /// </summary>
    /// <remarks>当前据点的行政损耗程度、包括因距离首都过远导致的腐败程度、受统治者行政水平、政体国策不同及文化宗教差异等影响的行政效率的程度</remarks>
    /// <value>
    /// <para>0=行政清廉：税收高效收取上缴、统治力不易下降</para>
    /// <para>100=严重腐败：税收易被严重贪污、统治力快速下降</para>
    /// </value>
    public byte Corruption { get; set; }

    /// <summary>
    /// 人口
    /// </summary>
    public int Population { get; set; }

    /// <summary>
    /// 治安
    /// </summary>
    public byte Stability { get; set; }

    /// <summary>
    /// 势力（官府）
    /// </summary>
    public required StrongholdActor ForceActor { get; set; }

    /// <summary>
    /// 民间
    /// </summary>
    public required StrongholdActor CivilianActor { get; set; }

    /// <summary>
    /// 商会
    /// </summary>
    public required List<StrongholdActor> MerchantActors { get; set; }

    /// <summary>
    /// 寺社
    /// </summary>
    public required List<StrongholdActor> ReligionActors { get; set; }

    /// <summary>
    /// 城防
    /// </summary>
    public byte Defense { get; set; }

    /// <summary>
    /// 维持费
    /// </summary>
    public int Maintenance { get; set; }

    /// <summary>
    /// 史实: 非史实据点收支会降为 设定惩罚百分比
    /// </summary>
    public bool IsHistorical { get; set; }

    /// <summary>
    /// 人头税
    /// </summary>
    public byte PollTaxRate { get; set; }

    /// <summary>
    /// 农业税
    /// </summary>
    public byte AgricultureTaxRate { get; set; }

    /// <summary>
    /// 商业税
    /// </summary>
    public byte CommerceTaxRate { get; set; }

    /// <summary>
    /// 关税
    /// </summary>
    public byte TariffTaxRate { get; set; }

    /// <summary>
    /// 商业值：代表城市贸易规模，并线性限制商人开店数量（CommerceValue / K）。
    /// </summary>
    public int CommerceValue { get; set; }

    /// <summary>据点订单簿市场（需 Market 设施方可挂单；M4-b 起启用撮合）。</summary>
    public required StrongholdMarket Market { get; set; }

    /// <summary>城防设施</summary>
    public required List<int> DefenseFacilityIds { get; set; }

    /// <summary>经济设施（Market、奢侈品工坊等；M4-d）。</summary>
    public required List<int> EconomyFacilityIds { get; set; }

    /// <summary>
    /// 保有核心
    /// </summary>
    public required List<int> HasCoreForceIds { get; set; }
}
