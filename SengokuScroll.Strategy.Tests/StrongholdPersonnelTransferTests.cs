using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdPersonnelTransferTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void DispatchCharacter_SchedulesTravelFromOriginToDestination()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var playerForceId = loaded.Meta.PlayerForceId;

        var okazaki = gameData.Strongholds[3];
        var destination = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != okazaki.Id);
        var shibata = gameData.Characters.Values.First(c => c.Name == "柴田胜家");

        PlaceGeneralAtStronghold(shibata, okazaki);

        var error = StrongholdPersonnelActions.TryDispatchCharacter(
            okazaki,
            destination,
            shibata.Id,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(CharacterForceStatus.Task, shibata.ForceStatus);
        Assert.Equal(destination.Id, shibata.ActionTarget.StrongholdId);
        Assert.Equal(destination.Id, shibata.StrongholdId);
    }

    [Fact]
    public void DispatchCharacter_RejectsGeneralNotAtOrigin()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var playerForceId = loaded.Meta.PlayerForceId;

        var okazaki = gameData.Strongholds[3];
        var other = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != okazaki.Id);
        var destination = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != okazaki.Id && sh.Id != other.Id);
        var shibata = gameData.Characters.Values.First(c => c.Name == "柴田胜家");

        PlaceGeneralAtStronghold(shibata, other);

        var error = StrongholdPersonnelActions.TryDispatchCharacter(
            okazaki,
            destination,
            shibata.Id,
            gameData,
            loaded.Meta);

        Assert.Equal(GameError.DomesticError.CharacterNotAtStronghold, error);
    }

    [Fact]
    public void TransferCharacter_SchedulesTravelFromOtherStronghold()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var playerForceId = loaded.Meta.PlayerForceId;

        var okazaki = gameData.Strongholds[3];
        var target = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != okazaki.Id);
        var shibata = gameData.Characters.Values.First(c => c.Name == "柴田胜家");

        PlaceGeneralAtStronghold(shibata, okazaki);

        var error = StrongholdPersonnelActions.TryTransferCharacter(
            target,
            shibata.Id,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(CharacterForceStatus.Task, shibata.ForceStatus);
        Assert.Equal(target.Id, shibata.ActionTarget.StrongholdId);
        Assert.Equal(target.Id, shibata.StrongholdId);
    }

    [Fact]
    public void TransferCharacter_RejectsMayor()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var playerForceId = loaded.Meta.PlayerForceId;

        var target = gameData.Strongholds.Values.First(sh => sh.ForceId == playerForceId);
        var mayorStronghold = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != target.Id);
        var mayor = gameData.Characters.Values.First(c =>
            c.ForceId == playerForceId
            && c.Id != StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
                playerForceId,
                loaded.Meta,
                gameData));
        mayorStronghold.LeaderId = mayor.Id;
        PlaceGeneralAtStronghold(mayor, mayorStronghold);

        var error = StrongholdPersonnelActions.TryTransferCharacter(
            target,
            mayor.Id,
            gameData,
            loaded.Meta);

        Assert.Equal(GameError.DomesticError.CharacterIsStrongholdMayor, error);
    }

    [Fact]
    public void TransferCharacter_RejectsLord()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var playerForceId = loaded.Meta.PlayerForceId;

        var okazaki = gameData.Strongholds[3];
        var target = gameData.Strongholds.Values.First(sh =>
            sh.ForceId == playerForceId && sh.Id != okazaki.Id);
        var lord = gameData.Characters.Values.First(c => c.Name == "林秀贞");

        okazaki.LordId = lord.Id;
        PlaceGeneralAtStronghold(lord, okazaki);

        var error = StrongholdPersonnelActions.TryTransferCharacter(
            target,
            lord.Id,
            gameData,
            loaded.Meta);

        Assert.Equal(GameError.DomesticError.CharacterIsStrongholdLord, error);
    }

    private static void PlaceGeneralAtStronghold(Character general, Stronghold stronghold)
    {
        general.LocationType = CharacterLocationType.Stronghold;
        general.LocationStrongholdId = stronghold.Id;
        general.StrongholdId = stronghold.Id;
        general.ForceStatus = CharacterForceStatus.Idle;
        general.ActionPlan = CharacterActionPlan.Rest;
        general.ActionStatus = CharacterActionStatus.Waiting;
        general.RecruitTask = null;
        general.RecruitAssignment = null;
    }
}
