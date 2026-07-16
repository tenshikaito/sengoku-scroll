namespace SengokuScroll.Strategy.Data.Models;

/// <summary>Region 收粮事件（月/日 + 产出份额万分比）。</summary>
public sealed record HarvestEventDefinition(int Month, int Day, int ShareBasisPoints);

/// <summary>Region 作型与收粮日历。</summary>
public sealed class RegionHarvestProfile
{
    public required int RegionId { get; init; }

    public required IReadOnlyList<HarvestEventDefinition> Events { get; init; }
}
