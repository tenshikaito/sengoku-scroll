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
/// 运输队自动派遣：评估低粮单位并从最近有粮据点生成运输 Unit 与路径。
/// </summary>
/// <remarks>
/// 业务循环：据点出库 → 运输 Unit 沿路径前进 → 抵达部队卸粮 → 返程回据点 → 移除实体；
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
    private IGameWorldContext WorldContext => context.GameWorldContext;

    /// <summary>
    /// 扫描己方单位，为缺粮且尚无在途（含返程）运输队的单位自动派遣补给。
    /// </summary>
    /// <returns>本次新创建的运输队数量。</returns>
    public int DispatchNeededConvoys()
    {
        var world = WorldContext.GameWorld;
        var gameData = world.GameData;
        var created = 0;

        foreach (var unit in WorldContext.EachUnit().Where(u => u.IsMilitary).ToList())
        {
            if (unit.Food >= SupplyDispatchConstants.UnitFoodThresholdGo)
                continue;

            if (TransportUnitRules.HasActiveConvoyForUnit(gameData, unit.Id))
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

            ConvoyUnitFactory.CreateTransportUnit(
                world,
                BuildConvoyName(stronghold, unit),
                unit.ForceId,
                ResolveConvoyLeaderId(stronghold, gameData),
                stronghold.Location,
                stronghold.Id,
                targetUnitId: unit.Id,
                targetStrongholdId: 0,
                foodCargo: cargo,
                moneyCargo: 0,
                cargoPopulation: 0,
                purpose: TransportPurpose.Supply,
                routePoints: RouteCalculator.ToDailyRouteQueuePoint2(path));

            created++;
        }

        return created;
    }

    /// <summary>
    /// 每月 1 日：自势力范围内各据点向目标居城运送金钱税赋（M4-b；粮税在收粮日另派）。
    /// </summary>
    public int DispatchMonthlyLordTributes()
    {
        var gameData = WorldContext.GameWorld.GameData;

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

            if (TransportUnitRules.HasActiveTributeTransport(gameData, origin.Id, targetStrongholdId))
                continue;

            if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
                continue;

            var moneyCargo = monthlyTaxCollectionLedger.ConsumeMoneyTributeObligation(origin.Id);
            var obligation = moneyCargo;
            moneyCargo = Math.Min(moneyCargo, origin.ForceActor.Money);

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

        var gameData = WorldContext.GameWorld.GameData;

        var destinationId = TributeRoutingHelper.ResolveTributeDestinationStrongholdId(
            origin,
            gameData,
            scenarioMeta);

        if (destinationId is not int targetStrongholdId)
            return false;

        if (!gameData.Strongholds.TryGetValue(targetStrongholdId, out var destination))
            return false;

        if (TransportUnitRules.HasActiveTributeTransport(gameData, origin.Id, targetStrongholdId))
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
        var gameData = WorldContext.GameWorld.GameData;
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
                WorldContext.GameWorld,
                origin,
                destination,
                foodCargo,
                pathfindingService) is not null;
        }

        return TryCreateLegacyTradeConvoy(origin, destination, gameData, foodCargo);
    }

    private bool TryCreateLegacyTradeConvoy(
        Stronghold origin,
        Stronghold destination,
        GameData gameData,
        int foodCargo)
    {
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, TradeMarketAiHelper.ResolvePathForceId(origin, gameData)),
            destination.Location);

        if (path is null || path.Count <= 1)
            return false;

        origin.ForceActor.Food -= foodCargo;

        if (gameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(originForce, gameData);

        ConvoyUnitFactory.CreateTransportUnit(
            WorldContext.GameWorld,
            $"{origin.Name}贸易→{destination.Name}",
            origin.ForceId,
            ResolveConvoyLeaderId(origin, gameData),
            origin.Location,
            origin.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo,
            moneyCargo: 0,
            cargoPopulation: 0,
            TransportPurpose.Trade,
            RouteCalculator.ToDailyRouteQueuePoint2(path),
            kindOverride: UnitKind.Convoy);

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

        ConvoyUnitFactory.CreateTransportUnit(
            WorldContext.GameWorld,
            foodCargo > 0
                ? $"{origin.Name}贡粮→{destination.Name}"
                : $"{origin.Name}钱纳→{destination.Name}",
            origin.ForceId,
            ResolveConvoyLeaderId(origin, gameData),
            origin.Location,
            origin.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo,
            moneyCargo,
            cargoPopulation: 0,
            purpose,
            RouteCalculator.ToDailyRouteQueuePoint2(path));

        return true;
    }

    /// <summary>
    /// 处理已抵达运输 Unit：卸粮后安排返程；返程抵达据点后移出地图。
    /// </summary>
    public void CompleteArrivedConvoys()
    {
        var world = WorldContext.GameWorld;
        var gameData = world.GameData;

        foreach (var transport in TransportUnitRules.EnumerateActiveTransportUnits(gameData).ToList())
        {
            if (!TransportUnitRules.HasArrived(transport))
                continue;

            if (transport.IsReturningToOrigin)
            {
                TransportUnitActions.DestroyTransport(WorldContext, transport);
                continue;
            }

            if (transport.TransportTargetStrongholdId > 0)
            {
                if (gameData.Strongholds.TryGetValue(transport.TransportTargetStrongholdId, out var destination))
                {
                    if (transport.TransportPurpose == TransportPurpose.Migrant)
                    {
                        MigrantConvoyActions.CompleteMigrantArrival(transport, destination);
                        TransportUnitActions.DestroyTransport(WorldContext, transport);
                        continue;
                    }

                    if (transport.TransportPurpose == TransportPurpose.Trade
                        && gameData.Strongholds.TryGetValue(transport.TransportOriginStrongholdId, out var tradeOrigin))
                    {
                        var revenue = TradeEconomyActions.CompleteTradeArrival(
                            transport,
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
                                    $"📦 贸易队 {transport.Name} 抵达 {destination.Name}，" +
                                    $"贸易收入 💰{revenue:N0}"
                            });
                        }

                        ScheduleReturnToOrigin(transport, gameData);
                        continue;
                    }

                    var deliveredFood = transport.Food;
                    var deliveredMoney = transport.Money;

                    TransportUnitActions.DeliverCargoToStronghold(transport, destination);

                    if (gameData.Strongholds.TryGetValue(transport.TransportOriginStrongholdId, out var origin))
                    {
                        var playerCapitalId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                            scenarioMeta.PlayerForceId,
                            gameData,
                            scenarioMeta);

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
                    TransportUnitActions.DestroyTransport(WorldContext, transport);
                    continue;
                }

                ScheduleReturnToOrigin(transport, gameData);
                continue;
            }

            if (!gameData.Units.TryGetValue(transport.TransportTargetUnitId, out var unit))
            {
                TransportUnitActions.DestroyTransport(WorldContext, transport);
                continue;
            }

            var foodDelivered = transport.Food;
            TransportUnitActions.DeliverCargoToUnit(transport, unit);

            if (unit.ForceId == scenarioMeta.PlayerForceId)
            {
                dayOutcomeBuffer.AddEvent(new StrategyEventDto
                {
                    Category = "SupplyConvoyArrived",
                    Brief = $"🌾 补给抵达 {unit.Name}",
                    Message =
                        $"🌾 运输队 {transport.Name} 向 {unit.Name} 卸粮 🌾{foodDelivered:N0}"
                });
            }

            ScheduleReturnToOrigin(transport, gameData);
        }
    }

    private void ScheduleReturnToOrigin(Unit transport, GameData gameData)
    {
        if (!gameData.Strongholds.TryGetValue(transport.TransportOriginStrongholdId, out var stronghold))
        {
            TransportUnitActions.DestroyTransport(WorldContext, transport);
            return;
        }

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(transport.Location, transport.ForceId),
            stronghold.Location);

        if (path is null || path.Count <= 1)
        {
            transport.Location = stronghold.Location;
            TransportUnitActions.DestroyTransport(WorldContext, transport);
            return;
        }

        TransportUnitActions.BeginReturnToOrigin(
            transport,
            RouteCalculator.ToDailyRouteQueuePoint2(path));
    }

    private Stronghold? FindNearestStrongholdWithFood(Unit unit, GameData gameData)
    {
        Stronghold? best = null;
        var bestSteps = int.MaxValue;

        foreach (var stronghold in WorldContext.EachStronghold())
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
