using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 信使实体：在地图格间传递方针、战略指令、战报，或向运输队投递假情报。
/// 异格通信必须经信使；同格（含同在据点内）则无需信使。
/// 概念上等同非军事地图单位（<see cref="IsMilitary"/> = false）；编制为 NPC 传令兵与护卫，不指派具体将领。
/// </summary>
public class Messenger : IHasForce, IMapObject
{
    /// <summary>信使唯一 Id。</summary>
    public int Id { get; set; }

    /// <summary>显示名（如「清洲信使→织田先锋」）。</summary>
    public required string Name { get; set; }

    /// <summary>所属势力 Id（发送方）。</summary>
    public int ForceId { get; set; }

    /// <summary>恒为 0：信使编制为 NPC，不指派具体将领（满足 <see cref="IHasLeader"/>）。</summary>
    public int LeaderId { get; set; }

    /// <summary>非军事单位；可与友军 occupy 同格。</summary>
    public bool IsMilitary => false;

    /// <summary>当前所在地图格。</summary>
    public Point3 Location { get; set; }

    /// <summary>出发据点 Id。</summary>
    public int SourceStrongholdId { get; set; }

    /// <summary>目标军事单位 Id；方针/指令类载荷的接收方。</summary>
    public int TargetUnitId { get; set; }

    /// <summary>传令兵（NPC）人数。</summary>
    public int CourierCount { get; set; }

    /// <summary>护卫兵（NPC）人数；不绑定具体武将。</summary>
    public int EscortSoldierCount { get; set; }

    /// <summary>携带信息的类型（方针变更、战报、假情报等）。</summary>
    public MessengerPayloadType PayloadType { get; set; }

    /// <summary>信使在途状态。</summary>
    public MessengerStatus Status { get; set; }

    /// <summary>
    /// 假情报等载荷的目标运输队 Id。
    /// 仅当 <see cref="PayloadType"/> 为 <see cref="MessengerPayloadType.FalseIntelligence"/> 时使用；0 表示无。
    /// </summary>
    public int TargetConvoyId { get; set; }

    /// <summary>剩余路径格点队列；抵达末格时触发投递逻辑。</summary>
    public Queue<Point3> RoutePoints { get; set; } = new();

    /// <summary>待投递方针（<see cref="MessengerPayloadType.PolicyChange"/> 时有效）。</summary>
    public UnitDirective? PendingDirective { get; set; }
}
