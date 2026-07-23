namespace SengokuScroll.Strategy.Constants;

/// <summary>农业劳力与分季进度常数。</summary>
public static class AgricultureConstants
{
    public const int ProgressBasisPoints = 10_000;

    /// <summary>每名农民可支撑多少人口（务农劳力 = Population / 此值）。</summary>
    public const int PopulationPerFarmer = 2;

    /// <summary>农忙期每日基础进度（万分比，满劳力时）。</summary>
    public const int BaseDailyProgressBp = 110;

    /// <summary>关键期缺劳力时，对该季进度上限的扣减（万分比/日）。</summary>
    public const int CriticalLaborMissCapPenaltyBp = 15;

    /// <summary>关键期劳力权重阈值：达到此值视为关键期。</summary>
    public const int CriticalLaborWeightBp = 8_000;
}
