namespace SengokuScroll.Strategy.Constants;

/// <summary>移民队常数。</summary>
public static class MigrantConstants
{
    public const int EmigrationPopularFeelingsThreshold = 40;
    public const int EmigrationStabilityThreshold = 40;
    public const int ImmigrationPopularFeelingsThreshold = 70;

    /// <summary>单次迁出人口上限。</summary>
    public const int MaxMigrantsPerConvoy = 800;

    /// <summary>迁出人口占据点人口比例（万分比）。</summary>
    public const int EmigrationPopulationRateBp = 200;

    public const int OriginPopularFeelingsRelief = 2;
    public const int DestinationStabilityPenalty = 2;
}
