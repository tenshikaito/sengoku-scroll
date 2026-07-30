using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>以 Merchant Unit 派出贸易队：粮自商户 Actor 出库，队属商家组织势力。</summary>
public static class TradeConvoyUnitFactory
{
    public static Unit? TryCreateMerchantTradeUnit(
        GameWorld world,
        Stronghold origin,
        Stronghold destination,
        StrongholdActor merchant,
        int cargoFoodGo,
        IPathfindingService pathfindingService)
    {
        if (cargoFoodGo <= 0)
            return null;

        // 业务：武家官府不派贸易队；仅商户库存出粮
        if (!world.GameData.Forces.TryGetValue(merchant.ForceId, out var merchantForce)
            || merchantForce.Category != ForceCategory.Merchant)
        {
            return null;
        }

        if (merchant.Food < cargoFoodGo)
            return null;

        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, world.GameData))
            return null;

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, TradeMarketAiHelper.ResolvePathForceId(origin, world.GameData)),
            destination.Location);

        if (path is null || path.Count <= 1)
            return null;

        merchant.Food -= cargoFoodGo;

        var escortMoney = Math.Min(merchant.Money / 20, cargoFoodGo / 2);
        if (escortMoney > 0 && merchant.Money >= escortMoney)
            merchant.Money -= escortMoney;
        else
            escortMoney = 0;

        return ConvoyUnitFactory.CreateTransportUnit(
            world,
            $"{merchant.Name}·{origin.Name}→{destination.Name}",
            merchant.ForceId,
            ResolveLeaderId(merchant, origin, world.GameData),
            origin.Location,
            origin.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo: cargoFoodGo,
            moneyCargo: escortMoney,
            cargoPopulation: 0,
            TransportPurpose.Trade,
            RouteCalculator.ToDailyRouteQueuePoint2(path),
            kindOverride: UnitKind.Merchant);
    }

    private static int ResolveLeaderId(StrongholdActor merchant, Stronghold origin, GameData gameData)
    {
        // 业务：贸易队总将必须是本店/商家组织角色，禁止回退武家代官（否则犬山今井屋会挂上酒井忠次）
        foreach (var characterId in merchant.CharacterIds)
        {
            if (characterId <= 0)
                continue;
            if (!gameData.Characters.TryGetValue(characterId, out var staff) || staff.IsDead)
                continue;
            return staff.Id;
        }

        var shopLeader = gameData.Characters.Values
            .FirstOrDefault(c =>
                c.ForceId == merchant.ForceId
                && c.StrongholdId == origin.Id
                && !c.IsDead);
        return shopLeader?.Id ?? 0;
    }
}
