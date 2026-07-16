using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>从城内兵数编组守城单位（普通 Unit，方针 Support）。</summary>
public static class StrongholdGarrisonActions
{
    /// <summary>
    /// 攻城/接敌需要守军实体时：优先返回已有守城单位，否则从城内兵数编组一支。
    /// </summary>
    public static Unit? EnsureDefenderUnit(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta = null)
    {
        var existing = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData);
        if (existing is not null)
            return existing;

        if (!StrongholdGarrisonRules.HasCityGarrison(stronghold))
            return null;

        // 业务：笼城待援时仅用城内数字驻军，不 materialize 地图单位
        if (meta is not null && GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(stronghold, gameData, meta))
            return null;

        // 业务：一格一军——敌已占城格则无法 materialize 守军
        if (!GarrisonBehaviorRules.CanMaterializeGarrisonOnTile(stronghold, gameData, meta))
            return null;

        return MaterializeGarrisonUnit(context, stronghold, gameData, stronghold.ForceActor.Soldier, meta);
    }

    /// <summary>
    /// 封锁下从城内出兵打击相邻敌军：不登记地图格点，伤亡写回 <see cref="Stronghold.ForceActor"/>。
    /// </summary>
    public static bool ExecuteAbstractSally(
        IGameWorldContext context,
        Stronghold stronghold,
        Unit targetEnemy,
        GameData gameData,
        GameMapMasterData mapMaster,
        int soldiersToCommit,
        StrategyDifficulty difficulty = StrategyDifficulty.Normal)
    {
        soldiersToCommit = Math.Min(soldiersToCommit, StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold));
        if (soldiersToCommit <= 0 || targetEnemy.Soldier <= 0)
            return false;

        var ephemeralId = -10_000 - stronghold.Id;
        stronghold.ForceActor.Soldier -= soldiersToCommit;

        var ephemeral = new Unit
        {
            Id = ephemeralId,
            Name = $"{stronghold.Name}出击队",
            ForceId = stronghold.ForceId,
            Location = stronghold.Location,
            Soldier = soldiersToCommit,
            Food = 0,
            Money = 0,
            Morale = stronghold.ForceActor.Morale,
            Training = stronghold.ForceActor.Training,
            Movement = 8,
            Ap = 0,
            IsMilitary = true,
            Directive = UnitDirective.Support,
            Stance = UnitStance.Normal,
            Status = UnitStatus.Waiting,
            ActionTarget = new UnitActionTarget
            {
                ForceId = stronghold.ForceId,
                StrongholdId = stronghold.Id,
                RoutePoints = new Queue<Point2>()
            },
            SubUnitIds = []
        };

        if (stronghold.LeaderId > 0)
            ephemeral.LeaderId = stronghold.LeaderId;

        gameData.Units[ephemeralId] = ephemeral;
        try
        {
            var resolveCtx = InstantBattleCalculator.CreateResolveContext(
                ephemeral,
                targetEnemy,
                gameData,
                mapMaster);
            var seed = InstantBattleCalculator.ComputeResolutionSeed(
                gameData.GameDate,
                ephemeralId,
                targetEnemy.Id,
                targetEnemy.Location.X,
                targetEnemy.Location.Y);
            var tactical = InstantBattleCalculator.ResolveTactical(resolveCtx, seed, commitReason: "出城打击");
            BattleCasualtyRules.ApplyCasualtiesToWorld(tactical, gameData, difficulty);

            if (tactical.CasualtiesByUnitId.TryGetValue(ephemeralId, out var attackerLoss))
            {
                var survivors = Math.Max(0, soldiersToCommit - attackerLoss);
                stronghold.ForceActor.Soldier += survivors;
            }
            else
            {
                stronghold.ForceActor.Soldier += ephemeral.Soldier;
            }

            return true;
        }
        finally
        {
            gameData.Units.Remove(ephemeralId);
        }
    }

    /// <summary>守城单位被击溃时，同步据点士气并将剩余兵力收回城内。</summary>
    public static void OnGarrisonUnitDestroyed(Unit unit, GameData gameData)
    {
        if (unit.Directive != UnitDirective.Support)
            return;

        var stronghold = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == unit.ForceId && s.Location.IsSameTile(unit.Location));

        if (stronghold is null)
            return;

        if (unit.Soldier > 0)
            StrongholdGarrisonRules.AbsorbSoldiersIntoCity(stronghold, unit.Soldier);

        StrongholdGarrisonRules.SyncCityMoraleFromUnit(stronghold, unit);
    }

    /// <summary>将地图守城单位解散回城内兵（Absorb + RemoveUnit）。</summary>
    public static bool DissolveGarrisonUnitToCity(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData)
    {
        var garrison = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData);
        if (garrison is null)
            return false;

        if (garrison.Soldier > 0)
            StrongholdGarrisonRules.AbsorbSoldiersIntoCity(stronghold, garrison.Soldier);

        StrongholdGarrisonRules.SyncCityMoraleFromUnit(stronghold, garrison);
        MapLocationActions.RemoveUnit(context, garrison);
        gameData.Units.Remove(garrison.Id);
        return true;
    }

    /// <summary>
    /// 主动出城的守城地图单位战败：残部（含败退地板）收回城内，避免整建制歼灭。
    /// </summary>
    public static bool TryAbsorbDefeatedFieldGarrisonIntoCity(
        IGameWorldContext context,
        Unit garrisonUnit,
        GameData gameData,
        StrategyDifficulty difficulty,
        int soldiersBeforeBattle,
        out int absorbedSoldiers)
    {
        absorbedSoldiers = 0;
        var stronghold = SiegeBattleRules.ResolveDefenderStronghold(garrisonUnit, gameData);
        if (stronghold is null || !StrongholdGarrisonRules.IsReliefSupportUnit(garrisonUnit, stronghold))
            return false;

        var survivors = garrisonUnit.Soldier;
        if (soldiersBeforeBattle > 0)
        {
            var ratio = StrategyDifficultyRules.DefeatResidualSoldierRatio(difficulty);
            var floor = Math.Max(1, (int)Math.Ceiling(soldiersBeforeBattle * ratio));
            survivors = Math.Max(survivors, floor);
        }

        if (survivors <= 0)
            return false;

        StrongholdGarrisonRules.AbsorbSoldiersIntoCity(stronghold, survivors);
        StrongholdGarrisonRules.SyncCityMoraleFromUnit(stronghold, garrisonUnit);
        MapLocationActions.RemoveUnit(context, garrisonUnit);
        gameData.Units.Remove(garrisonUnit.Id);
        absorbedSoldiers = survivors;
        return true;
    }

    private static Unit MaterializeGarrisonUnit(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        int soldiers,
        StrategyScenarioMeta? meta = null)
    {
        soldiers = Math.Min(soldiers, StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold));
        if (soldiers <= 0)
            return null!;

        if (!GarrisonBehaviorRules.CanMaterializeGarrisonOnTile(stronghold, gameData, meta))
            return null!;

        var unitId = gameData.Units.Keys.Where(id => id > 0).DefaultIfEmpty(100).Max() + 1;
        stronghold.ForceActor.Soldier -= soldiers;

        var unit = new Unit
        {
            Id = unitId,
            Name = $"{stronghold.Name}守军",
            ForceId = stronghold.ForceId,
            Location = stronghold.Location,
            Soldier = soldiers,
            Food = Math.Min(stronghold.ForceActor.Food / 4, soldiers * 2000),
            Money = 0,
            Morale = stronghold.ForceActor.Morale,
            Training = stronghold.ForceActor.Training,
            Movement = 8,
            Ap = 8,
            IsMilitary = true,
            Directive = UnitDirective.Support,
            Stance = UnitStance.Hold,
            Status = UnitStatus.Waiting,
            ActionTarget = new Unit.UnitActionTarget
            {
                ForceId = stronghold.ForceId,
                StrongholdId = stronghold.Id,
                RoutePoints = new Queue<Point2>()
            },
            SubUnitIds = []
        };

        if (stronghold.LeaderId > 0)
            unit.LeaderId = stronghold.LeaderId;

        gameData.Units[unitId] = unit;
        MapLocationActions.RegisterUnit(context.GameWorld, unit);
        return unit;
    }
}
