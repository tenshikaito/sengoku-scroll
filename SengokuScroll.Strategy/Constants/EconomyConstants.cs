namespace SengokuScroll.Strategy.Constants;

/// <summary>经济结算整型常数（M4-a）。比例一律用万分比（10000 = 100%）。</summary>
public static class EconomyConstants
{
    /// <summary>100% 对应万分比。</summary>
    public const int BasisPointsPer100Percent = 10_000;

    /// <summary>每月天数（日产折算分母）。</summary>
    public const int DaysPerMonth = 30;

    /// <summary>市民默认日口粮（毫合/人/日）；2000 = 2 合。</summary>
    public const int DailyCivilianRationMilliGo = 2000;

    /// <summary>征收效率下限（万分比）；3000 = 30%。</summary>
    public const int MinCollectionEfficiencyBp = 3000;

    /// <summary>非史实据点收入惩罚（万分比）；7000 = 70%。</summary>
    public const int FictionalIncomePenaltyBp = 7000;

    /// <summary>每点商业值允许的开店数线性除数（MaxShops = CommerceValue / K）。</summary>
    public const int CommerceValuePerShopSlot = 1000;

    /// <summary>市场日 K 线保留天数（约 2 年）。</summary>
    public const int MarketPriceHistoryRetentionDays = 730;
}
