using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdRecruitTaskTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void AssignMercenaryRecruitTask_PublishesAssignmentWithoutImmediateExecution()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        const int budget = RecruitConstants.MoneyPerKan;
        kiyosu.ForceActor.Money = budget;

        var error = StrongholdRecruitTaskActions.TryAssignMercenaryRecruitTask(
            kiyosu,
            general.Id,
            budget,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(0, kiyosu.ForceActor.Money);
        Assert.NotNull(general.RecruitAssignment);
        Assert.Equal(CharacterRecruitTaskKind.Mercenary, general.RecruitAssignment!.Kind);
        Assert.Equal(budget, general.RecruitAssignment.BudgetMoney);
        Assert.Null(general.RecruitTask);
        Assert.Equal(CharacterForceStatus.Task, general.ForceStatus);
    }

    [Fact]
    public void AssignConscriptRecruitTask_PublishesAssignmentWithoutImmediateExecution()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "柴田胜家");

        PlaceGeneralAtStronghold(general, kiyosu);
        var moneyBefore = kiyosu.ForceActor.Money;

        var error = StrongholdRecruitTaskActions.TryAssignConscriptRecruitTask(
            kiyosu,
            general.Id,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(moneyBefore, kiyosu.ForceActor.Money);
        Assert.NotNull(general.RecruitAssignment);
        Assert.Equal(CharacterRecruitTaskKind.Conscript, general.RecruitAssignment!.Kind);
        Assert.Null(general.RecruitTask);
        Assert.Equal(CharacterForceStatus.Task, general.ForceStatus);
    }

    [Fact]
    public void BeginAssignedRecruitExecution_StartsRecruitTaskAtStronghold()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        const int budget = RecruitConstants.MoneyPerKan;
        kiyosu.ForceActor.Money = budget;

        Assert.Null(StrongholdRecruitTaskActions.TryAssignMercenaryRecruitTask(
            kiyosu,
            general.Id,
            budget,
            gameData,
            loaded.Meta));

        StrongholdRecruitTaskActions.TryBeginAssignedRecruitExecution(general, gameData, loaded.Meta);

        Assert.Null(general.RecruitAssignment);
        Assert.NotNull(general.RecruitTask);
        Assert.Equal(CharacterRecruitTaskPhase.Execute, general.RecruitTask!.Phase);
        Assert.Equal(budget, general.RecruitTask.MoneyRemaining);
    }

    [Fact]
    public void CompleteTask_PublishesRecruitTaskCompletedEvent()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        const int recruited = 12;
        general.RecruitTask = new Domain.Entities.CharacterRecruitTask
        {
            Kind = CharacterRecruitTaskKind.Mercenary,
            StrongholdId = kiyosu.Id,
            ReportStrongholdId = kiyosu.Id,
            Phase = CharacterRecruitTaskPhase.Report,
            BudgetMoney = RecruitConstants.MoneyPerKan * 2,
            MoneyRemaining = 800,
            UsesPersonalFunds = false,
            SoldiersRecruited = recruited,
        };

        var buffer = new SengokuScroll.Strategy.Diagnostics.StrategyDayOutcomeBuffer();
        StrongholdRecruitTaskActions.CompleteTask(general, gameData, loaded.Meta, buffer);

        var evt = Assert.Single(buffer.Events);
        Assert.Equal("RecruitTaskCompleted", evt.Category);
        Assert.Equal(general.Id, evt.CharacterId);
        Assert.Equal(general.Name, evt.CharacterName);
        Assert.Contains("主公", evt.Brief);
        Assert.Contains("募集了士兵", evt.Title);
        Assert.Contains("获得兵士", evt.Message);
        Assert.Contains("2贯", evt.Message);
    }

    [Fact]
    public void MercenaryExecution_CompletesWithSoldiersAndRefundsRemainingMoney()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        var soldiersBefore = kiyosu.ForceActor.Soldier;
        var moneyBefore = kiyosu.ForceActor.Money;
        const int budget = RecruitConstants.MoneyPerKan * 2;
        const int recruited = 12;
        const int refund = 800;

        general.RecruitTask = new Domain.Entities.CharacterRecruitTask
        {
            Kind = CharacterRecruitTaskKind.Mercenary,
            StrongholdId = kiyosu.Id,
            ReportStrongholdId = kiyosu.Id,
            Phase = CharacterRecruitTaskPhase.Report,
            BudgetMoney = budget,
            MoneyRemaining = refund,
            UsesPersonalFunds = false,
            SoldiersRecruited = recruited,
        };

        StrongholdRecruitTaskActions.CompleteTask(general, gameData);

        Assert.Null(general.RecruitTask);
        Assert.Equal(CharacterForceStatus.Idle, general.ForceStatus);
        Assert.Equal(soldiersBefore + recruited, kiyosu.ForceActor.Soldier);
        Assert.Equal(moneyBefore + refund, kiyosu.ForceActor.Money);
        Assert.True(general.Popular >= RecruitConstants.MeritRewardOnComplete);
    }

    [Fact]
    public void MercenaryRecruitTask_DailyExecutionAddsSoldiers()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        kiyosu.Population = 10_000;
        const int budget = RecruitConstants.MoneyPerKan * 2;
        kiyosu.ForceActor.Money = budget;

        Assert.Null(StrongholdRecruitTaskActions.TryAssignMercenaryRecruitTask(
            kiyosu,
            general.Id,
            budget,
            gameData,
            loaded.Meta));

        StrongholdRecruitTaskActions.TryBeginAssignedRecruitExecution(general, gameData, loaded.Meta);
        StrongholdRecruitTaskActions.ProcessDailyTask(general, gameData);

        Assert.NotNull(general.RecruitTask);
        Assert.True(general.RecruitTask!.SoldiersRecruited > 0);
    }

    [Fact]
    public void PersonalMercenaryRecruitTask_DeductsFromCharacterMoney()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var kiyosu = gameData.Strongholds[1];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId
            && c.Name == "林秀贞");

        PlaceGeneralAtStronghold(general, kiyosu);
        kiyosu.LordId = general.Id;
        const int budget = RecruitConstants.MoneyPerKan;
        general.Money = budget;
        var moneyBefore = general.Money;

        var error = StrongholdRecruitTaskActions.TryAssignPersonalMercenaryRecruitTask(
            kiyosu,
            general,
            budget,
            gameData,
            loaded.Meta);

        Assert.Null(error);
        Assert.Equal(moneyBefore - budget, general.Money);
        Assert.True(general.RecruitTask!.UsesPersonalFunds);
    }

    [Fact]
    public void ApplyMonthlyMaintenance_PaysSalaryIntoCharacterTreasury()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var force = gameData.Forces[loaded.Meta.PlayerForceId];
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == force.Id && c.Salary <= 0);
        general.Salary = 120;
        general.Money = 0;

        foreach (var sh in gameData.Strongholds.Values.Where(s => s.ForceId == force.Id))
            sh.ForceActor.Money = 10_000;

        foreach (var c in gameData.Characters.Values.Where(c => c.ForceId == force.Id && c.Id != general.Id))
            c.Salary = 0;

        ForceEconomyActions.ApplyMonthlyMaintenance(force, gameData);

        Assert.Equal(120, general.Money);
    }

    private static void PlaceGeneralAtStronghold(Domain.Entities.Character general, Domain.Entities.Stronghold stronghold)
    {
        general.ForceStatus = CharacterForceStatus.Idle;
        general.RecruitTask = null;
        general.RecruitAssignment = null;
        general.ActionPlan = CharacterActionPlan.Rest;
        general.ActionStatus = CharacterActionStatus.Waiting;
        general.LocationType = CharacterLocationType.Stronghold;
        general.LocationStrongholdId = stronghold.Id;
        general.StrongholdId = stronghold.Id;
        general.Location = stronghold.Location;
    }
}
