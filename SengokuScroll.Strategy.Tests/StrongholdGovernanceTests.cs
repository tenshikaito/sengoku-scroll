using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdGovernanceTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void SetGovernancePriority_DirectRuleRemoteStronghold_DispatchesMessenger()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var state = host.GetState().Value!;
        var residence = state.Strongholds.First(s =>
            s.ForceId == state.PlayerForceId && s.IsLordResidence);
        var remote = state.Strongholds.First(s =>
            s.ForceId == state.PlayerForceId
            && s.IsDirectRule
            && (s.X != residence.X || s.Y != residence.Y));

        var result = host.SetStrongholdGovernancePriority(
            remote.Id,
            StrongholdGovernancePriority.Military);

        Assert.True(result.IsSuccess);
        Assert.Equal("CarrierDispatched", result.Value!.Outcome);
        Assert.Equal(
            "Autonomous",
            result.Value.State.Strongholds.First(s => s.Id == remote.Id).GovernancePriority);
        Assert.Single(result.Value.State.MessageCarriers);
        Assert.Equal(
            "GovernancePriorityChange",
            result.Value.State.MessageCarriers[0].PayloadType);
    }

    [Fact]
    public void IssueGovernancePriorityChange_DirectRuleRemote_DispatchesMessenger()
    {
        var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var gameData = ctx.World.GameData;
        var residence = gameData.Strongholds[1];
        var remote = StrategyTestWorldBuilder.CreateTestStronghold(2, 1, new Point3(5, 0));
        remote.LordId = 0;
        gameData.Strongholds[2] = remote;
        Domain.Actions.MapLocationActions.RegisterStronghold(ctx.World, remote);

        var helper = ctx.Services.GetRequiredService<MessageCarrierDispatchHelper>();
        var change = new PendingStrongholdGovernanceChange
        {
            Priority = StrongholdGovernancePriority.Domestic
        };
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            LordUnitId = 1,
            LordName = "测试当主"
        };

        var outcome = helper.IssueGovernancePriorityChange(
            residence.Location,
            residence.Id,
            remote,
            change,
            meta);

        Assert.Equal(MessageCarrierDispatchOutcome.CarrierDispatched, outcome);
        Assert.Equal(
            StrongholdGovernancePriority.Autonomous,
            remote.GovernancePriority);
        var carrier = Assert.Single(gameData.MessageCarriers.Values);
        Assert.Equal(MessagePayloadType.GovernancePriorityChange, carrier.Payload.Type);
        Assert.Equal(remote.Id, carrier.Payload.TargetStrongholdId);
        Assert.Equal(StrongholdGovernancePriority.Domestic, carrier.Payload.PendingGovernanceChange!.Priority);
    }

    [Fact]
    public void SetGovernancePriority_DirectRuleWithoutMayor_Succeeds()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var kiyosu = loaded.World.GameData.Strongholds[1];
        kiyosu.LordId = 0;
        kiyosu.LeaderId = 0;

        var error = StrongholdGovernanceActions.TrySetGovernancePriority(
            kiyosu,
            StrongholdGovernancePriority.Military,
            loaded.World.GameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(StrongholdGovernancePriority.Military, kiyosu.GovernancePriority);
    }

    [Fact]
    public void SetGovernancePriority_WithMayor_Succeeds()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.LordId = 0;
        var mayor = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "林秀贞");
        kiyosu.LeaderId = mayor.Id;

        var error = StrongholdGovernanceActions.TrySetGovernancePriority(
            kiyosu,
            StrongholdGovernancePriority.Domestic,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(StrongholdGovernancePriority.Domestic, kiyosu.GovernancePriority);
    }

    [Fact]
    public void MonthlyMilitaryPriority_AssignsConscriptToIdleGeneral()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.GovernancePriority = StrongholdGovernancePriority.Military;
        kiyosu.LeaderId = gameData.Characters.Values.First(c => c.Name == "林秀贞").Id;

        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(general, kiyosu);
        kiyosu.Population = 5000;

        StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
            kiyosu,
            gameData,
            loaded.Meta);

        Assert.NotNull(general.RecruitAssignment);
        Assert.Equal(CharacterRecruitTaskKind.Conscript, general.RecruitAssignment!.Kind);
    }

    [Fact]
    public void MonthlyMilitaryPriority_AssignsMercenaryWhenConscriptUnavailable()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.GovernancePriority = StrongholdGovernancePriority.Military;
        kiyosu.LeaderId = gameData.Characters.Values.First(c => c.Name == "林秀贞").Id;

        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(general, kiyosu);
        kiyosu.Population = 0;
        const int budgetPool = RecruitConstants.MoneyPerKan * 100;
        kiyosu.ForceActor.Money = budgetPool;

        StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
            kiyosu,
            gameData,
            loaded.Meta);

        Assert.NotNull(general.RecruitAssignment);
        Assert.Equal(CharacterRecruitTaskKind.Mercenary, general.RecruitAssignment!.Kind);
        Assert.True(general.RecruitAssignment.BudgetMoney > 0);
        Assert.True(kiyosu.ForceActor.Money < budgetPool);
    }

    [Fact]
    public void MonthlyGovernanceSystem_RunsOnlyOnFirstDayOfMonth()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.GovernancePriority = StrongholdGovernancePriority.Military;
        kiyosu.LeaderId = gameData.Characters.Values.First(c => c.Name == "林秀贞").Id;

        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(general, kiyosu);
        kiyosu.ForceActor.Money = RecruitConstants.MoneyPerKan * 100;
        kiyosu.Population = 5000;

        gameData.GameDate = new GameDate(1560, 6, 2);
        var gameContext = ctx.Services.GetRequiredService<IGameContext>();
        var system = new StrategyStrongholdGovernanceSystem(gameContext, loaded.Meta);
        system.Update();
        Assert.Null(general.RecruitAssignment);

        gameData.GameDate = new GameDate(1560, 7, 1);
        system.Update();
        Assert.NotNull(general.RecruitAssignment);
    }

    [Fact]
    public void InnerVassalStronghold_PlayerCannotSetGovernancePriority()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var innerVassalStronghold = gameData.Strongholds.Values.First(s => s.ForceId == 3);

        Assert.False(StrongholdGovernanceRules.CanPlayerConfigureGovernancePolicy(
            innerVassalStronghold,
            loaded.Meta,
            gameData));

        var error = StrongholdGovernanceActions.TrySetGovernancePriority(
            innerVassalStronghold,
            StrongholdGovernancePriority.Military,
            gameData,
            loaded.Meta);

        Assert.Equal(GameError.DiplomacyError.NotSelfForce, error);
    }

    [Fact]
    public void AutonomousDefault_IsZeroOnNewStronghold()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var kiyosu = loaded.World.GameData.Strongholds[1];
        Assert.Equal(StrongholdGovernancePriority.Autonomous, kiyosu.GovernancePriority);
    }

    [Fact]
    public void AutonomousWithLowGarrison_AssignsMilitaryTasks()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.GovernancePriority = StrongholdGovernancePriority.Autonomous;
        kiyosu.LeaderId = gameData.Characters.Values.First(c => c.Name == "林秀贞").Id;
        kiyosu.ForceActor.Soldier = 50;
        kiyosu.Population = 8000;

        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(general, kiyosu);

        StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
            kiyosu,
            gameData,
            loaded.Meta);

        Assert.NotNull(general.RecruitAssignment);
    }

    [Fact]
    public void AutonomousWithLowStability_SkipsMonthlyAssignments()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        kiyosu.GovernancePriority = StrongholdGovernancePriority.Autonomous;
        kiyosu.LeaderId = gameData.Characters.Values.First(c => c.Name == "林秀贞").Id;
        kiyosu.Stability = 20;
        kiyosu.CivilianActor.PopularFeelings = 20;
        kiyosu.ForceActor.Soldier = 50;
        kiyosu.Population = 8000;
        kiyosu.ForceActor.Money = RecruitConstants.MoneyPerKan * 100;

        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(general, kiyosu);

        StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
            kiyosu,
            gameData,
            loaded.Meta);

        Assert.Null(general.RecruitAssignment);
    }

    [Fact]
    public void EvaluateMonthlyFocus_BoldOfficialLeansMilitary()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var mayor = gameData.Characters.Values.First(c => c.Name == "林秀贞");
        kiyosu.LeaderId = mayor.Id;
        kiyosu.ForceActor.Soldier = 80;
        kiyosu.Population = 6000;
        mayor.Personality.Courage = 95;
        mayor.Personality.Ambition = 90;
        mayor.Power = 85;

        var focus = StrongholdGovernanceEvaluator.EvaluateMonthlyFocus(kiyosu, gameData, loaded.Meta);
        Assert.Equal(StrongholdGovernanceMonthlyFocus.Military, focus);
    }

    [Fact]
    public void ListGovernanceAssignableGenerals_ExcludesForceLordAndStrongholdLord()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var lord = gameData.Characters.Values.First(c => c.Name == "林秀贞");
        kiyosu.LordId = lord.Id;
        PlaceGeneralAtStronghold(lord, kiyosu);

        var forceLord = gameData.Characters.Values.First(c =>
            c.Id == StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
                kiyosu.ForceId,
                loaded.Meta,
                gameData));
        PlaceGeneralAtStronghold(forceLord, kiyosu);

        var idle = gameData.Characters.Values.First(c => c.Name == "柴田胜家");
        PlaceGeneralAtStronghold(idle, kiyosu);

        var assignable = StrongholdGovernanceRules
            .ListGovernanceAssignableGenerals(kiyosu, gameData, loaded.Meta)
            .Select(c => c.Id)
            .ToList();

        Assert.Contains(idle.Id, assignable);
        Assert.DoesNotContain(lord.Id, assignable);
        Assert.DoesNotContain(forceLord.Id, assignable);
    }

    private static void PlaceGeneralAtStronghold(Character character, Stronghold stronghold)
    {
        character.ForceStatus = CharacterForceStatus.Idle;
        character.RecruitTask = null;
        character.RecruitAssignment = null;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.LocationType = CharacterLocationType.Stronghold;
        character.LocationStrongholdId = stronghold.Id;
        character.StrongholdId = stronghold.Id;
        character.Location = stronghold.Location;
    }
}
