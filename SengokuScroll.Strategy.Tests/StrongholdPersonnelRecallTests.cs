using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdPersonnelRecallTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void Recall_SettlesMercenaryTaskWithHalfEffectAndRefund()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var stronghold = gameData.Strongholds.Values.First(sh => sh.ForceId == loaded.Meta.PlayerForceId);
        var general = gameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "柴田胜家");

        const int budget = 20_000;
        stronghold.ForceActor.Money = budget + 10_000;
        stronghold.ForceActor.Soldier = 100;

        general.RecruitTask = new CharacterRecruitTask
        {
            Kind = CharacterRecruitTaskKind.Mercenary,
            StrongholdId = stronghold.Id,
            ReportStrongholdId = stronghold.Id,
            Phase = CharacterRecruitTaskPhase.Execute,
            BudgetMoney = budget,
            MoneyRemaining = budget / 2,
            SoldiersRecruited = 100,
            ExecutionDaysRemaining = RecruitConstants.ExecutionDays / 2,
        };
        general.ForceStatus = CharacterForceStatus.Task;
        general.ActionPlan = CharacterActionPlan.Task;

        var moneyBefore = stronghold.ForceActor.Money;
        var error = StrongholdPersonnelActions.ApplyCharacterRecall(general, gameData, loaded.Meta);

        Assert.Null(error);
        Assert.Null(general.RecruitTask);
        Assert.Equal(100 + 50, stronghold.ForceActor.Soldier);
        Assert.Equal(moneyBefore + budget / 2, stronghold.ForceActor.Money);
    }

    [Fact]
    public void Recall_RejectsIdleGeneral()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");

        general.ForceStatus = CharacterForceStatus.Idle;
        general.RecruitTask = null;
        general.RecruitAssignment = null;

        var error = StrongholdPersonnelActions.ApplyCharacterRecall(general, gameData, loaded.Meta);

        Assert.Equal(GameError.DomesticError.CharacterNotOnRecallableTask, error);
    }

    [Fact]
    public void Recall_RefundsMercenaryAssignmentBudget()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var gameData = ctx.World.GameData;
        var stronghold = gameData.Strongholds.Values.First(sh => sh.ForceId == loaded.Meta.PlayerForceId);
        var general = gameData.Characters.Values.First(c => c.Name == "柴田胜家");

        const int budget = 5000;
        stronghold.ForceActor.Money = 1000;
        general.RecruitAssignment = new CharacterRecruitAssignment
        {
            Kind = CharacterRecruitTaskKind.Mercenary,
            StrongholdId = stronghold.Id,
            BudgetMoney = budget,
        };
        general.ForceStatus = CharacterForceStatus.Task;
        general.ActionPlan = CharacterActionPlan.Task;

        var error = StrongholdPersonnelActions.ApplyCharacterRecall(general, gameData, loaded.Meta);

        Assert.Null(error);
        Assert.Null(general.RecruitAssignment);
        Assert.Equal(1000 + budget, stronghold.ForceActor.Money);
    }
}
