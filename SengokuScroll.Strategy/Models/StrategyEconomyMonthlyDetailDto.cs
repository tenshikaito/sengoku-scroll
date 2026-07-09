namespace SengokuScroll.Strategy.Models;

/// <summary>月度/年度收支结算结构化明细（供前端表格对话框）。</summary>
public sealed record StrategyEconomySettlementDetailDto
{
    /// <summary>Monthly | Annual</summary>
    public required string Period { get; init; }

    public required int ReportingYear { get; init; }

    /// <summary>月度时为 1–12；年度时为 0。</summary>
    public required int ReportingMonth { get; init; }

    public required int TotalFood { get; init; }

    public required int TotalMoney { get; init; }

    public required int ExpenseMoney { get; init; }

    public required int ArmyMaintenanceMoney { get; init; }

    public required int TreasuryMoney { get; init; }

    public required int TreasuryFood { get; init; }

    public required IReadOnlyList<StrategyTributeLineDto> TributeLines { get; init; }
}

/// <summary>单条贡纳到账记录。</summary>
public sealed record StrategyTributeLineDto
{
    public required string OriginName { get; init; }

    public required int Food { get; init; }

    public required int Money { get; init; }
}
