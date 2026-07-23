namespace SengokuScroll.Strategy.Data.Models;

/// <summary>某一季作的农忙阶段（月日区间 + 劳力权重）。</summary>
public sealed record CropGrowthPhaseDefinition(
    int CycleIndex,
    int StartMonth,
    int StartDay,
    int EndMonth,
    int EndDay,
    int LaborWeightBp);
