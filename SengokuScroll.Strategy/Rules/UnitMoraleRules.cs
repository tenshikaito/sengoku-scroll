using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

/// <summary>单位士气与粮尽崩溃（强制解散）。</summary>
public static class UnitMoraleRules
{
    /// <summary>连续断粮达到此天数且士气归零时强制解散。</summary>
    public const int ForcedDisbandCollapseDays = 14;

    /// <summary>断粮且补给切断时累计崩溃日数；恢复携行粮则清零。</summary>
    public static void ApplyDailySupplyCollapseTracking(Unit unit, GameData gameData)
    {
        if (unit.Food > 0)
        {
            unit.SupplyCollapseDays = 0;
            return;
        }

        if (SupplyStatusEvaluator.EvaluateStatus(unit, gameData) != SupplyStatusEvaluator.CutOff)
            return;

        unit.SupplyCollapseDays++;
        unit.Morale = (byte)Math.Max(0, unit.Morale - 3);
    }

    /// <summary>断粮崩溃且士气归零时触发强制解散（SubUnit 不 recover）。</summary>
    public static void ProcessForcedDisbands(
        IGameWorldContext context,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyDayOutcomeBuffer dayOutcomeBuffer,
        BattleReportDeliveryHelper? reportDelivery = null)
    {
        foreach (var unitId in gameData.Units.Keys.ToList())
        {
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                continue;

            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (unit.Morale > 0 || unit.SupplyCollapseDays < ForcedDisbandCollapseDays)
                continue;

            UnitStrongholdPresenceActions.ForcedDisband(
                context,
                unit,
                gameData,
                meta,
                dayOutcomeBuffer,
                reportDelivery);
        }
    }
}
