using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式战斗结算：日推进时执行已下达的攻击命令（M3-b）。</summary>
public interface IStrategyBattleResolutionSystem : IGameSystem
{
}

/// <summary>
/// 在单位移动之后结算「攻击中」姿态的单位。
/// </summary>
/// <remarks>
/// <para>业务规则：</para>
/// <list type="number">
///   <item>攻击命令在玩家确认后仅排队，日推进时才真正接敌。</item>
///   <item>单方攻击：下达方=攻方。</item>
///   <item>双方互攻：AP 高者先行动并担任攻方（见 <see cref="BattleEngagementResolver"/>）。</item>
///   <item>结算后双方各派战报信使，自<strong>己方参战部队</strong>所在格向当主回程。</item>
///   <item>战报信使在结算当日不移动，次日随信使系统出发（Order 在信使系统之后）。</item>
/// </list>
/// </remarks>
public sealed class StrategyBattleResolutionSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    MessengerDispatchHelper messengerDispatchHelper,
    GameRuleConfig rules) : IStrategyBattleResolutionSystem
{
    public int Order { get; } = 26;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var challengers = context.GameWorldContext.EachUnit()
            .Where(u => u.Stance == UnitStance.Attacking && u.ActionTarget.UnitId > 0)
            .ToList();

        var processedPairs = new HashSet<(int, int)>();

        foreach (var challenger in challengers)
        {
            var defenderId = challenger.ActionTarget.UnitId;
            var pairKey = (Math.Min(challenger.Id, defenderId), Math.Max(challenger.Id, defenderId));
            if (processedPairs.Contains(pairKey))
                continue;

            if (!gameData.Units.TryGetValue(defenderId, out var defender))
            {
                ClearAttackOrder(challenger);
                continue;
            }

            if (!IsAdjacent(challenger.Location, defender.Location))
            {
                ClearAttackOrder(challenger);
                continue;
            }

            processedPairs.Add(pairKey);

            var mutualAttack = defender.Stance == UnitStance.Attacking
                && defender.ActionTarget.UnitId == challenger.Id;

            var (attacker, defenderRole, bothOrdered) = BattleEngagementResolver.ResolveRoles(
                challenger,
                defender,
                aOrderedAttackOnB: true,
                bOrderedAttackOnA: mutualAttack);

            ResolveEngagement(attacker, defenderRole, bothOrdered, gameData);
            ClearAttackOrder(challenger);
            ClearAttackOrder(defender);
        }
    }

    /// <summary>执行一次野战结算并写入日结果缓冲。</summary>
    private void ResolveEngagement(Unit attacker, Unit defender, bool bothOrderedAttack, GameData gameData)
    {
        var date = gameData.GameDate;
        var target = (Point2)defender.Location;
        var seed = InstantBattleCalculator.ComputeResolutionSeed(
            date,
            attacker.Id,
            defender.Id,
            target.X,
            target.Y);
        var outcome = InstantBattleCalculator.Resolve(attacker, defender, seed);

        UnitBattleActions.ApplyCasualties(attacker, outcome.AttackerCasualties);
        UnitBattleActions.ApplyCasualties(defender, outcome.DefenderCasualties);
        UnitBattleActions.MarkAttacked(attacker, rules);

        dayOutcomeBuffer.AddBattle(new StrategyBattleResultDto
        {
            AttackerWon = outcome.AttackerWon,
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
            AttackerSoldiersBefore = outcome.AttackerSoldiersBefore,
            DefenderSoldiersBefore = outcome.DefenderSoldiersBefore,
            AttackerCasualties = outcome.AttackerCasualties,
            DefenderCasualties = outcome.DefenderCasualties,
            AttackerSoldiersAfter = attacker.Soldier,
            DefenderSoldiersAfter = defender.Soldier,
            AttackerWinRatePercent = outcome.AttackerWinRatePercent,
            ResolutionSeed = outcome.ResolutionSeed,
            ResolutionRoll = outcome.ResolutionRoll,
            LogEntries = InstantBattleCalculator.BuildBattleLog(
                attacker,
                defender,
                outcome,
                bothOrderedAttack,
                attacker.Ap >= defender.Ap)
        });

        DispatchBattleReports(attacker, defender, gameData);
    }

    private void DispatchBattleReports(Unit attacker, Unit defender, GameData gameData)
    {
        DispatchForForce(attacker.ForceId, attacker.Location, gameData);
        if (defender.ForceId != attacker.ForceId)
            DispatchForForce(defender.ForceId, defender.Location, gameData);
    }

    private void DispatchForForce(int forceId, Point3 origin, GameData gameData)
    {
        var meta = forceId == scenarioMeta.PlayerForceId
            ? scenarioMeta
            : BuildForceMeta(forceId, gameData);

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, meta);
        var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, meta, lordLocation);

        messengerDispatchHelper.DispatchBattleReport(
            origin,
            forceId,
            strongholdId,
            lordLocation);
    }

    private static StrategyScenarioMeta BuildForceMeta(int forceId, GameData gameData)
    {
        var stronghold = gameData.Strongholds.Values
            .Where(s => s.ForceId == forceId)
            .OrderBy(s => s.Id)
            .FirstOrDefault();

        return new StrategyScenarioMeta
        {
            PlayerForceId = forceId,
            LordStrongholdId = stronghold?.Id,
            LordName = stronghold?.Name ?? "当主"
        };
    }

    private static bool IsAdjacent(Point3 a, Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    private static void ClearAttackOrder(Unit unit)
    {
        unit.Stance = UnitStance.Normal;
        unit.ActionTarget.UnitId = 0;
    }
}
