using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 运输队自动派遣：评估低粮单位并从最近有粮据点生成运输队与路径。
/// </summary>
/// <remarks>
/// 业务循环：据点出库 → 运输队沿路径前进 → 抵达部队卸粮 → 返程回据点 → 移除实体；
/// 若部队仍缺粮，下一日可再次自动派遣。玩家改道须经信使（前端占位，后续 API）。
/// </remarks>
public class SupplyConvoyDispatchHelper(
    IGameContext context,
    IPathfindingService pathfindingService,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyTributeLedger tributeLedger,
    MonthlyTaxCollectionLedger monthlyTaxCollectionLedger)
{
    /// <summary>
    /// 扫描己方单位，为缺粮且尚无在途（含返程）运输队的单位自动派遣补给。
    /// </summary>
    /// <returns>本次新创建的运输队数量。</returns>
    public int DispatchNeededConvoys()
    {
        var world = context.GameWorldContext.GameWorld;
        var gameData = world.GameData;
        var created = 0;

        foreach (var unit in context.GameWorldContext.EachUnit())
        {
            // 业务：缺粮且尚无在途运输队时，从最近有粮据点自动发粮
            if (unit.Food >= SupplyDispatchConstants.UnitFoodThresholdGo)
                continue;

            if (HasActiveConvoyForUnit(gameData.SupplyConvoys, unit.Id))
                continue;

            var stronghold = FindNearestStrongholdWithFood(unit, gameData);
            if (stronghold is null)
                continue;

            if (GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData))
                continue;

            var path = pathfindingService.CalculatePath(
                new MapPathAgent(stronghold.Location, unit.ForceId),
                unit.Location);

            if (path is null || path.Count <= 1)
                continue;

            var cargo = Math.Min(
                LogisticsConstants.DefaultConvoyCargoGo,
                stronghold.ForceActor.Food);

            if (cargo <= 0)
                continue;

            stronghold.ForceActor.Food -= cargo;

            var convoyId = NextEntityId(gameData.SupplyConvoys.Keys);
            var convoy = new SupplyConvoy
            {
                Id = convoyId,
                Name = BuildConvoyName(stronghold, unit),
                ForceId = unit.ForceId,
                LeaderId = ResolveConvoyLeaderId(stronghold, gameData),
                Location = stronghold.Location,
                OriginStrongholdId = stronghold.Id,
                TargetUnitId = unit.Id,
                CargoFoodGo = cargo,
                Purpose = TransportPurpose.Supply,
                PorterCount = LogisticsConstants.DefaultPorterCount,
                EscortSoldierCount = LogisticsConstants.DefaultEscortSoldierCount,
                Movement = LogisticsConstants.ConvoyDailyAp,
                Ap = 0,
                Status = SupplyConvoyStatus.Moving,
                RoutePoints = RouteCalculator.ToDailyRouteQueue(path)
            };

            gameData.SupplyConvoys[convoyId] = convoy;
            created++;
        }

        return created;
    }

    /// <summary>
    /// 每月 1 日：自势力范围内各据点向目标居城运送金钱税赋（M4-b；粮税在收粮日另派）。
    /// </summary>
    public int DispatchMonthlyLordTributes()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        if (!EconomyRules.IsMonthlySettlementDay(gameData.GameDate))
            return 0;

        var created = 0;

        foreach (var origin in gameData.Strongholds.Values)
        {
            var destinationId = TributeRoutingHelper.ResolveTributeDestinationStrongholdId(
                origin,
                gameData,
                scenarioMeta);

            if (destinationId is not int targetStrongholdId)
                continue;

            if (!gameData.Strongholds.TryGetValue(targetStrongholdId, out var destination))
                continue;

            if (HasActiveTributeConvoy(gameData.SupplyConvoys, origin.Id, targetStrongholdId))
                continue;

            if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
                continue;

            var moneyCargo = monthlyTaxCollectionLedger.ConsumeMoneyTributeObligation(origin.Id);
            var obligation = moneyCargo;
            moneyCargo = Math.Min(moneyCargo, origin.ForceActor.Money);

        // 业务：府库不足则记欠账，仍尝试按实际库存发运
            if (moneyCargo <= 0)
            {
                if (obligation > 0)
                    TributeArrearsActions.AccrueShortfall(gameData, origin, 0, obligation);

                continue;
            }

            if (moneyCargo < obligation)
                TributeArrearsActions.AccrueShortfall(gameData, origin, 0, obligation - moneyCargo);

            if (!TryCreateTributeConvoy(origin, destination, gameData, 0, moneyCargo, TransportPurpose.TaxMoney))
                continue;

            created++;

            if (TributeRoutingHelper.ResolveRealmRootForceId(origin.ForceId, gameData)
                == scenarioMeta.PlayerForceId)
            {
                dayOutcomeBuffer.AddEvent(new StrategyEventDto
                {
                    Category = "LordTributeDispatched",
                    Brief = $"💰 钱纳队自 {origin.Name} 出发",
                    Message =
                        $"💰 钱纳运输队自 {origin.Name} 出发，向 {destination.Name} 运送税赋 " +
                        $"💰{moneyCargo:N0}"
                });
            }
        }

        return created;
    }

    /// <summary>收粮日：自府库运送贡粮义务至目标居城。</summary>
    public bool DispatchHarvestFoodTribute(Stronghold origin, int obligationFoodGo)
    {
        if (obligationFoodGo <= 0)
            return false;

        var gameData = context.GameWorldContext.GameWorld.GameData;

        var destinationId = TributeRoutingHelper.ResolveTributeDestinationStrongholdId(
            origin,
            gameData,
            scenarioMeta);

        if (destinationId is not int targetStrongholdId)
            return false;

        if (!gameData.Strongholds.TryGetValue(targetStrongholdId, out var destination))
            return false;

        if (HasActiveTributeConvoy(gameData.SupplyConvoys, origin.Id, targetStrongholdId))
            return false;

        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
            return false;

        var foodCargo = Math.Min(obligationFoodGo, origin.ForceActor.Food);
        if (foodCargo <= 0)
        {
            TributeArrearsActions.AccrueShortfall(gameData, origin, obligationFoodGo, 0);
            return false;
        }

        if (foodCargo < obligationFoodGo)
            TributeArrearsActions.AccrueShortfall(gameData, origin, obligationFoodGo - foodCargo, 0);

        if (!TryCreateTributeConvoy(origin, destination, gameData, foodCargo, 0, TransportPurpose.Tribute))
            return false;

        if (TributeRoutingHelper.ResolveRealmRootForceId(origin.ForceId, gameData)
            == scenarioMeta.PlayerForceId)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "LordTributeDispatched",
                Brief = $"🌾 贡粮队自 {origin.Name} 出发",
                Message =
                    $"🌾 贡粮运输队自 {origin.Name} 出发，向 {destination.Name} 运送 🌾{foodCargo:N0}"
            });
        }

        return true;
    }

    /// <summary>扫描同势力据点间粮价差，派遣贸易运输队（M4-c）。</summary>
    public int DispatchTradeConvoys()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var created = 0;

        foreach (var destination in gameData.Strongholds.Values)
        {
            foreach (var origin in gameData.Strongholds.Values)
            {
                if (!TradeMarketAiHelper.ShouldDispatchTrade(origin, destination, gameData))
                    continue;

                var cargo = TradeMarketAiHelper.CalculateTradeCargoGo(origin, destination);
                if (cargo <= 0)
                    continue;

                if (!TryCreateTradeConvoy(origin, destination, gameData, cargo))
                    continue;

                created++;

                if (origin.ForceId == scenarioMeta.PlayerForceId)
                {
                    dayOutcomeBuffer.AddEvent(new StrategyEventDto
                    {
                        Category = "TradeConvoyDispatched",
                        Brief = $"📦 贸易队 {origin.Name}→{destination.Name}",
                        Message =
                            $"📦 贸易运输队自 {origin.Name} 出发，向 {destination.Name} 运送 🌾{cargo:N0}"
                    });
                }

                break;
            }
        }

        return created;
    }

    private bool TryCreateTradeConvoy(
        Stronghold origin,
        Stronghold destination,
        GameData gameData,
        int foodCargo)
    {
        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
            return false;

        if (TradeConvoyMigrationRules.PreferUnitTradeConvoys)
        {
            return TradeConvoyUnitFactory.TryCreateMerchantTradeUnit(
                context.GameWorldContext.GameWorld,
                origin,
                destination,
                foodCargo) is not null;
        }

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, TradeMarketAiHelper.ResolvePathForceId(origin, gameData)),
            destination.Location);

        if (path is null || path.Count <= 1)
            return false;

        origin.ForceActor.Food -= foodCargo;

        if (gameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(originForce, gameData);

        var convoyId = NextEntityId(gameData.SupplyConvoys.Keys);
        var convoy = new SupplyConvoy
        {
            Id = convoyId,
            Name = $"{origin.Name}贸易→{destination.Name}",
            ForceId = origin.ForceId,
            LeaderId = ResolveConvoyLeaderId(origin, gameData),
            Location = origin.Location,
            OriginStrongholdId = origin.Id,
            TargetUnitId = 0,
            TargetStrongholdId = destination.Id,
            CargoFoodGo = foodCargo,
            CargoMoney = 0,
            Purpose = TransportPurpose.Trade,
            PorterCount = LogisticsConstants.DefaultPorterCount,
            EscortSoldierCount = LogisticsConstants.DefaultEscortSoldierCount,
            Movement = LogisticsConstants.ConvoyDailyAp,
            Ap = 0,
            Status = SupplyConvoyStatus.Moving,
            RoutePoints = RouteCalculator.ToDailyRouteQueue(path)
        };

        gameData.SupplyConvoys[convoyId] = convoy;
        return true;
    }

    private bool TryCreateTributeConvoy(
        Stronghold origin,
        Stronghold destination,
        GameData gameData,
        int foodCargo,
        int moneyCargo,
        TransportPurpose purpose)
    {
        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
            return false;

        var pathForceId = TributeRoutingHelper.ResolveRealmRootForceId(origin.ForceId, gameData);
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, pathForceId),
            destination.Location);

        if (path is null || path.Count <= 1)
            return false;

        origin.ForceActor.Food -= foodCargo;
        origin.ForceActor.Money -= moneyCargo;

        if (gameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(originForce, gameData);

        var convoyId = NextEntityId(gameData.SupplyConvoys.Keys);
        var convoy = new SupplyConvoy
        {
            Id = convoyId,
            Name = foodCargo > 0
                ? $"{origin.Name}贡粮→{destination.Name}"
                : $"{origin.Name}钱纳→{destination.Name}",
            ForceId = origin.ForceId,
            LeaderId = ResolveConvoyLeaderId(origin, gameData),
            Location = origin.Location,
            OriginStrongholdId = origin.Id,
            TargetUnitId = 0,
            TargetStrongholdId = destination.Id,
            CargoFoodGo = foodCargo,
            CargoMoney = moneyCargo,
            Purpose = purpose,
            PorterCount = LogisticsConstants.DefaultPorterCount,
            EscortSoldierCount = LogisticsConstants.DefaultEscortSoldierCount,
            Movement = LogisticsConstants.ConvoyDailyAp,
            Ap = 0,
            Status = SupplyConvoyStatus.Moving,
            RoutePoints = RouteCalculator.ToDailyRouteQueue(path)
        };

        gameData.SupplyConvoys[convoyId] = convoy;
        return true;
    }

    /// <summary>
    /// 处理已抵达运输队：卸粮后安排返程；返程抵达据点后移出地图。
    /// </summary>
    public void CompleteArrivedConvoys()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        foreach (var convoy in gameData.SupplyConvoys.Values.ToList())
        {
            if (convoy.Status != SupplyConvoyStatus.Arrived)
                continue;

            // 业务：返程抵达出发据点后移除实体，结束运输循环
            if (convoy.IsReturningToOrigin)
            {
                gameData.SupplyConvoys.Remove(convoy.Id);
                continue;
            }

            if (convoy.TargetStrongholdId > 0)
            {
                if (gameData.Strongholds.TryGetValue(convoy.TargetStrongholdId, out var destination))
                {
                    if (convoy.Purpose == TransportPurpose.Migrant)
                    {
                        MigrantConvoyActions.CompleteMigrantArrival(convoy, destination);
                        gameData.SupplyConvoys.Remove(convoy.Id);
                        continue;
                    }

                    if (convoy.Purpose == TransportPurpose.Trade
                        && gameData.Strongholds.TryGetValue(convoy.OriginStrongholdId, out var tradeOrigin))
                    {
                        var revenue = TradeEconomyActions.CompleteTradeArrival(
                            convoy,
                            tradeOrigin,
                            destination,
                            gameData);

                        if (tradeOrigin.ForceId == scenarioMeta.PlayerForceId && revenue > 0)
                        {
                            dayOutcomeBuffer.AddEvent(new StrategyEventDto
                            {
                                Category = "TradeConvoyArrived",
                                Brief = $"📦 贸易队抵达 {destination.Name}",
                                Message =
                                    $"📦 贸易队 {convoy.Name} 抵达 {destination.Name}，" +
                                    $"贸易收入 💰{revenue:N0}"
                            });
                        }

                        ScheduleReturnToOrigin(convoy, gameData);
                        continue;
                    }

                    var deliveredFood = convoy.CargoFoodGo;
                    var deliveredMoney = convoy.CargoMoney;

                    SupplyConvoyActions.DeliverCargoToStronghold(convoy, destination);

                    if (gameData.Strongholds.TryGetValue(convoy.OriginStrongholdId, out var origin))
                    {
                        var playerCapitalId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                            scenarioMeta.PlayerForceId,
                            gameData,
                            scenarioMeta);

                        // 业务：贡纳抵达玩家居城时记入贡纳台账并推送事件
                        if (destination.Id == playerCapitalId
                            && TributeRoutingHelper.ResolveRealmRootForceId(origin.ForceId, gameData)
                            == scenarioMeta.PlayerForceId)
                        {
                            tributeLedger.RecordArrival(
                                gameData.GameDate.Year,
                                origin.Id,
                                origin.Name,
                                deliveredFood,
                                deliveredMoney);

                            dayOutcomeBuffer.AddEvent(new StrategyEventDto
                            {
                                Category = "LordTributeArrived",
                                Brief = $"🌾 {origin.Name} 贡纳抵达 {destination.Name}",
                                Message =
                                    $"🌾 贡纳运输队自 {origin.Name} 抵达 {destination.Name}，" +
                                    $"入库 🌾{deliveredFood:N0} 💰{deliveredMoney:N0}"
                            });
                        }

                        if (gameData.Forces.TryGetValue(destination.ForceId, out var destForce))
                            ForceEconomyActions.SyncForceTreasuryFromStrongholds(destForce, gameData);
                    }
                }
                else
                {
                    gameData.SupplyConvoys.Remove(convoy.Id);
                    continue;
                }

                ScheduleReturnToOrigin(convoy, gameData);
                continue;
            }

            if (!gameData.Units.TryGetValue(convoy.TargetUnitId, out var unit))
            {
                gameData.SupplyConvoys.Remove(convoy.Id);
                continue;
            }

            SupplyConvoyActions.DeliverCargoToUnit(convoy, unit);

            if (unit.ForceId == scenarioMeta.PlayerForceId)
            {
                dayOutcomeBuffer.AddEvent(new StrategyEventDto
                {
                    Category = "SupplyConvoyArrived",
                    Brief = $"🌾 补给抵达 {unit.Name}",
                    Message =
                        $"🌾 运输队 {convoy.Name} 向 {unit.Name} 卸粮 🌾{convoy.CargoFoodGo:N0}"
                });
            }

            ScheduleReturnToOrigin(convoy, gameData);
        }
    }

    /// <summary>计算返回出发据点的路径并进入返程阶段。</summary>
    private void ScheduleReturnToOrigin(SupplyConvoy convoy, GameData gameData)
    {
        if (!gameData.Strongholds.TryGetValue(convoy.OriginStrongholdId, out var stronghold))
        {
            gameData.SupplyConvoys.Remove(convoy.Id);
            return;
        }

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(convoy.Location, convoy.ForceId),
            stronghold.Location);

        if (path is null || path.Count <= 1)
        {
            convoy.Location = stronghold.Location;
            gameData.SupplyConvoys.Remove(convoy.Id);
            return;
        }

        SupplyConvoyActions.BeginReturnToOrigin(
            convoy,
            RouteCalculator.ToDailyRouteQueue(path));
    }

    private Stronghold? FindNearestStrongholdWithFood(Unit unit, GameData gameData)
    {
        Stronghold? best = null;
        var bestSteps = int.MaxValue;

        foreach (var stronghold in context.GameWorldContext.EachStronghold())
        {
            if (stronghold.ForceId != unit.ForceId)
                continue;

            if (GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData))
                continue;

            if (stronghold.ForceActor.Food < SupplyDispatchConstants.StrongholdMinFoodGo)
                continue;

            var path = pathfindingService.CalculatePath(
                new MapPathAgent(stronghold.Location, unit.ForceId),
                unit.Location);

            var steps = RouteCalculator.CountSteps(path);
            if (steps <= 0 || steps >= bestSteps)
                continue;

            bestSteps = steps;
            best = stronghold;
        }

        return best;
    }

    /// <summary>目标部队是否已有在途或返程中的运输任务（同一部队不重复派遣）。</summary>
    private static bool HasActiveConvoyForUnit(
        IReadOnlyDictionary<int, SupplyConvoy> convoys,
        int unitId)
    {
        foreach (var convoy in convoys.Values)
        {
            if (convoy.TargetUnitId != unitId)
                continue;

            if (convoy.Status is SupplyConvoyStatus.Moving
                or SupplyConvoyStatus.Deceived
                or SupplyConvoyStatus.Forming
                or SupplyConvoyStatus.Arrived)
                return true;
        }

        return false;
    }

    private static bool HasActiveTributeConvoy(
        IReadOnlyDictionary<int, SupplyConvoy> convoys,
        int originStrongholdId,
        int targetStrongholdId)
    {
        foreach (var convoy in convoys.Values)
        {
            if (convoy.OriginStrongholdId != originStrongholdId)
                continue;

            if (convoy.TargetStrongholdId != targetStrongholdId)
                continue;

            if (convoy.IsReturningToOrigin)
                continue;

            if (convoy.Status is SupplyConvoyStatus.Moving
                or SupplyConvoyStatus.Deceived
                or SupplyConvoyStatus.Forming
                or SupplyConvoyStatus.Arrived)
                return true;
        }

        return false;
    }

    private static int NextEntityId(IEnumerable<int> existingIds)
    {
        var max = existingIds.DefaultIfEmpty(0).Max();
        return max + 1;
    }

    private static string BuildConvoyName(Stronghold origin, Unit target)
        => $"{origin.Name}粮运→{target.Name}";

    private static int ResolveConvoyLeaderId(Stronghold origin, GameData gameData)
    {
        if (origin.LeaderId > 0)
            return origin.LeaderId;

        return gameData.Characters.Values
            .FirstOrDefault(c => c.ForceId == origin.ForceId && c.StrongholdId == origin.Id)?.Id ?? 0;
    }
}
