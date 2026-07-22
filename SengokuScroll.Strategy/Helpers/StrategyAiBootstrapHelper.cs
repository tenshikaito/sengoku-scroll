using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>全势力 AI 观战/仿真模式的启动辅助。</summary>
public static class StrategyAiBootstrapHelper
{
    /// <summary>
    /// 全 AI 模式下：军事单位 Move 且存在敌对目标时预先升为 Occupy，避免首日空转。
    /// </summary>
    public static int BootstrapAggressiveDirectives(GameWorld world, StrategyScenarioMeta meta)
    {
        if (!meta.AllForcesAiControlled)
            return 0;

        var gameData = world.GameData;
        var promoted = 0;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0 || unit.Directive != UnitDirective.Move)
                continue;

            if (!StrategyAiControlRules.IsForceAiControlled(meta, unit.ForceId))
                continue;

            var hostiles = StrategyUnitAIRules.ResolveHostileUnits(unit, gameData);
            var hostileStrongholds = StrategyUnitAIRules.ResolveHostileStrongholds(unit, gameData);
            if (hostiles.Count == 0 && hostileStrongholds.Count == 0)
                continue;

            if (unit.Morale < StrategyUnitAIRules.LowMoraleRetreatThreshold)
                continue;

            unit.Directive = UnitDirective.Occupy;
            promoted++;
        }

        return promoted;
    }
}
