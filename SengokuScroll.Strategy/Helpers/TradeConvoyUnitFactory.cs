using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>以 Merchant Unit 替代 SupplyConvoy 的贸易队。</summary>
public static class TradeConvoyUnitFactory
{
    public static Unit? TryCreateMerchantTradeUnit(
        GameWorld world,
        Stronghold origin,
        Stronghold destination,
        int cargoFoodGo,
        IPathfindingService pathfindingService)
    {
        if (cargoFoodGo <= 0)
            return null;

        if (origin.ForceActor.Food < cargoFoodGo)
            return null;

        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, world.GameData))
            return null;

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, TradeMarketAiHelper.ResolvePathForceId(origin, world.GameData)),
            destination.Location);

        if (path is null || path.Count <= 1)
            return null;

        origin.ForceActor.Food -= cargoFoodGo;

        if (world.GameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(originForce, world.GameData);

        return ConvoyUnitFactory.CreateTransportUnit(
            world,
            $"{origin.Name}→{destination.Name}贸易队",
            origin.ForceId,
            ResolveLeaderId(origin, world.GameData),
            origin.Location,
            origin.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo: cargoFoodGo,
            moneyCargo: Math.Min(origin.ForceActor.Money / 20, cargoFoodGo / 2),
            cargoPopulation: 0,
            TransportPurpose.Trade,
            RouteCalculator.ToDailyRouteQueuePoint2(path),
            kindOverride: UnitKind.Merchant);
    }

    private static int ResolveLeaderId(Stronghold origin, GameData gameData)
    {
        if (origin.LeaderId > 0)
            return origin.LeaderId;

        return gameData.Characters.Values
            .FirstOrDefault(c => c.ForceId == origin.ForceId && c.StrongholdId == origin.Id)?.Id ?? 0;
    }
}
