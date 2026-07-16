using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>
/// 攻城日更：包围封锁效果、强攻空城占城；不处理踩格自动占领。
/// </summary>
public interface IStrategySiegeSystem : IGameSystem
{
}

public sealed class StrategySiegeSystem(
    IGameContext context,
    StrongholdCaptureHelper captureHelper,
    StrategyDayOutcomeBuffer dayOutcomeBuffer) : IStrategySiegeSystem
{
    public const int EncircleGarrisonMoraleDrain = 2;
    public const int EncircleStrongholdMoraleDrain = 1;
    public const int AssaultStrongholdMoraleDrain = 2;
    public const int EncircleStabilityDrain = 1;
    public const int EncircleFoodDrainBasisPoints = 200;

    public int Order { get; } = 21;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var masterData = context.GameWorldContext.GameWorld.GameMasterData;

        // 阶段1：处理包围据点——逐日削弱守军/城内士气与粮储
        foreach (var unit in context.GameWorldContext.EachUnit()
                     .Where(u => u.IsMilitary && u.Soldier > 0 && u.SiegeMode == UnitSiegeMode.Encircle)
                     .OrderBy(u => u.Id))
        {
            if (unit.Stance != UnitStance.Surrounding || unit.ActionTarget.StrongholdId <= 0)
                continue;

            if (!gameData.Strongholds.TryGetValue(unit.ActionTarget.StrongholdId, out var target))
                continue;

            if (!SiegeOrderRules.IsAdjacentToStronghold(unit, target))
                continue;

            ApplyEncirclePressure(unit, target, gameData, dayOutcomeBuffer);
        }

        // 阶段1b：强攻同格——攻方伤亡、城防折损；笼城时额外消耗城内兵与士气
        ApplyAssaultPressureByStronghold(context, gameData, masterData, dayOutcomeBuffer);

        // 阶段2：强攻 / 包围——守备崩溃且满足攻城位置时占城
        foreach (var unit in context.GameWorldContext.EachUnit()
                     .Where(u => u.IsMilitary && u.Soldier > 0
                                 && u.SiegeMode is UnitSiegeMode.Assault or UnitSiegeMode.Encircle)
                     .OrderBy(u => u.Id))
        {
            if (unit.ActionTarget.StrongholdId <= 0)
                continue;

            if (!gameData.Strongholds.TryGetValue(unit.ActionTarget.StrongholdId, out var target))
                continue;

            var canCapture = unit.SiegeMode == UnitSiegeMode.Assault
                ? SiegeOrderRules.CanCaptureViaAssaultOrder(unit, target, gameData)
                : SiegeOrderRules.CanCaptureViaEncircleOrder(unit, target, gameData);

            if (!canCapture)
                continue;

            captureHelper.CaptureStronghold(unit, target, target.ForceId, gameData);
        }

        BattlefieldContainerRules.PruneOpenBattlefields(gameData);
    }

    private static void ApplyAssaultPressureByStronghold(
        IGameContext context,
        GameData gameData,
        GameMasterData masterData,
        StrategyDayOutcomeBuffer buffer)
    {
        var assaultGroups = context.GameWorldContext.EachUnit()
            .Where(u => u.IsMilitary && u.Soldier > 0 && u.SiegeMode == UnitSiegeMode.Assault)
            .Where(u => u.ActionTarget.StrongholdId > 0)
            .GroupBy(u => u.ActionTarget.StrongholdId);

        foreach (var group in assaultGroups)
        {
            if (!gameData.Strongholds.TryGetValue(group.Key, out var target))
                continue;

            var onTile = group
                .Where(u => u.Location.IsSameTile(target.Location))
                .ToList();
            if (onTile.Count == 0)
                continue;

            SiegeAssaultRules.ApplyAttackerDailyCasualties(onTile, target, masterData);

            var totalAttackers = onTile.Sum(u => u.Soldier);
            var hasMapGarrison = StrongholdGarrisonRules.FindGarrisonUnit(target, gameData) is not null;
            ApplyStrongholdAssaultPressure(
                onTile[0],
                target,
                totalAttackers,
                hasMapGarrison,
                gameData,
                masterData,
                buffer);
        }
    }

    private static void ApplyStrongholdAssaultPressure(
        Unit representativeAttacker,
        Stronghold target,
        int totalAttackerSoldiers,
        bool hasMapGarrison,
        GameData gameData,
        GameMasterData masterData,
        StrategyDayOutcomeBuffer buffer)
    {
        var loc = new Point2(target.Location.X, target.Location.Y);
        var siegeBattlefield = BattlefieldContainerRules.FindOpenAt(loc, gameData, BattlefieldKind.Siege);
        if (siegeBattlefield is not null)
            siegeBattlefield.StandoffDays++;

        var assaultDays = siegeBattlefield?.StandoffDays ?? 1;
        var totalDefense = StrongholdDefenseRules.ResolveTotalDefense(target, masterData);

        if (SiegeAssaultRules.ShouldWearDefenseFacilityToday(assaultDays, totalDefense, totalAttackerSoldiers))
        {
            StrongholdDefenseRules.ApplySiegeDamage(target, 1);
            StrongholdDefenseRules.SyncDefenseValue(target, masterData);
        }

        // 业务：地图守军在场时由野战结算伤亡；此处仅处理笼城（纯城内数字兵）
        if (!hasMapGarrison)
        {
            var citySoldiers = StrongholdGarrisonRules.GetCityGarrisonSoldiers(target);
            if (citySoldiers > 0)
            {
                var soldierLoss = Math.Max(1, (int)Math.Ceiling(citySoldiers * 0.025));
                target.ForceActor.Soldier = Math.Max(0, citySoldiers - soldierLoss);
            }

            target.ForceActor.Morale = (byte)Math.Max(0, target.ForceActor.Morale - AssaultStrongholdMoraleDrain);

            if (target.ForceActor.Morale <= BattleConstants.FearfulMoraleThreshold)
            {
                buffer.AddEvent(new StrategyEventDto
                {
                    Category = "SiegeAssault",
                    Brief = $"⚔ {target.Name} 城内军心动摇",
                    Message = $"{representativeAttacker.Name} 强攻 {target.Name}，城内士气持续下降。"
                });
            }
        }
    }

    private static void ApplyEncirclePressure(
        Unit encircler,
        Stronghold target,
        GameData gameData,
        StrategyDayOutcomeBuffer buffer)
    {
        var garrison = StrongholdGarrisonRules.FindGarrisonUnit(target, gameData);

        // 业务：野战守军被围时标记被围姿态并扣士气
        if (garrison is not null)
        {
            garrison.Status = UnitStatus.BeingSurround;
            garrison.Stance = UnitStance.Hold;
            garrison.Morale = (byte)Math.Max(
                BattleConstants.LowMoraleEngageThreshold,
                garrison.Morale - EncircleGarrisonMoraleDrain);
        }

        target.ForceActor.Morale = (byte)Math.Max(
            BattleConstants.LowMoraleEngageThreshold,
            target.ForceActor.Morale - EncircleGarrisonMoraleDrain);

        target.ForceActor.Morale = (byte)Math.Max(0, target.ForceActor.Morale - EncircleStrongholdMoraleDrain);
        target.Stability = (byte)Math.Max(0, target.Stability - EncircleStabilityDrain);

        var foodDrain = target.ForceActor.Food * EncircleFoodDrainBasisPoints / 10000;
        if (foodDrain > 0)
            target.ForceActor.Food = Math.Max(0, target.ForceActor.Food - foodDrain);

        if (garrison is not null && garrison.Morale <= BattleConstants.FearfulMoraleThreshold)
        {
            buffer.AddEvent(new StrategyEventDto
            {
                Category = "SiegeEncircle",
                Brief = $"⭕ {target.Name} 被围士气动摇",
                Message = $"{encircler.Name} 包围 {target.Name}，守军士气持续下降。"
            });
        }
        else if (garrison is null
                 && target.ForceActor.Morale <= BattleConstants.FearfulMoraleThreshold
                 && StrongholdGarrisonRules.HasCityGarrison(target))
        {
            buffer.AddEvent(new StrategyEventDto
            {
                Category = "SiegeEncircle",
                Brief = $"⭕ {target.Name} 城内军心动摇",
                Message = $"{encircler.Name} 包围 {target.Name}，城内驻军士气持续下降。"
            });
        }
    }
}
