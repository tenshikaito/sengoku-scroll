namespace SengokuScroll.Strategy.Models;

/// <summary>和谈条款输入。</summary>
public sealed record StrategyPeaceTermsDto
{
    public IReadOnlyList<int> CededStrongholdIds { get; init; } = [];

    public int ReparationsMoney { get; init; }

    public bool DemandOuterVassalage { get; init; }
}

/// <summary>和谈预览：战争分数、条款成本与预计接受率。</summary>
public sealed record StrategyPeaceSettlementPreviewDto
{
    public required int WarId { get; init; }

    public required int ProposerWarScore { get; init; }

    public required int RequiredWarScore { get; init; }

    public required int AcceptanceChancePercent { get; init; }

    public required bool CanForceAcceptance { get; init; }

    public required bool IsWhitePeace { get; init; }

    public required IReadOnlyList<StrategyPeaceTermCostDto> TermCosts { get; init; }
}

public sealed record StrategyPeaceTermCostDto
{
    public required string Kind { get; init; }

    public required string Label { get; init; }

    public required int WarScoreCost { get; init; }
}
