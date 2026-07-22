using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

/// <summary>据点任命指令：当主驻居城时对本家据点下达任命。</summary>
public class StrongholdLordCommandTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void CanLordCommandAtStronghold_FromResidence_AllowsRemoteDirectRuleStronghold()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var residence = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];

        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));
        Assert.NotEqual(residence.Id, okazaki.Id);
        Assert.True(StrategyStrongholdLordHelper.IsDirectRule(okazaki));
    }

    [Fact]
    public void AppointLord_FromResidence_ToRemoteDirectRuleStronghold_Succeeds()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        var okazaki = gameData.Strongholds[3];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));

        var error = StrongholdLordActions.TryAppointLord(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Null(error);
        Assert.Equal(hayashi.Id, okazaki.LordId);
    }

    [Fact]
    public void AppointStrongholdLord_ViaHost_FromResidence_ToOkazaki_Succeeds()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var state = host.GetState().Value!;
        var residence = state.Strongholds.First(s =>
            s.ForceId == state.PlayerForceId && s.IsLordResidence);
        var okazaki = state.Strongholds.First(s =>
            s.Id == 3 && s.ForceId == state.PlayerForceId);
        var hayashi = state.Characters!.First(c => c.Name == "林秀贞");

        Assert.Equal("清洲", residence.Name);
        Assert.Equal("冈崎", okazaki.Name);
        Assert.Equal(residence.X, state.Lord.X);
        Assert.Equal(residence.Y, state.Lord.Y);

        var result = host.AppointStrongholdLord(okazaki.Id, hayashi.Id, "Lord");

        Assert.True(result.IsSuccess, result.Error?.Code ?? "unknown error");
        var updated = result.Value!.Strongholds.First(s => s.Id == okazaki.Id);
        Assert.Equal(hayashi.Id, updated.LordId);
        Assert.False(updated.IsDirectRule);
    }

    [Fact]
    public void AppointLord_AfterLordLeavesResidenceTile_ReturnsLordNotAtResidence()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        nobunaga.LocationType = CharacterLocationType.Map;
        nobunaga.Location = new Point3(0, 0);
        nobunaga.LocationStrongholdId = 0;

        var okazaki = gameData.Strongholds[3];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        Assert.False(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.False(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));

        var error = StrongholdLordActions.TryAppointLord(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Equal(GameError.DomesticError.LordNotAtResidence, error);
    }

    [Fact]
    public void AppointLord_AfterLeaveStrongholdOnSameTile_StillAtResidence()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];

        var leave = CharacterPlayerActions.TryLeaveStronghold(
            worldContext,
            nobunaga,
            gameData,
            loaded.Meta,
            gateApCost: 1,
            forceUnderBlockade: false,
            simulationSeed: gameData.SimulationSeed,
            out _);

        Assert.True(leave.IsSuccess);
        Assert.Equal(CharacterLocationType.Map, nobunaga.LocationType);
        Assert.True(nobunaga.Location.IsSameTile(kiyosu.Location));

        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));

        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");
        var error = StrongholdLordActions.TryAppointLord(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Null(error);
    }

    [Fact]
    public void LordDtoLocation_UsesActualStronghold_NotNominalHome()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];

        nobunaga.LocationType = CharacterLocationType.Stronghold;
        nobunaga.LocationStrongholdId = okazaki.Id;
        nobunaga.StrongholdId = kiyosu.Id;
        nobunaga.Location = okazaki.Location;

        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta);

        Assert.Equal(okazaki.Location.X, dto.Lord.X);
        Assert.Equal(okazaki.Location.Y, dto.Lord.Y);
        Assert.False(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
    }

    [Fact]
    public void AppointLord_FromOkazakiTile_ToRemoteStronghold_ReturnsLordNotAtResidence()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var okazaki = gameData.Strongholds[3];
        var kiyosu = gameData.Strongholds[1];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        nobunaga.LocationType = CharacterLocationType.Map;
        nobunaga.Location = okazaki.Location;
        nobunaga.LocationStrongholdId = 0;

        Assert.False(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));
        Assert.False(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, kiyosu));

        var error = StrongholdLordActions.TryAppointLord(
            kiyosu,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Equal(GameError.DomesticError.LordNotAtResidence, error);
    }

    [Fact]
    public void AppointMayor_FromResidence_ToOkazaki_Succeeds()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var lordRegistry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();

        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        Assert.Equal(hayashi.Id, kiyosu.LeaderId);
        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));

        var error = StrongholdLordActions.TryAppointMayor(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding,
            lordRegistry);

        Assert.Null(error);
        Assert.Equal(hayashi.Id, okazaki.LeaderId);
        Assert.Equal(0, kiyosu.LeaderId);
        Assert.Equal(CharacterLocationType.Stronghold, hayashi.LocationType);
        Assert.Equal(CharacterActionPlan.Task, hayashi.ActionPlan);
        Assert.Equal(CharacterForceStatus.Task, hayashi.ForceStatus);
        Assert.Equal(okazaki.Id, hayashi.ActionTarget.StrongholdId);
        Assert.Equal(CharacterActionStatus.Waiting, hayashi.ActionStatus);
        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, kiyosu));
    }

    [Fact]
    public void AppointStrongholdMayor_ViaHost_FromResidence_ToOkazaki_Succeeds()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var before = host.GetState().Value!;
        var kiyosu = before.Strongholds.First(s => s.Id == 1);
        var okazaki = before.Strongholds.First(s => s.Id == 3);
        var hayashi = before.Characters!.First(c => c.Name == "林秀贞");

        Assert.Equal("林秀贞", kiyosu.MayorName);
        Assert.Equal(kiyosu.X, before.Lord.X);
        Assert.Equal(kiyosu.Y, before.Lord.Y);

        var result = host.AppointStrongholdLord(okazaki.Id, hayashi.Id, "Mayor");

        Assert.True(result.IsSuccess, result.Error?.Code ?? "unknown error");

        var after = result.Value!;
        var updatedOkazaki = after.Strongholds.First(s => s.Id == okazaki.Id);
        var updatedKiyosu = after.Strongholds.First(s => s.Id == kiyosu.Id);

        Assert.Equal("林秀贞", updatedOkazaki.MayorName);
        Assert.True(string.IsNullOrWhiteSpace(updatedKiyosu.MayorName));
        Assert.Equal(kiyosu.X, after.Lord.X);
        Assert.Equal(kiyosu.Y, after.Lord.Y);
    }

    [Fact]
    public void CanLordCommand_AfterLordVisitedOkazakiAndReturnedToKiyosuTile_AllowsRemoteAppoint()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var lordRegistry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        UnitCommanderEscapeHelper.EnterStronghold(nobunaga, okazaki, loaded.Meta, gameData);
        Assert.Equal(1, nobunaga.StrongholdId);

        var leave = CharacterPlayerActions.TryLeaveStronghold(
            worldContext,
            nobunaga,
            gameData,
            loaded.Meta,
            gateApCost: 1,
            forceUnderBlockade: false,
            simulationSeed: gameData.SimulationSeed,
            out _);
        Assert.True(leave.IsSuccess);

        nobunaga.Location = kiyosu.Location;

        Assert.Equal(1, StrategyLordHelper.ResolveLordResidenceStrongholdId(
            loaded.Meta.PlayerForceId,
            gameData,
            loaded.Meta));
        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.True(StrongholdDomesticRules.CanLordCommandAtStronghold(loaded.Meta, gameData, okazaki));

        var error = StrongholdLordActions.TryAppointMayor(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding,
            lordRegistry);

        Assert.Null(error);
    }

    [Fact]
    public void IsLordAtResidence_UsesMetaResidence_EvenWhenLordStrongholdIdWasCorruptedByVisit()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var kiyosu = gameData.Strongholds[1];

        nobunaga.StrongholdId = 3;
        nobunaga.LocationType = CharacterLocationType.Map;
        nobunaga.Location = kiyosu.Location;
        nobunaga.LocationStrongholdId = 0;

        Assert.Equal(1, StrategyLordHelper.ResolveLordResidenceStrongholdId(
            loaded.Meta.PlayerForceId,
            gameData,
            loaded.Meta));
        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
    }

    [Fact]
    public void AppointMayor_CharacterAiOnDayAdvance_StartsRouteWithoutInstantLeave()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var lordRegistry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();
        var characterAi = ctx.Services.GetRequiredService<IStrategyCharacterAISystem>();

        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds[3];
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        var error = StrongholdLordActions.TryAppointMayor(
            okazaki,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding,
            lordRegistry);

        Assert.Null(error);
        Assert.Equal(CharacterLocationType.Stronghold, hayashi.LocationType);
        Assert.Equal(kiyosu.Id, hayashi.LocationStrongholdId);

        characterAi.Update();

        Assert.Equal(CharacterActionStatus.Moving, hayashi.ActionStatus);
        Assert.Equal(CharacterLocationType.Map, hayashi.LocationType);
        Assert.Equal(okazaki.Id, hayashi.ActionTarget.StrongholdId);
        Assert.True(hayashi.ActionTarget.RoutePoints.Count > 0);
    }
}
