using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>经济月结（M3-d）单元测试。</summary>
public class EconomyMonthlySettlementTests
{
    [Fact]
    public void EconomyCalculator_StrongholdTax_ScalesWithPopulationAndRates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var stronghold = loaded.World.GameData.Strongholds.Values.First();
        stronghold.Population = 8000;
        stronghold.PollTaxRate = 10;
        stronghold.AgricultureTaxRate = 25;
        stronghold.CommerceTaxRate = 12;
        stronghold.TariffTaxRate = 8;

        Assert.Equal(1440, EconomyCalculator.CalculateStrongholdMonthlyTaxMoney(stronghold));
        Assert.Equal(2000, EconomyCalculator.CalculateStrongholdMonthlyTaxFood(stronghold));
    }

    [Fact]
    public void TributeRouting_InnerVassalCapital_SendsToSuzerainCapital()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var gameData = loaded.World.GameData;

        var inuyama = gameData.Strongholds[2];
        var destination = TributeRoutingHelper.ResolveTributeDestinationStrongholdId(
            inuyama,
            gameData,
            loaded.Meta);

        Assert.Equal(1, destination);
    }

    [Fact]
    public void TributeRouting_InnerVassalTerritory_SendsToInnerCapital()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var gameData = loaded.World.GameData;

        var numazu = gameData.Strongholds[10];
        var destination = TributeRoutingHelper.ResolveTributeDestinationStrongholdId(
            numazu,
            gameData,
            loaded.Meta);

        Assert.Equal(5, destination);
    }

    [Fact]
    public void AdvanceDay_OnFirstDayOfMonth_EmitsMonthlySettlementEvent()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        ctx.Services.GetRequiredService<SupplyConvoyDispatchHelper>().DispatchMonthlyLordTributes();

        while (ctx.World.GameData.GameDate is var d && !(d.Month == 2 && d.Day == 1))
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        var monthly = buffer.Events.First(e => e.Category == "EconomyMonthly");
        Assert.NotNull(monthly.EconomySettlement);
        Assert.Equal("Monthly", monthly.EconomySettlement!.Period);
    }

    [Fact]
    public void AdvanceDay_OnFirstDayOfYear_EmitsAnnualSettlementEvent()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        var startYear = loaded.World.GameData.GameDate.Year;

        while (ctx.World.GameData.GameDate is var d
               && !(d.Year == startYear + 1 && d.Month == 1 && d.Day == 1))
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        var annual = buffer.Events.FirstOrDefault(e => e.Category == "EconomyAnnual");
        Assert.NotNull(annual);
        Assert.NotNull(annual!.EconomySettlement);
        Assert.Equal("Annual", annual.EconomySettlement!.Period);
    }

    [Fact]
    public void IsMonthlySettlementDay_IsFirstDayOfMonth()
    {
        var date = new Domain.Types.GameDate(1560, 2, 1);
        Assert.True(EconomyRules.IsMonthlySettlementDay(date));

        var notSettlement = new Domain.Types.GameDate(1560, 2, 2);
        Assert.False(EconomyRules.IsMonthlySettlementDay(notSettlement));
    }
}
