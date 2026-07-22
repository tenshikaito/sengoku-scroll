using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdLordActionsTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void AppointLord_FormerMayor_ClearsMayorRoleAndBecomesLord()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        var kiyosu = gameData.Strongholds[1];
        var target = gameData.Strongholds.Values.First(s =>
            s.ForceId == loaded.Meta.PlayerForceId && s.Id != 1 && s.LordId == 0);
        var hayashi = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        Assert.True(StrongholdDomesticRules.IsLordAtResidence(loaded.Meta, gameData));
        Assert.Equal(hayashi.Id, kiyosu.LeaderId);

        var error = StrongholdLordActions.TryAppointLord(
            target,
            hayashi.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Null(error);
        Assert.Equal(0, kiyosu.LeaderId);
        Assert.Equal(hayashi.Id, target.LordId);
    }

    [Fact]
    public void AppointMayor_RejectsCharacterWhoIsLord()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        var kiyosu = gameData.Strongholds[1];
        var shibata = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        kiyosu.LordId = shibata.Id;

        var error = StrongholdLordActions.TryAppointMayor(
            kiyosu,
            shibata.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding);

        Assert.Equal(GameError.DomesticError.CharacterIsStrongholdLord, error);
    }

    [Fact]
    public void AppointMayor_RejectsForceLord()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var lordRegistry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");

        var error = StrongholdLordActions.TryAppointMayor(
            gameData.Strongholds[1],
            nobunaga.Id,
            gameData,
            loaded.Meta,
            worldContext,
            pathfinding,
            lordRegistry);

        Assert.Equal(GameError.DomesticError.CharacterIsForceLord, error);
    }

    [Fact]
    public void Succession_ReleasesDeceasedLordAndSuccessorGovernanceRoles()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var registry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();
        var events = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        var meta = loaded.Meta;

        var nobunaga = gameData.Characters.Values.First(c => c.Name == "织田信长");
        var shibata = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        var kiyosu = gameData.Strongholds[1];
        var okazaki = gameData.Strongholds.Values.First(s => s.ForceId == meta.PlayerForceId && s.Id != 1);

        okazaki.LordId = nobunaga.Id;
        kiyosu.LeaderId = shibata.Id;
        gameData.Forces[meta.PlayerForceId].Successor = shibata.Id;
        nobunaga.IsDead = true;

        var reason = ForceSuccessionRules.TryResolveAfterLordRemoved(
            meta.PlayerForceId,
            conquerorForceId: 2,
            lordCaptured: false,
            lordKilled: true,
            gameData,
            ctx.World.GameMasterData,
            meta,
            registry,
            events,
            removedLordCharacterId: nobunaga.Id);

        Assert.Equal(ForceSuccessionRules.LordRemovalReason.KilledWithSuccession, reason);
        Assert.Equal(0, okazaki.LordId);
        Assert.Equal(0, kiyosu.LeaderId);
    }
}
