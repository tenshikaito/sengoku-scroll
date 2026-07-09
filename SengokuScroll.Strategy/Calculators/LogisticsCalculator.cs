using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>
/// 后勤相关的纯计算公式（不修改游戏状态）。
/// </summary>
public static class LogisticsCalculator
{
    /// <summary>
    /// 计算运输队在途一日的自耗粮（合）：人夫与护卫口粮从载粮中扣除。
    /// </summary>
    /// <param name="porterCount">人夫数量。</param>
    /// <param name="escortSoldierCount">护卫兵数量。</param>
    /// <returns>一日消耗合数（向上取整）。</returns>
    public static int CalculateDailyTransitConsumption(int porterCount, int escortSoldierCount)
    {
        var porterCost = porterCount * LogisticsConstants.PorterDailyRationGo;
        var escortCost = escortSoldierCount * LogisticsConstants.DailySoldierRationGo;
        return (int)Math.Ceiling(porterCost + escortCost);
    }

    /// <summary>
    /// 计算前线军事单位一日的粮耗（合）。
    /// </summary>
    /// <param name="soldierCount">兵士数量。</param>
    /// <returns>一日消耗合数（向上取整）。</returns>
    public static int CalculateUnitDailyFoodConsumption(int soldierCount)
        => (int)Math.Ceiling(soldierCount * LogisticsConstants.DailySoldierRationGo);
}
