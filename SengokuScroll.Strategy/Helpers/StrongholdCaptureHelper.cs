using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>攻城占领、空城占城与势力存续判定。</summary>
public sealed class StrongholdCaptureHelper(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyForceLordRegistry lordRegistry,
    StrategyWarOccupationRegistry warOccupationRegistry,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    BattleReportDeliveryHelper reportDelivery)
{
    /// <summary>驻军兵数或士气归零后宣告占领。</summary>
    public bool TryCaptureAfterGarrisonBroken(Unit captor, Unit garrison, GameData gameData)
    {
        if (!SiegeBattleRules.IsSiegeEngagement(captor, garrison, gameData)
            && !SiegeBattleRules.IsStrongholdGarrison(garrison, gameData))
            return false;

        if (!SiegeBattleRules.IsGarrisonBroken(garrison))
            return false;

        var stronghold = SiegeBattleRules.ResolveDefenderStronghold(garrison, gameData);
        if (stronghold is null || stronghold.ForceId != garrison.ForceId)
            return false;

        return CaptureStronghold(captor, stronghold, garrison.ForceId, gameData);
    }

    /// <summary>进入无人防守的敌方据点并执行占领。</summary>
    public bool TryCaptureVacantStronghold(Unit unit, GameData gameData)
    {
        if (!unit.IsMilitary || unit.Soldier <= 0)
            return false;

        if (unit.Directive is not (UnitDirective.Occupy or UnitDirective.Raid))
            return false;

        var stronghold = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.Location.X == unit.Location.X
            && s.Location.Y == unit.Location.Y
            && s.ForceId != unit.ForceId);

        if (stronghold is null)
            return false;

        if (!SiegeOrderRules.CanCaptureViaAssaultOrder(unit, stronghold, gameData))
            return false;

        // 业务：仍有敌方 InStronghold/地图守军则不可占空城
        if (gameData.Units.Values.Any(u =>
                u.Id != unit.Id
                && u.IsMilitary
                && u.Soldier > 0
                && u.ForceId == stronghold.ForceId
                && (u.InStronghold && u.LocationStrongholdId == stronghold.Id
                    || u.Location.IsSameTile(stronghold.Location))))
        {
            return false;
        }

        // 业务：城内仍有驻军则不可踩格占城
        if (StrongholdGarrisonRules.IsAnyGarrisonPresent(stronghold, gameData))
            return false;

        return CaptureStronghold(unit, stronghold, stronghold.ForceId, gameData);
    }

    /// <summary>执行据点占领全流程：易手、后果规则、单位归位与势力存续判定。</summary>
    public bool CaptureStronghold(
        Unit captor,
        Stronghold stronghold,
        int previousForceId,
        GameData gameData)
    {
        var defenseSnapshot = StrongholdCaptureRules.DescribeDefenseState(stronghold, gameData);

        if (!StrongholdCaptureRules.CanTransferOwnership(captor, stronghold, gameData, out var rejectReason))
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "CaptureDiagnostic",
                Brief = $"⛔ 占城条件不足：{stronghold.Name}",
                Message =
                    $"{captor.Name} 无法占领 {stronghold.Name}（{rejectReason}）。" +
                    $" siege={captor.SiegeMode} pos=({captor.Location.X},{captor.Location.Y}) [{defenseSnapshot}]"
            });
            return false;
        }

        if (stronghold.ForceId == captor.ForceId)
            return false;

        var strongholdName = stronghold.Name;
        var oldForceName = gameData.Forces.TryGetValue(previousForceId, out var oldForce)
            ? oldForce.Name
            : $"势力{previousForceId}";
        var newForceName = gameData.Forces.TryGetValue(captor.ForceId, out var newForce)
            ? newForce.Name
            : $"势力{captor.ForceId}";

        StrongholdCaptureActions.TransferStrongholdOwnership(
            stronghold,
            captor.ForceId,
            gameData,
            context.GameWorldContext.GameWorld.GameMasterData);

        StrongholdCaptureConsequenceRules.Apply(
            stronghold,
            previousForceId,
            captor.ForceId,
            gameData,
            context.GameWorldContext.GameWorld.GameMasterData,
            scenarioMeta,
            lordRegistry,
            warOccupationRegistry,
            dayOutcomeBuffer,
            gameData.GameDate);

        StrategyWarScoreRules.RecordStrongholdOccupation(
            gameData,
            stronghold,
            captor.ForceId,
            previousForceId);

        captor.Directive = UnitDirective.Occupy;
        captor.Stance = UnitStance.Hold;
        captor.ActionTarget.UnitId = 0;
        captor.ActionTarget.RoutePoints.Clear();
        captor.Status = UnitStatus.Waiting;
        captor.SiegeMode = UnitSiegeMode.None;
        captor.ActionTarget.StrongholdId = 0;
        captor.DirectiveTargetId = stronghold.Id;

        MapLocationActions.SetUnitLocation(context.GameWorldContext, captor, stronghold.Location);

        BattlefieldContainerRules.CloseBattlefieldsForStronghold(stronghold, gameData);
        ReleaseSiegeOrdersAgainst(stronghold, gameData, captor.Id);
        ConsolidateFriendlyOccupiersOnTile(stronghold, captor, gameData);
        BattlefieldContainerRules.PruneOpenBattlefields(gameData);

        var captureEvent = new StrategyEventDto
        {
            Category = "StrongholdCaptured",
            Brief = $"🏯 {strongholdName} 陷落",
            Message =
                $"{newForceName} 占领 {strongholdName}（原属 {oldForceName}）。" +
                $" [{defenseSnapshot}]"
        };

        foreach (var recipient in new[] { previousForceId, captor.ForceId }.Distinct().Order())
        {
            reportDelivery.DeliverPlayerStrategicReport(
                recipient,
                stronghold.Location,
                gameData,
                captureEvent);
        }

        // 业务：占城后若势力无据点且无兵则宣告势力灭亡
        TryResolveForceAfterStrongholdLoss(previousForceId, captor.ForceId, gameData);

        return true;
    }

    private static void ReleaseSiegeOrdersAgainst(Stronghold stronghold, GameData gameData, int captorUnitId)
    {
        foreach (var unit in gameData.Units.Values)
        {
            if (unit.Id == captorUnitId || unit.SiegeMode == UnitSiegeMode.None)
                continue;

            if (unit.ActionTarget.StrongholdId != stronghold.Id)
                continue;

            unit.SiegeMode = UnitSiegeMode.None;
            unit.ActionTarget.StrongholdId = 0;
            unit.ActionTarget.RoutePoints.Clear();
            if (unit.Stance == UnitStance.Surrounding)
                unit.Stance = UnitStance.Normal;
            if (unit.Status == UnitStatus.Moving)
                unit.Status = UnitStatus.Waiting;
        }
    }

    /// <summary>占城后同格友军转为据守姿态，留在城外格地图上（不并入城内驻军）。</summary>
    private static void ConsolidateFriendlyOccupiersOnTile(
        Stronghold stronghold,
        Unit captor,
        GameData gameData)
    {
        foreach (var unit in gameData.Units.Values)
        {
            if (unit.Id == captor.Id || !unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (unit.ForceId != captor.ForceId)
                continue;

            if (!unit.Location.IsSameTile(stronghold.Location))
                continue;

            unit.Directive = UnitDirective.Occupy;
            unit.Stance = UnitStance.Hold;
            unit.ActionTarget.UnitId = 0;
            unit.ActionTarget.RoutePoints.Clear();
            unit.Status = UnitStatus.Waiting;
            unit.SiegeMode = UnitSiegeMode.None;
            unit.ActionTarget.StrongholdId = 0;
            unit.DirectiveTargetId = stronghold.Id;
            unit.BattlefieldId = 0;
            unit.BattlefieldEntryFrom = null;
        }
    }

    private void TryResolveForceAfterStrongholdLoss(int forceId, int conquerorForceId, GameData gameData)
    {
        if (ForceResistanceRules.HasActiveResistance(forceId, gameData))
            return;

        ForceSuccessionRules.ApplyForceElimination(
            forceId,
            conquerorForceId,
            gameData,
            context.GameWorldContext.GameWorld.GameMasterData,
            dayOutcomeBuffer,
            "失去所有据点且无可战之兵，势力投降。");
    }
}
