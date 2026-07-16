using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>经济规则与单位日耗粮（<see cref="UnitEconomyActions"/>）的单元测试。</summary>
public class EconomyRulesAndActionsTests
{
    [Fact]
    public void ShouldConsumeDailyFood_WhenMilitaryWithSoldiers_ReturnsTrue()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        unit.IsMilitary = true;
        unit.Soldier = 50;

        Assert.True(EconomyRules.ShouldConsumeDailyFood(unit));
    }

    [Fact]
    public void ShouldConsumeDailyFood_WhenNoSoldiers_ReturnsFalse()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        unit.IsMilitary = true;
        unit.Soldier = 0;

        Assert.False(EconomyRules.ShouldConsumeDailyFood(unit));
    }

    [Fact]
    public void ApplyDailyFoodConsumption_DeductsFromUnitFood()
    {
        // 100 兵 × 3 合/日 = 300 合
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0), food: 1000);
        unit.IsMilitary = true;
        unit.Soldier = 100;

        var deducted = UnitEconomyActions.ApplyDailyFoodConsumption(unit);

        Assert.Equal(300, deducted);
        Assert.Equal(700, unit.Food);
    }

    [Fact]
    public void ApplyDailyFoodConsumption_DoesNotGoBelowZero()
    {
        // 粮不足时扣至 0
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0), food: 50);
        unit.IsMilitary = true;
        unit.Soldier = 100;

        var deducted = UnitEconomyActions.ApplyDailyFoodConsumption(unit);

        Assert.Equal(50, deducted);
        Assert.Equal(0, unit.Food);
    }
}

/// <summary>策略经济系统日推进集成测试（M1-d）。</summary>
public class StrategyEconomyIntegrationTests
{
    [Fact]
    public void AdvanceDay_DeductsFoodForMilitaryUnits()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var unit = ctx.World.GameData.Units[1];
        unit.IsMilitary = true;
        unit.Soldier = 100;
        unit.Food = 1000;

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var expected = 1000 - LogisticsCalculator.CalculateUnitDailyFoodConsumption(100);
        Assert.Equal(expected, unit.Food);
    }

    [Fact]
    public void ApplyDailyGarrisonFoodConsumption_800Soldiers_DeductsAboutTwoPointFourKoku()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, new Common.Types.Point3(1, 1));
        stronghold.ForceActor.Soldier = 800;
        stronghold.ForceActor.Food = 10_000;

        var deducted = StrongholdEconomyActions.ApplyDailyGarrisonFoodConsumption(stronghold);

        // 800 人 × 3 合/日 = 2400 合 ≈ 2.4 石
        Assert.Equal(2400, deducted);
        Assert.Equal(7600, stronghold.ForceActor.Food);
    }
}
