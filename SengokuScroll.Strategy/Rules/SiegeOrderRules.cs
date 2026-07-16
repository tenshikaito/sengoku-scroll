using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>攻城指令：强攻 / 包围；需消耗 AP，不自动因踩格占城。</summary>
public static class SiegeOrderRules
{
    /// <summary>校验攻城指令是否合法：敌对、相邻、AP 足够、模式与位置匹配。</summary>
    public static GameResult Validate(
        Unit attacker,
        Stronghold target,
        UnitSiegeMode mode,
        GameData gameData,
        int siegeApCost)
    {
        if (!attacker.IsMilitary || attacker.Soldier <= 0)
            return GameError.UnitError.UnitNotFound;

        if (target.ForceId == attacker.ForceId)
            return GameError.DataNotFound;

        if (!gameData.Forces.TryGetValue(attacker.ForceId, out var myForce)
            || !gameData.Forces.TryGetValue(target.ForceId, out var enemyForce))
            return GameError.DataNotFound;

        if (!DiplomacyRules.IsEnemy(myForce, enemyForce).IsSuccess)
            return GameError.DataNotFound;

        if (attacker.Ap < siegeApCost)
            return GameError.ApNotEnough;

        // 业务：攻城须在据点同格入 Siege Battlefield（邻格包围废止）
        if (!attacker.Location.IsSameTile(target.Location)
            && !IsAdjacentToStronghold(attacker, target))
            return GameError.DataNotFound;

        // 业务：包围最终以同格兵力计压力；允许先从邻格下令后入城，Apply 时再建 BF
        return GameResult.Ok();
    }

    /// <summary>下达攻城指令：扣 AP、设置方针与攻城模式，并按模式触发接敌或包围。</summary>
    public static void Apply(
        IGameWorldContext context,
        Unit attacker,
        Stronghold target,
        UnitSiegeMode mode,
        GameData gameData,
        int siegeApCost,
        StrategyScenarioMeta? meta = null)
    {
        attacker.Ap = Math.Max(0, attacker.Ap - siegeApCost);
        attacker.Directive = UnitDirective.Occupy;
        attacker.DirectiveTargetId = target.Id;
        attacker.ActionTarget.StrongholdId = target.Id;
        attacker.ActionTarget.UnitId = 0;
        attacker.ActionTarget.RoutePoints.Clear();
        attacker.SiegeMode = mode;

        WarRules.EnsureWarBetween(gameData, attacker.ForceId, target.ForceId, gameData.GameDate);

        var holdInCity = meta is not null
                         && GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(target, gameData, meta);

        var garrison = StrongholdGarrisonRules.FindGarrisonUnit(target, gameData);
        if (garrison is null && !holdInCity)
            garrison = StrongholdGarrisonActions.EnsureDefenderUnit(context, target, gameData, meta);

        if (attacker.Location.IsSameTile(target.Location))
            BattlefieldContainerRules.EnsureSiegeBattlefield(attacker, target, gameData);

        if (mode == UnitSiegeMode.Assault)
        {
            attacker.Stance = UnitStance.Attacking;

            // 业务：强攻且守军地图单位已出战——排队攻击
            if (garrison is not null && BattleFactorEvaluator.CanUnitEngage(attacker))
            {
                UnitBattleActions.QueueAttack(attacker, garrison.Id);
                return;
            }

            // 业务：笼城仅用城内兵——同格强攻进入围城压制，不 materialize 守军
            if (holdInCity && StrongholdGarrisonRules.HasCityGarrison(target))
            {
                attacker.Stance = UnitStance.Normal;
                attacker.SiegeMode = UnitSiegeMode.Assault;
                return;
            }

            // 业务：无驻军且攻方已入城格，守备已崩溃则待日更占城
            if (!StrongholdGarrisonRules.IsAnyGarrisonPresent(target, gameData)
                && attacker.Location.IsSameTile(target.Location))
                return;

            attacker.Stance = UnitStance.Normal;
            attacker.SiegeMode = UnitSiegeMode.Assault;
            return;
        }

        // 业务：包围模式——攻方待机，守军标记为被包围
        attacker.Stance = UnitStance.Surrounding;
        attacker.Status = UnitStatus.Waiting;

        if (garrison is not null)
        {
            garrison.Status = UnitStatus.BeingSurround;
            garrison.Stance = UnitStance.Hold;
        }
    }

    /// <summary>持攻城令期间禁止另行规划路径或下达移动。</summary>
    public static bool IsSiegeMovementLocked(Unit unit)
        => unit.SiegeMode != UnitSiegeMode.None;

    /// <summary>攻方是否与目标据点相邻或已站在据点格上。</summary>
    public static bool IsAdjacentToStronghold(Unit unit, Stronghold stronghold)
    {
        if (unit.Location.IsSameTile(stronghold.Location))
            return true;

        return unit.Location.IsAdjacent(stronghold.Location);
    }

    /// <summary>据点是否仍有守军（城内或出城野战）。</summary>
    public static bool HasDefendingGarrison(Stronghold stronghold, GameData gameData)
        => StrongholdGarrisonRules.IsAnyGarrisonPresent(stronghold, gameData);

    /// <summary>强攻模式下，攻方位置正确且守备已崩溃时可占城。</summary>
    public static bool CanCaptureViaAssaultOrder(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (unit.SiegeMode != UnitSiegeMode.Assault)
            return false;

        if (unit.ActionTarget.StrongholdId != stronghold.Id)
            return false;

        if (!unit.Location.IsSameTile(stronghold.Location))
            return false;

        return StrongholdCaptureRules.IsStrongholdDefenseBroken(stronghold, gameData);
    }

    /// <summary>包围模式下，相邻且守备已崩溃时可占城。</summary>
    public static bool CanCaptureViaEncircleOrder(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (unit.SiegeMode != UnitSiegeMode.Encircle)
            return false;

        if (unit.ActionTarget.StrongholdId != stronghold.Id)
            return false;

        if (!IsAdjacentToStronghold(unit, stronghold))
            return false;

        if (unit.Location.IsSameTile(stronghold.Location))
            return false;

        return StrongholdCaptureRules.IsStrongholdDefenseBroken(stronghold, gameData);
    }
}
