using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Types;
using SengokuScroll.Domain.Definitions;
using static SengokuScroll.Domain.Definitions.CharacterDefinition;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class UnitCommanderEscapeTests
{
    private static StrategyTestContext CreateEscapeWorld()
    {
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            LordUnitId = 1,
            LordName = "测试当主",
            ForceLordCharacterIds = new Dictionary<int, int> { [1] = 100 }
        };

        return StrategyTestWorldFactory.CreateFromWorld(
            StrategyTestWorldBuilder.BuildLogisticsWorld(new Point3(5, 0)),
            meta);
    }

    [Fact]
    public void ReleaseToMapAndRouteHome_DoesNotTeleportToResidenceSameDay()
    {
        using var ctx = CreateEscapeWorld();
        var gameContext = ctx.Services.GetRequiredService<IGameContext>();
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        var unit = ctx.World.GameData.Units[1];
        unit.LeaderId = 100;

        var commander = CreateCommander(100, 1, 1, new Point3(5, 0));
        commander.LocationType = CharacterLocationType.Unit;
        ctx.World.GameData.Characters[100] = commander;

        UnitCommanderEscapeHelper.ReleaseToMapAndRouteHome(
            gameContext.GameWorldContext,
            commander,
            new Point3(5, 0),
            ctx.World.GameData,
            meta,
            pathfinding);

        Assert.Equal(CharacterLocationType.Map, commander.LocationType);
        Assert.Equal(5, commander.Location.X);
        Assert.Equal(0, commander.Location.Y);

        var lordLocation = StrategyLordHelper.ResolveLocation(ctx.World.GameData, meta);
        Assert.Equal(5, lordLocation.X);
        Assert.Equal(0, lordLocation.Y);
    }

    [Fact]
    public void CommanderEscape_MovesTowardHomeAtDailyPace_NotInstantly()
    {
        using var ctx = CreateEscapeWorld();
        var gameContext = ctx.Services.GetRequiredService<IGameContext>();
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        var commander = CreateCommander(100, 1, 1, new Point3(5, 0));
        commander.LocationType = CharacterLocationType.Unit;
        ctx.World.GameData.Units[1].LeaderId = 100;
        ctx.World.GameData.Characters[100] = commander;

        UnitCommanderEscapeHelper.ReleaseToMapAndRouteHome(
            gameContext.GameWorldContext,
            commander,
            new Point3(5, 0),
            ctx.World.GameData,
            meta,
            pathfinding);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(CharacterLocationType.Map, commander.LocationType);
        Assert.InRange(commander.Location.X, 1, 4);

        var lordLocation = StrategyLordHelper.ResolveLocation(ctx.World.GameData, meta);
        Assert.Equal(commander.Location.X, lordLocation.X);
    }

    [Fact]
    public void ResolveAnnihilatedUnit_EscapedCommanderStaysOnMapWhenPathfindingUnavailable()
    {
        using var ctx = CreateEscapeWorld();
        var gameContext = ctx.Services.GetRequiredService<IGameContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        var winner = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(6, 0));
        winner.Soldier = 2000;
        ctx.World.GameData.Units[2] = winner;
        MapLocationActions.RegisterUnit(ctx.World, winner);

        var loser = ctx.World.GameData.Units[1];
        loser.Soldier = 0;
        loser.LeaderId = 100;

        var commander = CreateCommander(100, 1, 1, new Point3(5, 0));
        commander.LocationType = CharacterLocationType.Unit;
        ctx.World.GameData.Characters[100] = commander;

        UnitDestructionRules.ResolveAnnihilatedUnit(
            gameContext.GameWorldContext,
            loser,
            victor: null,
            ctx.World.GameData,
            meta,
            null,
            pathfinding: null);

        Assert.Equal(CharacterLocationType.Map, commander.LocationType);
        Assert.Equal(5, commander.Location.X);

        var residence = StrategyLordHelper.ResolveLordResidenceStrongholdId(1, ctx.World.GameData, meta);
        Assert.True(residence > 0);
        Assert.NotEqual(
            ctx.World.GameData.Strongholds[residence].Location.X,
            commander.Location.X);
    }

    private static Character CreateCommander(int id, int forceId, int strongholdId, Point3 location)
        => new()
        {
            Id = id,
            Name = "测试当主",
            Description = "测试",
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
            StrongholdId = strongholdId,
            Location = location,
            Birthday = new GameDate(1530, 1, 1),
            ActionTarget = new Character.CharacterActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };
}
