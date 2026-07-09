namespace SengokuScroll.Strategy.Constants;

/// <summary>运输队自动派遣相关阈值（M1-c）。</summary>
public static class SupplyDispatchConstants
{
    /// <summary>单位粮低于此值（合）时触发自动补给评估。</summary>
    public const int UnitFoodThresholdGo = 500;

    /// <summary>据点至少需有这么多粮（合）才允许出库派遣。</summary>
    public const int StrongholdMinFoodGo = 1000;
}
