using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static SengokuScroll.Domain.Definitions.CharacterDefinition;

namespace SengokuScroll.Strategy.Tests;

/// <summary>商家贸易队不得把武家代官挂成总将。</summary>
public class TradeConvoyLeaderTests
{
    [Fact]
    public void TryCreateMerchantTradeUnit_UsesShopStaff_NotMilitaryMayor()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var world = ctx.World;
        var gameData = world.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();

        var origin = gameData.Strongholds.Values.OrderBy(s => s.Id).First();
        var destination = StrategyTestWorldBuilder.CreateTestStronghold(99, 1, new Point3(4, 0));
        gameData.Strongholds[destination.Id] = destination;
        MapLocationActions.RegisterStronghold(world, destination);

        var mayor = CreateCharacter(6, "酒井忠次", origin.ForceId, origin);
        var clerk = CreateCharacter(90_012, "今井宗久", 9001, origin);
        gameData.Characters[mayor.Id] = mayor;
        gameData.Characters[clerk.Id] = clerk;
        origin.LeaderId = mayor.Id;

        var merchantForce = StrategyTestWorldBuilder.CreateTestForce(9001);
        merchantForce.Name = "今井屋";
        merchantForce.Category = ForceCategory.Merchant;
        gameData.Forces[9001] = merchantForce;

        var merchant = new StrongholdActor
        {
            Id = origin.Id * 1000 + 71,
            Name = "今井屋",
            Type = ActorType.Merchant,
            ForceId = 9001,
            StrongholdId = origin.Id,
            CharacterIds = [clerk.Id],
            SubUnitIds = [],
            Food = 80_000,
            Money = 50_000,
        };
        origin.MerchantActors.Add(merchant);

        var unit = TradeConvoyUnitFactory.TryCreateMerchantTradeUnit(
            world,
            origin,
            destination,
            merchant,
            cargoFoodGo: 10_000,
            pathfinding);

        Assert.NotNull(unit);
        Assert.Equal(clerk.Id, unit!.LeaderId);
        Assert.NotEqual(mayor.Id, unit.LeaderId);
    }

    [Fact]
    public void TryCreateMerchantTradeUnit_WithoutShopStaff_DoesNotFallbackToMayor()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var world = ctx.World;
        var gameData = world.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();

        var origin = gameData.Strongholds.Values.OrderBy(s => s.Id).First();
        var destination = StrategyTestWorldBuilder.CreateTestStronghold(99, 1, new Point3(4, 0));
        gameData.Strongholds[destination.Id] = destination;
        MapLocationActions.RegisterStronghold(world, destination);

        var mayor = CreateCharacter(6, "酒井忠次", origin.ForceId, origin);
        gameData.Characters[mayor.Id] = mayor;
        origin.LeaderId = mayor.Id;

        var merchantForce = StrategyTestWorldBuilder.CreateTestForce(9001);
        merchantForce.Name = "今井屋";
        merchantForce.Category = ForceCategory.Merchant;
        gameData.Forces[9001] = merchantForce;

        var merchant = new StrongholdActor
        {
            Id = origin.Id * 1000 + 71,
            Name = "今井屋",
            Type = ActorType.Merchant,
            ForceId = 9001,
            StrongholdId = origin.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Food = 80_000,
            Money = 50_000,
        };
        origin.MerchantActors.Add(merchant);

        var unit = TradeConvoyUnitFactory.TryCreateMerchantTradeUnit(
            world,
            origin,
            destination,
            merchant,
            cargoFoodGo: 10_000,
            pathfinding);

        Assert.NotNull(unit);
        Assert.Equal(0, unit!.LeaderId);
    }

    private static Character CreateCharacter(int id, string name, int forceId, Stronghold stronghold)
        => new()
        {
            Id = id,
            Name = name,
            Description = "",
            Portrait = "",
            Personality = new PersonalityData(),
            Proficiency = new ProficiencyData
            {
                Infantry = 1,
                Ride = 1,
                Archery = 1,
                Firelock = 1,
                Sealing = 1,
                Military = 1,
                Fighting = 1,
                Spy = 1,
                Agriculture = 1,
                Commerce = 1,
                Construct = 1,
                Smelt = 1,
                Eloquence = 1,
                Court = 1,
                Sociality = 1,
                Healing = 1
            },
            ForceId = forceId,
            StrongholdId = stronghold.Id,
            LocationStrongholdId = stronghold.Id,
            LocationType = Character.CharacterLocationType.Stronghold,
            Location = stronghold.Location,
            ActionTarget = new Character.CharacterActionTarget { RoutePoints = new Queue<Point2>() },
        };
}
