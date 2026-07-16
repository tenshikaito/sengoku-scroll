using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Localization;
using SengokuScroll.Localization.Abstractions;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
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
/// 相邻接敌后由 <see cref="FieldBattleAutoResolver"/> 判定当日为「对峙」或「决战」并结算。
/// </summary>
public sealed class StrategyBattleResolutionSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyFieldEngagementRegistry engagementRegistry,
    MessengerDispatchHelper messengerDispatchHelper,
    BattleReportDeliveryHelper battleReportDeliveryHelper,
    BattleAftermathHelper aftermathHelper,
    IStrategyDayDebugLog dayDebugLog,
    ITextLocalizer localizer,
    GameRuleConfig rules) : IStrategyBattleResolutionSystem
{
    public int Order { get; } = 26;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        // 阶段1：清理已脱离接敌范围的战场登记
        engagementRegistry.PruneNonAdjacent(gameData);

        // 阶段2：收集已下达攻击命令或处于对峙的单位
        var challengers = context.GameWorldContext.EachUnit()
            .Where(u => u.ActionTarget.UnitId > 0
                        && (u.Stance == UnitStance.Attacking || u.Status == UnitStatus.Standoff))
            .ToList();

        var processedPairs = new HashSet<(int, int)>();

        // 阶段3：逐对结算接敌战斗（对峙延续或决战/劝降）
        foreach (var challenger in challengers)
        {
            var defenderId = challenger.ActionTarget.UnitId;
            var pairKey = (Math.Min(challenger.Id, defenderId), Math.Max(challenger.Id, defenderId));
            if (processedPairs.Contains(pairKey))
                continue;

            if (!gameData.Units.TryGetValue(defenderId, out var defender) || defender.Soldier <= 0)
            {
                ClearAttackOrder(challenger);
                BattlefieldEngagementRules.LeaveBattlefield(challenger);
                continue;
            }

            if (!MoveEngagementRules.IsInEngagementRange(challenger, defender))
            {
                BattlefieldEngagementRules.LeaveBattlefield(challenger);
                continue;
            }

            processedPairs.Add(pairKey);

            // 业务：双方互下攻击令时各自为攻方，否则由规则判定单方进攻
            var mutualAttack = defender.Stance == UnitStance.Attacking
                && defender.ActionTarget.UnitId == challenger.Id;

            var (roleAttacker, roleDefender, bothOrdered) = BattleEngagementResolver.ResolveRoles(
                challenger,
                defender,
                aOrderedAttackOnB: true,
                bOrderedAttackOnA: mutualAttack);

            var standoffBefore = engagementRegistry.GetStandoffDays(roleAttacker.Id, roleDefender.Id);
            var mapMaster = context.GameWorldContext.GameWorld.GameMapMasterData;
            // 业务：追击撤退中的敌军视为追击接敌
            var isPursuitEngagement = challenger.Stance == UnitStance.Attacking
                && challenger.ActionTarget.UnitId == defenderId
                && defender.Directive == UnitDirective.Retreat;
            var dayResult = FieldBattleAutoResolver.ResolveDailyEngagement(
                gameData.GameDate,
                roleAttacker,
                roleDefender,
                standoffBefore,
                gameData,
                mapMaster,
                isPursuitEngagement,
                bothOrdered);

            if (dayResult.Kind == FieldBattleAutoResolver.FieldBattleDayKind.Standoff)
            {
                // 业务：对峙日仅维持战场状态，特定日数推送对峙战报
                engagementRegistry.SetStandoffDays(
                    roleAttacker.Id,
                    roleDefender.Id,
                    dayResult.StandoffDays);

                SyncBattlefieldStandoffDays(roleAttacker, roleDefender, dayResult.StandoffDays, gameData);

                BattlefieldEngagementRules.MaintainStandoff(roleAttacker, roleDefender.Id);
                BattlefieldEngagementRules.MaintainStandoff(roleDefender, roleAttacker.Id);

                if (BattleConstants.IsStandoffReportDay(dayResult.StandoffDays))
                    DispatchStandoffReports(roleAttacker, roleDefender, dayResult.StandoffDays, gameData);

                dayDebugLog.LogLocalized(
                    "Battle",
                    LocalizationKeys.Debug.BattleStandoff,
                    roleAttacker.Name,
                    roleDefender.Name,
                    dayResult.StandoffDays);

                continue;
            }

            // 业务：决战/劝降后清除攻击命令并应用战果
            engagementRegistry.ClearStandoff(roleAttacker.Id, roleDefender.Id);
            ClearAttackOrder(challenger);
            ClearAttackOrder(defender);

            if (dayResult.Kind == FieldBattleAutoResolver.FieldBattleDayKind.Surrender)
                ApplySurrenderBattle(dayResult, gameData);
            else
                ApplyDecisiveBattle(dayResult, bothOrdered, gameData);
        }
    }

    private void ApplySurrenderBattle(
        FieldBattleAutoResolver.FieldBattleDayResult dayResult,
        GameData gameData)
    {
        if (dayResult.Outcome is not { } outcome)
            return;

        var attacker = dayResult.CommittedAggressor;
        var defender = dayResult.CommittedDefender;

        UnitBattleActions.MarkAttacked(attacker, rules);
        aftermathHelper.ApplySurrender(attacker, defender, dayResult.EngagementKind);

        var log = new List<StrategyBattleLogEntryDto>
        {
            new()
            {
                Order = 1,
                Side = "system",
                Phase = "劝降",
                Message = dayResult.CommitReason ?? $"{attacker.Name} 向 {defender.Name} 劝降成功。"
            },
            new()
            {
                Order = 2,
                Side = "attacker",
                Phase = "收编",
                Message = $"{attacker.Name} 兵不血刃收服敌军，己方无伤亡。"
            },
            new()
            {
                Order = 3,
                Side = "defender",
                Phase = "降伏",
                Message = $"{defender.Name} 放下武器，部队解散离场（残部收编）。"
            }
        };

        var battleDto = new StrategyBattleResultDto
        {
            AttackerWon = true,
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            AttackerForceId = attacker.ForceId,
            DefenderForceId = defender.ForceId,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
            AttackerSoldiersBefore = outcome.AttackerSoldiersBefore,
            DefenderSoldiersBefore = outcome.DefenderSoldiersBefore,
            AttackerCasualties = 0,
            DefenderCasualties = 0,
            AttackerSoldiersAfter = attacker.Soldier,
            DefenderSoldiersAfter = defender.Soldier,
            AttackerWinRatePercent = outcome.AttackerWinRatePercent,
            ResolutionSeed = outcome.ResolutionSeed,
            ResolutionRoll = outcome.ResolutionRoll,
            EngagementKind = dayResult.EngagementKind.ToString(),
            LogEntries = log,
            FactorNotes = [],
            IsSurrendered = true
        };

        DispatchBattleReports(attacker, defender, outcome, gameData, battleDto);
        LogBattleResolved(dayResult, outcome, surrender: true);
    }

    private void ApplyDecisiveBattle(
        FieldBattleAutoResolver.FieldBattleDayResult dayResult,
        bool bothOrderedAttack,
        GameData gameData)
    {
        if (dayResult.Outcome is not { } outcome)
            return;

        var attacker = dayResult.CommittedAggressor;
        var defender = dayResult.CommittedDefender;

        // 业务：战术模拟有结果时走子队伤亡分摊，否则走瞬间战估算伤亡
        if (dayResult.TacticalResult is { } tactical)
        {
            if (tactical.IsSurrounded)
            {
                defender.Status = UnitStatus.BeingSurround;
                attacker.Stance = UnitStance.Surrounding;
            }

            BattleCasualtyRules.ApplyCasualtiesToWorld(tactical, gameData, scenarioMeta.Difficulty);
            outcome = BattleCasualtyRules.CapOutcome(tactical.Outcome, scenarioMeta.Difficulty);
        }
        else
        {
            outcome = BattleCasualtyRules.CapOutcome(outcome, scenarioMeta.Difficulty);
            UnitBattleActions.ApplyCasualties(attacker, outcome.AttackerCasualties, gameData);
            UnitBattleActions.ApplyCasualties(defender, outcome.DefenderCasualties, gameData);
        }

        UnitBattleActions.MarkAttacked(attacker, rules);
        aftermathHelper.Apply(attacker, defender, outcome, dayResult.EngagementKind);

        if (dayResult.TacticalResult is { } tacticalAftermath)
            aftermathHelper.ApplyAnnihilatedTacticalParticipants(tacticalAftermath, attacker, defender, gameData);

        var logEntries = dayResult.TacticalResult?.LogEntries
            ?? InstantBattleCalculator.BuildBattleLog(
                attacker,
                defender,
                outcome,
                bothOrderedAttack,
                attacker.Movement >= defender.Movement,
                dayResult.CommitReason);

        if (aftermathHelper.ConsumeCaptureBattleNote() is { } captureNote)
        {
            logEntries = new List<StrategyBattleLogEntryDto>(logEntries)
            {
                new()
                {
                    Order = logEntries.Count + 1,
                    Side = "system",
                    Phase = "占城",
                    Message = captureNote
                }
            };
        }

        var battleDto = new StrategyBattleResultDto
        {
            AttackerWon = outcome.AttackerWon,
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            AttackerForceId = attacker.ForceId,
            DefenderForceId = defender.ForceId,
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
            EngagementKind = dayResult.EngagementKind.ToString(),
            LogEntries = logEntries,
            FactorNotes = [],
            AttackerReinforcementNames = ResolveReinforcementNames(
                dayResult.TacticalResult?.AttackerParticipantUnitIds, attacker.Id, gameData),
            DefenderReinforcementNames = ResolveReinforcementNames(
                dayResult.TacticalResult?.DefenderParticipantUnitIds, defender.Id, gameData),
            IsSurrendered = false
        };

        DispatchBattleReports(attacker, defender, outcome, gameData, battleDto, dayResult.TacticalResult);
        LogBattleResolved(dayResult, outcome, surrender: false);
    }

    private void LogBattleResolved(
        FieldBattleAutoResolver.FieldBattleDayResult dayResult,
        InstantBattleOutcome outcome,
        bool surrender)
    {
        var attacker = dayResult.CommittedAggressor;
        var defender = dayResult.CommittedDefender;
        var kindLabel = BattleEngagementClassifier.ToDisplayLabel(dayResult.EngagementKind, localizer);
        var outcomeLabel = surrender
            ? localizer.GetString(LocalizationKeys.Debug.BattleOutcomeSurrender)
            : outcome.AttackerWon
                ? localizer.GetString(LocalizationKeys.Debug.BattleOutcomeAttackerWin)
                : localizer.GetString(LocalizationKeys.Debug.BattleOutcomeDefenderWin);

        dayDebugLog.LogLocalized(
            "Battle",
            LocalizationKeys.Debug.BattleResolve,
            kindLabel,
            attacker.Name,
            defender.Name,
            outcomeLabel,
            surrender ? 0 : outcome.AttackerCasualties,
            surrender ? 0 : outcome.DefenderCasualties);
    }

    private void DispatchStandoffReports(Unit unitA, Unit unitB, int standoffDays, GameData gameData)
    {
        var message =
            $"⚔ {unitA.Name} 与 {unitB.Name} 大军对峙第 {standoffDays} 日，战线僵持未决。";

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "StandoffReport",
            Message = message
        });

        DispatchStandoffForForce(unitA.ForceId, unitA.Location, gameData, unitA, unitB);
        if (unitB.ForceId != unitA.ForceId)
            DispatchStandoffForForce(unitB.ForceId, unitB.Location, gameData, unitA, unitB);
    }

    private void DispatchStandoffForForce(
        int forceId,
        Point3 origin,
        GameData gameData,
        Unit unitA,
        Unit unitB)
    {
        if (!BattleReportDispatchRules.ShouldDispatchStandoffReport(
                forceId, unitA, unitB, scenarioMeta.PlayerForceId, gameData))
            return;

        var meta = forceId == scenarioMeta.PlayerForceId
            ? scenarioMeta
            : BuildForceMeta(forceId, gameData);

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, meta);
        var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, meta, lordLocation);

        messengerDispatchHelper.DispatchBattleReport(origin, forceId, strongholdId, lordLocation);
    }

    private void DispatchBattleReports(
        Unit attacker,
        Unit defender,
        InstantBattleOutcome outcome,
        GameData gameData,
        StrategyBattleResultDto battleResult,
        TacticalBattleResult? tactical = null)
    {
        var attackerParticipants = tactical?.AttackerParticipantUnitIds;
        var defenderParticipants = tactical?.DefenderParticipantUnitIds;

        battleReportDeliveryHelper.DeliverDecisiveBattleReport(
            attacker.ForceId,
            attacker.Location,
            gameData,
            outcome,
            attacker,
            defender,
            battleResult,
            attackerParticipants,
            defenderParticipants);

        if (defender.ForceId != attacker.ForceId)
        {
            battleReportDeliveryHelper.DeliverDecisiveBattleReport(
                defender.ForceId,
                defender.Location,
                gameData,
                outcome,
                attacker,
                defender,
                battleResult,
                attackerParticipants,
                defenderParticipants);
        }
    }

    private static IReadOnlyList<string> ResolveReinforcementNames(
        IReadOnlyList<int>? participantIds,
        int primaryId,
        GameData gameData)
    {
        if (participantIds is null)
            return [];

        return participantIds
            .Where(id => id != primaryId && gameData.Units.TryGetValue(id, out _))
            .Select(id => gameData.Units[id].Name)
            .ToList();
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

    private static void SyncBattlefieldStandoffDays(
        Unit attacker,
        Unit defender,
        int standoffDays,
        GameData gameData)
    {
        foreach (var unit in new[] { attacker, defender })
        {
            if (unit.BattlefieldId <= 0
                || !gameData.Battlefields.TryGetValue(unit.BattlefieldId, out var battlefield)
                || battlefield.IsClosed)
            {
                continue;
            }

            battlefield.StandoffDays = standoffDays;
        }
    }

    private static void ClearAttackOrder(Unit unit)
        => BattlefieldEngagementRules.LeaveBattlefield(unit);
}
