using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>
/// 后勤相关的纯计算公式（不修改游戏状态）；全部整型运算。
/// </summary>
public static class LogisticsCalculator
{
    /// <summary>
    /// 计算运输队在途一日的自耗粮（合）：人夫与护卫口粮从载粮中扣除。
    /// </summary>
    public static int CalculateDailyTransitConsumption(int porterCount, int escortSoldierCount)
    {
        var porterCost = porterCount * LogisticsConstants.PorterDailyRationMilliGo;
        var escortCost = escortSoldierCount * LogisticsConstants.DailySoldierRationMilliGo;
        return CeilMilliGoToGo(porterCost + escortCost);
    }

    /// <summary>计算军事单位一日粮耗（合）。</summary>
    public static int CalculateUnitDailyFoodConsumption(int soldierCount)
        => CeilMilliGoToGo(soldierCount * LogisticsConstants.DailySoldierRationMilliGo);

    /// <summary>计算市民聚合一日粮耗（合）。</summary>
    public static int CalculateCivilianDailyFoodConsumption(int population)
        => population <= 0
            ? 0
            : CeilMilliGoToGo(population * LogisticsConstants.DailyCivilianRationMilliGo);

    /// <summary>毫合向上取整为合（后勤最小计量单位）。</summary>
    public static int CeilMilliGoToGo(int milliGo)
        => (milliGo + 999) / 1000;
}
