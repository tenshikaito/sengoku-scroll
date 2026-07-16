using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Actions;

/// <summary>单位级经济单步变更（日耗粮等）。</summary>
public static class UnitEconomyActions
{
    /// <summary>
    /// 按兵数扣除单位携行粮；不足时扣至 0（M1-d 占位，断粮逃兵 M3）。
    /// </summary>
    /// <param name="unit">出征军事单位。</param>
    /// <returns>本日实际扣除的合数。</returns>
    public static int ApplyDailyFoodConsumption(Unit unit)
    {
        var consumption = LogisticsCalculator.CalculateUnitDailyFoodConsumption(unit.Soldier);
        // 业务：携行粮不足时扣至 0，断粮逃兵由后续系统处理
        var deducted = Math.Min(unit.Food, consumption);
        unit.Food -= deducted;
        return deducted;
    }
}
