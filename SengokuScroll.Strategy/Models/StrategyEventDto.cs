namespace SengokuScroll.Strategy.Models;

/// <summary>日推进或信使投递产生的玩家可见事件（左上角消息栏）。</summary>
public sealed record StrategyEventDto
{
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
}
