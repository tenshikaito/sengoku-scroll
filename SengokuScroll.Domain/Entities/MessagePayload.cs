using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Entities;

/// <summary>在途文书的载荷数据（与载体实体分离）。</summary>
public class MessagePayload
{
    /// <summary>载荷类型。</summary>
    public MessagePayloadType Type { get; set; }

    /// <summary>目标军事单位 Id；方针/指令类载荷的接收方。</summary>
    public int TargetUnitId { get; set; }

    /// <summary>目标据点 Id；<see cref="MessagePayloadType.TaxRateChange"/> 时使用。</summary>
    public int TargetStrongholdId { get; set; }

    /// <summary>
    /// 假情报等载荷的目标运输队 Id。
    /// 仅当 <see cref="Type"/> 为 <see cref="MessagePayloadType.FalseIntelligence"/> 时使用；0 表示无。
    /// </summary>
    public int TargetConvoyId { get; set; }

    /// <summary>待投递方针（<see cref="MessagePayloadType.PolicyChange"/> 时有效）。</summary>
    public UnitDirective? PendingDirective { get; set; }

    /// <summary>待投递税率（<see cref="MessagePayloadType.TaxRateChange"/> 时有效）。</summary>
    public PendingStrongholdTaxChange? PendingTaxChange { get; set; }
}
