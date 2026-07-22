using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 在途文书载体（过渡实现）：携带 <see cref="MessagePayload"/> 在格间移动。
/// 长期由单位/角色实体直接实现 <see cref="IMessageCarrier"/>。
/// </summary>
public class MessageCarrier : IMessageCarrier
{
    /// <summary>载体唯一 Id。</summary>
    public int Id { get; set; }

    /// <summary>显示名（如「清洲文书→织田先锋」）。</summary>
    public required string Name { get; set; }

    /// <summary>所属势力 Id（发送方）。</summary>
    public int ForceId { get; set; }

    /// <summary>
    /// 载体类型：单位护送可在势力迷雾下贡献视野；匿名角色信差不共享视野。
    /// </summary>
    public MessageCarrierKind CarrierKind { get; set; } = MessageCarrierKind.Character;

    /// <summary>恒为 0：NPC 编制，不指派具体将领（满足 <see cref="IHasLeader"/>）。</summary>
    public int LeaderId { get; set; }

    /// <summary>非军事单位；可与友军 occupy 同格。</summary>
    public bool IsMilitary => false;

    /// <summary>当前所在地图格。</summary>
    public Point3 Location { get; set; }

    /// <summary>出发据点 Id。</summary>
    public int SourceStrongholdId { get; set; }

    /// <summary>传令兵（NPC）人数。</summary>
    public int CourierCount { get; set; }

    /// <summary>护卫兵（NPC）人数；不绑定具体武将。</summary>
    public int EscortSoldierCount { get; set; }

    /// <summary>载体在途状态。</summary>
    public MessageCarrierStatus Status { get; set; }

    /// <summary>剩余路径格点队列；抵达末格时触发投递逻辑。</summary>
    public Queue<Point3> RoutePoints { get; set; } = new();

    /// <summary>携带的文书载荷。</summary>
    public required MessagePayload Payload { get; set; }
}
