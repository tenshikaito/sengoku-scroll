namespace SengokuScroll.Strategy.Constants;

/// <summary>征兵常数。</summary>
public static class RecruitConstants
{
    /// <summary>每名士兵消耗金钱（文）。</summary>
    public const int MoneyCostPerSoldier = 120;

    /// <summary>每名士兵消耗人口。</summary>
    public const int PopulationCostPerSoldier = 2;

    /// <summary>单次征兵上限。</summary>
    public const int MaxRecruitPerOrder = 2000;

    /// <summary>征兵略降民心。</summary>
    public const int PopularFeelingsPenalty = 1;
}
