using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 野战/攻城结算后的方针、追击、败方重整与据点占领。
/// 败方不强制后撤一格：仅设 Retreat 方针，次日由 AI/玩家自行撤离。
/// </summary>
public sealed class BattleAftermathHelper(
    IGameContext context,
    StrongholdCaptureHelper captureHelper,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    BattleReportDeliveryHelper reportDelivery,
    IPathfindingService pathfinding)
{
    private string? captureBattleNote;

    /// <summary>决战战报可追加的占城说明（消费后清空）。</summary>
    public string? ConsumeCaptureBattleNote()
    {
        var note = captureBattleNote;
        captureBattleNote = null;
        return note;
    }

    /// <summary>应用决战/攻城战果：胜方追击、败方撤退、攻城破城占点。</summary>
    public void Apply(
        Unit attacker,
        Unit defender,
        InstantBattleOutcome outcome,
        BattleEngagementKind engagementKind = BattleEngagementKind.FieldBattle)
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        if (outcome.IsSurrendered)
        {
            ApplySurrender(attacker, defender, engagementKind);
            return;
        }

        if (engagementKind == BattleEngagementKind.Siege
            && SiegeBattleRules.IsGarrisonBroken(defender)
            && SiegeBattleRules.ResolveDefenderStronghold(defender, gameData) is { } siegeStronghold)
        {
            if (StrongholdCaptureRules.HasActiveSiegeOrder(attacker, siegeStronghold)
                && StrongholdCaptureRules.IsStrongholdDefenseBroken(siegeStronghold, gameData))
            {
                if (attacker.SiegeMode == UnitSiegeMode.Assault
                    && !attacker.Location.IsSameTile(siegeStronghold.Location))
                {
                    MapLocationActions.SetUnitLocation(
                        context.GameWorldContext, attacker, siegeStronghold.Location);
                }

                if (captureHelper.TryCaptureAfterGarrisonBroken(attacker, defender, gameData))
                    captureBattleNote = $"🏯 {siegeStronghold.Name} 守备崩溃，据点陷落。";
                CloseUnitBattlefield(defender, gameData);
                ApplyLoserDefeat(
                    defender, attacker, gameData, outcome.DefenderSoldiersBefore);
                BattleMoraleRules.ApplyBattleOutcome(attacker, defender, decisiveVictory: true);
                return;
            }
        }

        // 业务：攻方胜则败方撤退、胜方占格/追击；守方胜则对称处理
        if (outcome.AttackerWon)
        {
            TryAdvanceWinnerIntoStrongholdAfterGarrisonFight(attacker, defender, gameData);
            ApplyLoserDefeat(
                defender, attacker, gameData, outcome.DefenderSoldiersBefore);
            ApplyWinnerVictory(attacker, defender, gameData);
            BattleMoraleRules.ApplyBattleOutcome(attacker, defender, decisiveVictory: true);
        }
        else
        {
            ApplyLoserDefeat(
                attacker, defender, gameData, outcome.AttackerSoldiersBefore);
            ApplyWinnerVictory(defender, attacker, gameData);
            BattleMoraleRules.ApplyBattleOutcome(defender, attacker, decisiveVictory: true);
        }
    }

    /// <summary>劝降成功：攻方收编部分残部；降方撤出地图并结束战场；若在敌城格则自动强攻/占城。</summary>
    public void ApplySurrender(
        Unit winner,
        Unit loser,
        BattleEngagementKind engagementKind = BattleEngagementKind.FieldBattle)
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var strongholdAtLoser = SiegeBattleRules.ResolveDefenderStronghold(loser, gameData)
            ?? gameData.Strongholds.Values.FirstOrDefault(s => s.Location.IsSameTile(loser.Location));

        // 业务：结束双方所属战场容器
        CloseUnitBattlefield(winner, gameData);
        CloseUnitBattlefield(loser, gameData);

        winner.Stance = UnitStance.Normal;
        winner.ActionTarget.UnitId = 0;
        winner.Directive = UnitDirective.Occupy;
        winner.Status = UnitStatus.Waiting;
        winner.Morale = (byte)Math.Min(100, winner.Morale + 6);

        // 业务：收编约三成残部入攻方，其余解除武装离场（降伏不再留地图单位）
        var remnant = Math.Max(0, loser.Soldier);
        var absorbed = remnant * 30 / 100;
        if (absorbed > 0)
            winner.Soldier += absorbed;

        loser.Soldier = 0;
        loser.Stance = UnitStance.Normal;
        loser.ActionTarget.UnitId = 0;
        loser.ActionTarget.RoutePoints.Clear();
        BattlefieldEngagementRules.LeaveBattlefield(loser);

        UnitDestructionRules.ResolveAnnihilatedUnit(
            context.GameWorldContext,
            loser,
            winner,
            gameData,
            scenarioMeta,
            dayOutcomeBuffer,
            reportDelivery,
            pathfinding);

        // 业务：胜方仍在敌城格时自动下达强攻令，城防已垮则占城
        TryAutoSiegeOrCaptureAfterVictory(winner, gameData, engagementKind, strongholdAtLoser);
    }

    private static void CloseUnitBattlefield(Unit unit, GameData gameData)
    {
        if (unit.BattlefieldId <= 0)
            return;

        if (gameData.Battlefields.TryGetValue(unit.BattlefieldId, out var bf) && !bf.IsClosed)
            BattlefieldContainerRules.CloseBattlefield(bf, gameData);
        else
            BattlefieldEngagementRules.LeaveBattlefield(unit);
    }

    /// <summary>战后仍站在敌方据点格：强制强攻令并尝试占城或接敌剩余城防。</summary>
    private void TryAutoSiegeOrCaptureAfterVictory(
        Unit winner,
        GameData gameData,
        BattleEngagementKind engagementKind,
        Stronghold? preferredStronghold)
    {
        var stronghold = preferredStronghold
            ?? gameData.Strongholds.Values.FirstOrDefault(s => s.Location.IsSameTile(winner.Location));

        if (stronghold is null || stronghold.ForceId == winner.ForceId)
            return;

        if (!gameData.Forces.TryGetValue(winner.ForceId, out var wf)
            || !gameData.Forces.TryGetValue(stronghold.ForceId, out var sf)
            || (!DiplomacyRules.IsEnemy(wf, sf).IsSuccess
                && !WarRules.AreWarEnemies(winner.ForceId, stronghold.ForceId, gameData)))
            return;

        WarRules.EnsureWarBetween(gameData, winner.ForceId, stronghold.ForceId, gameData.GameDate);

        winner.Directive = UnitDirective.Occupy;
        winner.SiegeMode = UnitSiegeMode.Assault;
        winner.ActionTarget.StrongholdId = stronghold.Id;
        winner.DirectiveTargetId = stronghold.Id;

        BattlefieldContainerRules.EnsureSiegeBattlefield(winner, stronghold, gameData);

        if (StrongholdCaptureRules.IsStrongholdDefenseBroken(stronghold, gameData))
        {
            if (captureHelper.CaptureStronghold(winner, stronghold, stronghold.ForceId, gameData)
                || captureHelper.TryCaptureVacantStronghold(winner, gameData))
            {
                captureBattleNote = $"🏯 {stronghold.Name} 守军降伏，据点陷落。";
            }

            return;
        }

        // 业务：城内仍有数字驻军且非笼城时才编组地图守军接敌
        if (!GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(stronghold, gameData, scenarioMeta))
        {
            var garrison = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData)
                ?? StrongholdGarrisonActions.EnsureDefenderUnit(
                    context.GameWorldContext, stronghold, gameData, scenarioMeta);

            if (garrison is not null && BattleFactorEvaluator.CanUnitEngage(winner))
            {
                winner.Stance = UnitStance.Attacking;
                UnitBattleActions.QueueAttack(winner, garrison.Id);
            }
        }
    }

    /// <summary>战术战中除主将外兵力归零的驰援部队：溃灭处理与战报事件。</summary>
    public void ApplyAnnihilatedTacticalParticipants(
        TacticalBattleResult tactical,
        Unit primaryAttacker,
        Unit primaryDefender,
        GameData gameData)
    {
        var victor = tactical.Outcome.AttackerWon ? primaryAttacker : primaryDefender;
        if (victor.Soldier <= 0)
        {
            var alt = tactical.Outcome.AttackerWon ? primaryDefender : primaryAttacker;
            victor = alt.Soldier > 0 ? alt : null!;
        }

        foreach (var unitId in tactical.CasualtiesByUnitId.Keys)
        {
            if (unitId == primaryAttacker.Id || unitId == primaryDefender.Id)
                continue;

            if (!gameData.Units.TryGetValue(unitId, out var unit) || unit.Soldier > 0)
                continue;

            UnitDestructionRules.ResolveAnnihilatedUnit(
                context.GameWorldContext,
                unit,
                victor is { Soldier: > 0 } v ? v : null,
                gameData,
                scenarioMeta,
                dayOutcomeBuffer,
                reportDelivery,
                pathfinding);
        }
    }

    private void ApplyLoserDefeat(Unit loser, Unit winner, GameData gameData, int soldiersBeforeBattle = 0)
    {
        // 业务：主动出城的守城单位战败——残部收回城内，不当整建制歼灭
        if (StrongholdGarrisonActions.TryAbsorbDefeatedFieldGarrisonIntoCity(
                context.GameWorldContext,
                loser,
                gameData,
                scenarioMeta.Difficulty,
                soldiersBeforeBattle,
                out var absorbed))
        {
            var refuge = SiegeBattleRules.ResolveDefenderStronghold(loser, gameData);
            if (refuge is not null)
            {
                dayOutcomeBuffer.AddEvent(new StrategyEventDto
                {
                    Category = "UnitFledToStronghold",
                    Brief = $"🏯 {loser.Name} 败退入城",
                    Message =
                        $"{loser.Name} 出城接敌失利，{absorbed} 人撤回 {refuge.Name} 固守。"
                });
            }

            if (winner.ActionTarget.UnitId == loser.Id)
            {
                winner.ActionTarget.UnitId = 0;
                if (winner.Stance == UnitStance.Attacking)
                    winner.Stance = UnitStance.Normal;
            }

            return;
        }

        // 业务：败退保留最低残部（按战前兵力比例），避免「撤退仍必被歼灭」
        if (soldiersBeforeBattle > 0)
        {
            var ratio = StrategyDifficultyRules.DefeatResidualSoldierRatio(scenarioMeta.Difficulty);
            var floor = Math.Max(1, (int)Math.Ceiling(soldiersBeforeBattle * ratio));
            if (loser.Soldier < floor)
                loser.Soldier = floor;
        }

        if (loser.Soldier <= 0)
        {
            UnitDestructionRules.ResolveAnnihilatedUnit(
                context.GameWorldContext,
                loser,
                winner,
                gameData,
                scenarioMeta,
                dayOutcomeBuffer,
                reportDelivery,
                pathfinding);
            return;
        }

        // 业务：邻格有友城且城格未被敌占时，优先逃入城内（残部并入守备）
        if (BattleFleeToStrongholdRules.TryFleeAfterDefeat(
                context.GameWorldContext,
                loser,
                winner,
                gameData,
                dayOutcomeBuffer) is not null)
            return;

        gameData.Characters.TryGetValue(loser.LeaderId, out var commander);
        BattleRetreatRules.ApplyDefeatRetreat(loser, winner, commander, gameData);
    }

    private void ApplyWinnerVictory(Unit winner, Unit loser, GameData gameData)
    {
        if (winner.Soldier <= 0)
            return;

        winner.Stance = UnitStance.Normal;
        winner.ActionTarget.UnitId = 0;
        winner.Directive = UnitDirective.Occupy;

        if (winner.Stance == UnitStance.Surrounding)
            winner.Stance = UnitStance.Normal;

        gameData.Characters.TryGetValue(winner.LeaderId, out var commander);
        var pursue = BattlePursuitRules.ShouldAiPursue(commander?.Personality);

        // 业务：追击在同一 Battlefield / 同格内结算；成功则再攻击，失败则败方离场、胜方修正
        var sameBattlefield = winner.BattlefieldId > 0 && winner.BattlefieldId == loser.BattlefieldId;
        var canPursue = pursue
            && (sameBattlefield || MoveEngagementRules.IsInEngagementRange(winner, loser))
            && loser.Soldier > 0
            && BattleFactorEvaluator.CanUnitEngage(winner);

        if (canPursue)
        {
            UnitBattleActions.QueueAttack(winner, loser.Id);
            return;
        }

        // 业务：不追击或追击条件不足——败方离开战场（Routing），胜方原地修正 1 日（扣 AP）
        if (loser.BattlefieldId > 0)
            BattlefieldContainerRules.LeaveBattlefield(loser);

        winner.Ap = Math.Max(0, winner.Ap - 1);
        winner.Status = UnitStatus.Waiting;
        if (winner.BattlefieldId > 0
            && gameData.Battlefields.TryGetValue(winner.BattlefieldId, out var bf))
        {
            var remainingEnemies = bf.SideAUnitIds.Concat(bf.SideBUnitIds)
                .Where(id => id != winner.Id && gameData.Units.TryGetValue(id, out var u) && u.Soldier > 0)
                .Select(id => gameData.Units[id])
                .Any(u => u.ForceId != winner.ForceId);

            var keepSiegeBattlefield = winner.SiegeMode == UnitSiegeMode.Assault
                && winner.ActionTarget.StrongholdId > 0
                && gameData.Strongholds.TryGetValue(winner.ActionTarget.StrongholdId, out var siegeTarget)
                && winner.Location.IsSameTile(siegeTarget.Location)
                && !StrongholdCaptureRules.IsStrongholdDefenseBroken(siegeTarget, gameData);

            if (!remainingEnemies && !keepSiegeBattlefield)
                BattlefieldContainerRules.CloseBattlefield(bf, gameData);
            else if (keepSiegeBattlefield)
            {
                winner.ActionTarget.UnitId = 0;
                winner.Stance = UnitStance.Normal;
                winner.Status = UnitStatus.Waiting;
            }
        }

        // 业务：胜方站在敌城格时自动转入强攻/占城，避免「站着不攻」
        TryAutoSiegeOrCaptureAfterVictory(winner, gameData, BattleEngagementKind.FieldBattle, preferredStronghold: null);
    }

    /// <summary>
    /// 城下野战击溃据守城格的守军后：胜方进入据点格（在格子里攻城），空城则占城，仍有城内兵则同格接敌。
    /// </summary>
    private void TryAdvanceWinnerIntoStrongholdAfterGarrisonFight(
        Unit winner,
        Unit loser,
        GameData gameData)
    {
        if (!SiegeBattleRules.IsGarrisonBroken(loser))
            return;

        var stronghold = SiegeBattleRules.ResolveDefenderStronghold(loser, gameData);
        if (stronghold is null || stronghold.ForceId != loser.ForceId)
            return;

        if (!StrongholdGarrisonRules.WasGarrisonUnit(loser, stronghold))
            return;

        if (winner.ForceId == stronghold.ForceId)
            return;

        if (winner.Directive is not (UnitDirective.Occupy or UnitDirective.Raid))
            return;

        if (!StrongholdCaptureRules.HasActiveSiegeOrder(winner, stronghold))
            return;

        if (!winner.Location.IsSameTile(stronghold.Location)
            && !winner.Location.IsAdjacent(stronghold.Location))
            return;

        if (winner.SiegeMode == UnitSiegeMode.Assault
            && !winner.Location.IsSameTile(stronghold.Location))
        {
            MapLocationActions.SetUnitLocation(context.GameWorldContext, winner, stronghold.Location);
        }

        TryContinueSiegeOnTile(winner, stronghold, gameData);
    }

    private void TryContinueSiegeOnTile(Unit winner, Stronghold stronghold, GameData gameData)
    {
        if (captureHelper.CaptureStronghold(winner, stronghold, stronghold.ForceId, gameData))
        {
            captureBattleNote = $"🏯 {stronghold.Name} 守备崩溃，据点陷落。";
            return;
        }

        if (StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData) is not null)
            return;

        if (!StrongholdGarrisonRules.HasCityGarrison(stronghold))
            return;

        if (GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(stronghold, gameData, scenarioMeta))
            return;

        var defender = StrongholdGarrisonActions.EnsureDefenderUnit(
            context.GameWorldContext,
            stronghold,
            gameData,
            scenarioMeta);

        if (defender is not null && BattleFactorEvaluator.CanUnitEngage(winner))
            UnitBattleActions.QueueAttack(winner, defender.Id);
    }
}
