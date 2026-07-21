using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>全情报：单位 DTO 不做掩码（调试/Custom Full）。</summary>
public sealed class FullIntelPolicy : IIntelPolicy
{
    public StrategyUnitStateDto ApplyUnitIntelMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells)
        => unit;
}

/// <summary>
/// ForceIntel 难度下不再因「进入视野」自动模糊兵数；
/// 非自势力具体数值改由 <see cref="EspionageIntelRules"/> + 谍报台账控制。
/// </summary>
public sealed class ForceIntelPolicy : IIntelPolicy
{
    public StrategyUnitStateDto ApplyUnitIntelMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells)
        => unit;
}

/// <summary>情报档位换算（士气/训练/兵数模糊显示）。</summary>
public static class StrategyIntelMaskRules
{
    public static string MaskSoldierCount(int soldiers, bool heightAdvantage)
    {
        if (soldiers <= 0)
            return "0";

        var text = soldiers.ToString();
        return heightAdvantage && text.Length > 0
            ? $"{text[0]}{new string('*', Math.Max(0, text.Length - 1))}"
            : "****";
    }

    public static string MaskMoraleBand(int morale)
        => morale >= 70 ? "高" : morale >= 40 ? "中" : "低";

    public static string MaskTrainingBand(int training)
        => training >= 70 ? "高" : training >= 40 ? "中" : "低";
}
