namespace SengokuScroll.Strategy.Models;

/// <summary>日推进或信使投递产生的玩家可见事件（左上角消息栏）。</summary>
public sealed record StrategyEventDto
{
    /// <summary>Explicit private recipient; legacy single-player events may omit it.</summary>
    public int? RecipientForceId { get; init; }
    public string? OccurrenceKey { get; init; }
    /// <summary>MessengerArrived | PolicyDelivered | BattleReportArrived 等。</summary>
    public required string Category { get; init; }

    /// <summary>详情文案（对话框 / 通知点击后展示）。</summary>
    public required string Message { get; init; }

    /// <summary>大略信息（左上角消息栏 / 消息图标 tooltip）；省略时与 Message 相同。</summary>
    public string? Brief { get; init; }

    /// <summary>收支结算结构化明细（Category=EconomyMonthly | EconomyAnnual 时）。</summary>
    public StrategyEconomySettlementDetailDto? EconomySettlement { get; init; }

    /// <summary>Category=BattleReportArrived 时附带的完整战报（信使抵达或同格即时送达）。</summary>
    public StrategyBattleResultDto? BattleResult { get; init; }

    /// <summary>Category=StrategicReportArrived 时附带的原始事件分类（UnitDestroyed 等）。</summary>
    public string? DetailCategory { get; init; }

    /// <summary>Category=StrategicReportArrived 时附带的完整详情文案。</summary>
    public string? DetailMessage { get; init; }

    /// <summary>Category=RecruitTaskCompleted 时的汇报将领 Id。</summary>
    public int? CharacterId { get; init; }

    /// <summary>Category=RecruitTaskCompleted 时的汇报将领姓名。</summary>
    public string? CharacterName { get; init; }

    /// <summary>详情对话框标题（Category=RecruitTaskCompleted 等）；Brief 仍为气泡/简报文案。</summary>
    public string? Title { get; init; }
}
