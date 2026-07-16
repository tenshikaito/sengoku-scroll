using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>M4-a 据点生产与市民口粮。</summary>
public class StrongholdEconomyActionsTests
{
    [Fact]
    public void ApplyDailyProduction_AddsFoodAndMoneyToCivilianActor()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.CivilianActor.Food = 0;
        stronghold.CivilianActor.Money = 0;

        var (food, money) = StrongholdEconomyActions.ApplyDailyProduction(
            stronghold,
            StrategyTestWorldBuilder.BuildMinimalWorld().GameData);

        Assert.Equal(500, food);
        Assert.Equal(333, money);
        Assert.Equal(500, stronghold.CivilianActor.Food);
        Assert.Equal(333, stronghold.CivilianActor.Money);
    }

    [Fact]
    public void ApplyDailyCivilianFoodConsumption_DeductsFromCivilianFood()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.Population = 100;
        stronghold.CivilianActor.Food = 1000;

        var deducted = StrongholdEconomyActions.ApplyDailyCivilianFoodConsumption(stronghold);

        Assert.Equal(200, deducted);
        Assert.Equal(800, stronghold.CivilianActor.Food);
    }
}

/// <summary>M4-a 整型经济计算。</summary>
public class EconomyCalculatorIntegerTests
{
    [Fact]
    public void ApplyBasisPointsTax_FloorsResult()
    {
        Assert.Equal(250, EconomyCalculator.ApplyBasisPointsTax(1000, 2500));
    }

    [Fact]
    public void CalculateCollectionEfficiencyBp_ClampsToMinimum()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.Authority = 0;
        stronghold.Corruption = 100;
        stronghold.IsHistorical = false;

        var bp = EconomyCalculator.CalculateCollectionEfficiencyBp(stronghold);

        Assert.Equal(EconomyConstants.MinCollectionEfficiencyBp, bp);
    }
}
