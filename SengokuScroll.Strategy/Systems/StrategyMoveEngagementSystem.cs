using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>
/// 单位移动后扫描同格敌军：自动创建 Battlefield、下达接敌命令，
/// 由 <see cref="StrategyBattleResolutionSystem"/> 于同日结算。
/// </summary>
public interface IStrategyMoveEngagementSystem : IGameSystem
{
}

public sealed class StrategyMoveEngagementSystem(
    IGameContext context,
    IStrategyDayDebugLog dayDebugLog) : IStrategyMoveEngagementSystem
{
    public int Order { get; } = 22;

    public void Update()
    {
        var units = context.GameWorldContext.EachUnit()
            .Where(u => u.IsMilitary && u.Soldier > 0)
            .OrderBy(u => u.Id)
            .ToList();

        var processedPairs = new HashSet<(int, int)>();
        var gameData = context.GameWorldContext.GameWorld.GameData;

        for (var i = 0; i < units.Count; i++)
        {
            for (var j = i + 1; j < units.Count; j++)
            {
                var a = units[i];
                var b = units[j];
                var pairKey = (Math.Min(a.Id, b.Id), Math.Max(a.Id, b.Id));

                if (processedPairs.Contains(pairKey))
                    continue;

                var forceA = context.GameWorldContext.GetForce(a);
                var forceB = context.GameWorldContext.GetForce(b);

                if (!MoveEngagementRules.ShouldEngage(a, b, forceA, forceB, gameData))
                    continue;

                processedPairs.Add(pairKey);

                var bf = BattlefieldContainerRules.EnsureFieldBattlefield(a, b, gameData);
                BattlefieldContainerRules.BindEngagementTargets(bf, a, entryFrom: null, gameData);
                BattlefieldContainerRules.BindEngagementTargets(bf, b, entryFrom: null, gameData);

                var aAgg = MoveEngagementRules.IsAggressiveDirective(a.Directive)
                           && a.SiegeMode != UnitSiegeMode.Encircle;
                var bAgg = MoveEngagementRules.IsAggressiveDirective(b.Directive)
                           && b.SiegeMode != UnitSiegeMode.Encircle;

                if (aAgg && bAgg)
                {
                    UnitBattleActions.QueueAttack(a, b.Id);
                    UnitBattleActions.QueueAttack(b, a.Id);
                    LogEngagement(a, b, mutual: true);
                    continue;
                }

                var aggressor = MoveEngagementRules.ResolveSingleAggressor(a, b);
                if (aggressor is null)
                {
                    // 业务：无明确进攻方时由先入格/较小 Id 作挑战者
                    aggressor = a.Id < b.Id ? a : b;
                }

                var defender = aggressor.Id == a.Id ? b : a;
                UnitBattleActions.QueueAttack(aggressor, defender.Id);
                LogEngagement(aggressor, defender, mutual: false);
            }
        }
    }

    private void LogEngagement(Unit aggressor, Unit defender, bool mutual)
    {
        dayDebugLog.LogLocalized(
            "Engage",
            LocalizationKeys.Debug.EngagementQueue,
            aggressor.Name,
            aggressor.Id,
            defender.Name,
            defender.Id,
            mutual);
        // 业务：接敌仅同格触发，记录坐标便于区分「邻格逼近」与「踏入城格」
        dayDebugLog.LogLine(
            "Engage",
            $"同格接敌 @{aggressor.Location.X},{aggressor.Location.Y} | " +
            $"{aggressor.Name}#{aggressor.Id}({aggressor.Directive}) vs " +
            $"{defender.Name}#{defender.Id}({defender.Directive}) | 互攻={mutual}");
    }
}
