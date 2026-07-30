using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Diplomacy;

namespace SengokuScroll.Strategy.Tests;

public class DiplomacyMissionTests
{
    [Fact]
    public void AssignMission_AdvanceDays_ChangesRelationOnSuccess()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);

        var gameData = ctx.World.GameData;
        const int envoyId = 4;
        const int targetForceId = 5;

        Assert.True(gameData.Characters.TryGetValue(envoyId, out var envoy));
        var residenceId = loaded.Meta.ForceLordResidenceStrongholdIds[loaded.Meta.PlayerForceId];
        envoy.LocationType = CharacterLocationType.Stronghold;
        envoy.LocationStrongholdId = residenceId;
        envoy.StrongholdId = residenceId;
        envoy.ForceStatus = CharacterForceStatus.Idle;
        envoy.RecruitTask = null;
        envoy.RecruitAssignment = null;
        envoy.DiplomacyMission = null;

        Assert.True(DiplomacyMissionActions.TryAssignMission(
            gameData,
            meta,
            envoyId,
            targetForceId,
            "Ally",
            out var assignError));
        Assert.Null(assignError);
        Assert.NotNull(envoy.DiplomacyMission);

        // 业务：测试聚焦任务流转，强制成功以验证关系写入
        envoy.DiplomacyMission!.SuccessChancePercent = 100;
        var travelDays = envoy.DiplomacyMission.RemainingDays;

        for (var day = 0; day < travelDays; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Null(envoy.DiplomacyMission);
        Assert.Equal(CharacterForceStatus.Idle, envoy.ForceStatus);

        var oda = gameData.Forces[meta.PlayerForceId];
        var diplomacy = oda.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId);
        Assert.NotNull(diplomacy);
        Assert.Equal(DiplomacyRelation.Allied, diplomacy!.Relation);
    }

    [Fact]
    public void AssignMission_WarOnAlreadyEnemyForce_IsRejected()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);

        var gameData = ctx.World.GameData;
        const int envoyId = 4;
        const int targetForceId = 5;
        var playerForceId = meta.PlayerForceId;

        Assert.True(gameData.Forces.TryGetValue(playerForceId, out var playerForce));
        playerForce.Diplomacies.RemoveAll(d => d.TargetForceId == targetForceId);
        playerForce.Diplomacies.Add(new Diplomacy
        {
            TargetForceId = targetForceId,
            Relation = DiplomacyRelation.Enemy,
        });

        Assert.True(gameData.Characters.TryGetValue(envoyId, out var envoy));
        var residenceId = loaded.Meta.ForceLordResidenceStrongholdIds[playerForceId];
        envoy.LocationType = CharacterLocationType.Stronghold;
        envoy.LocationStrongholdId = residenceId;
        envoy.StrongholdId = residenceId;
        envoy.ForceStatus = CharacterForceStatus.Idle;
        envoy.RecruitTask = null;
        envoy.RecruitAssignment = null;
        envoy.DiplomacyMission = null;

        Assert.False(DiplomacyMissionActions.TryAssignMission(
            gameData,
            meta,
            envoyId,
            targetForceId,
            "War",
            out var error));
        Assert.Equal(GameError.DiplomacyError.EnemyForce, error);
    }

    [Fact]
    public void AssignMission_AllyOnAlreadyAlliedForce_IsRejected()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);

        var gameData = ctx.World.GameData;
        const int envoyId = 4;
        const int targetForceId = 5;
        var playerForceId = meta.PlayerForceId;

        Assert.True(gameData.Forces.TryGetValue(playerForceId, out var playerForce));
        playerForce.Diplomacies.RemoveAll(d => d.TargetForceId == targetForceId);
        playerForce.Diplomacies.Add(new Diplomacy
        {
            TargetForceId = targetForceId,
            Relation = DiplomacyRelation.Allied,
        });

        Assert.True(gameData.Characters.TryGetValue(envoyId, out var envoy));
        var residenceId = loaded.Meta.ForceLordResidenceStrongholdIds[playerForceId];
        envoy.LocationType = CharacterLocationType.Stronghold;
        envoy.LocationStrongholdId = residenceId;
        envoy.StrongholdId = residenceId;
        envoy.ForceStatus = CharacterForceStatus.Idle;
        envoy.DiplomacyMission = null;

        Assert.False(DiplomacyMissionActions.TryAssignMission(
            gameData,
            meta,
            envoyId,
            targetForceId,
            "Ally",
            out var error));
        Assert.Equal(GameError.DiplomacyError.AllyForce, error);
    }

    [Fact]
    public void AssignMission_PeaceOnNeutralForce_IsRejected()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);

        var gameData = ctx.World.GameData;
        const int envoyId = 4;
        const int targetForceId = 5;
        var playerForceId = meta.PlayerForceId;

        Assert.True(gameData.Forces.TryGetValue(playerForceId, out var playerForce));
        playerForce.Diplomacies.RemoveAll(d => d.TargetForceId == targetForceId);
        playerForce.Diplomacies.Add(new Diplomacy
        {
            TargetForceId = targetForceId,
            Relation = DiplomacyRelation.Neutral,
        });

        Assert.True(gameData.Characters.TryGetValue(envoyId, out var envoy));
        var residenceId = loaded.Meta.ForceLordResidenceStrongholdIds[playerForceId];
        envoy.LocationType = CharacterLocationType.Stronghold;
        envoy.LocationStrongholdId = residenceId;
        envoy.StrongholdId = residenceId;
        envoy.ForceStatus = CharacterForceStatus.Idle;
        envoy.DiplomacyMission = null;

        Assert.False(DiplomacyMissionActions.TryAssignMission(
            gameData,
            meta,
            envoyId,
            targetForceId,
            "Peace",
            out var error));
        Assert.Equal(GameError.DiplomacyError.NotEnemyForce, error);
    }

    [Fact]
    public void IsMissionAllowedForDiplomacyStatus_IgnoresRelationshipValue()
    {
        Assert.True(DiplomacyMissionRules.IsMissionAllowedForDiplomacyStatus(
            "Ally",
            DiplomacyRelation.Neutral,
            out var error));
        Assert.Null(error);

        Assert.False(DiplomacyMissionRules.IsMissionAllowedForDiplomacyStatus(
            "Ally",
            DiplomacyRelation.Allied,
            out error));
        Assert.Equal(GameError.DiplomacyError.AllyForce, error);
    }
}
