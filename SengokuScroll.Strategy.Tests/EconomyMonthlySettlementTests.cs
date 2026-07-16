using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
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

        Assert.Equal(22_400, EconomyCalculator.CalculateStrongholdMonthlyTaxMoney(stronghold));
        Assert.Equal(50_000, EconomyCalculator.CalculateStrongholdMonthlyTaxFood(stronghold));
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
    public void TributeLedger_AggregatesMultipleArrivalsFromSameOrigin()
    {
        var ledger = new StrategyTributeLedger();
        ledger.RecordArrival(1560, 1, "清洲", 100, 50);
        ledger.RecordArrival(1560, 1, "清洲", 200, 30);

        var summary = ledger.ConsumeMonthlySettlement(1560, 1);

        Assert.Equal(2, summary.ConvoyCount);
        Assert.Single(summary.Lines);
        Assert.Equal(300, summary.TotalFood);
        Assert.Equal(80, summary.TotalMoney);
        Assert.Equal("清洲", summary.Lines[0].OriginName);
    }

    [Fact]
    public void MapConvoy_ExposesCargoMoney()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var gameData = loaded.World.GameData;
        gameData.SupplyConvoys.Clear();
        gameData.SupplyConvoys[99] = new Domain.Entities.SupplyConvoy
        {
            Id = 99,
            Name = "测试贡纳",
            ForceId = 1,
            Location = new SengokuScroll.Common.Types.Point3(1, 4, 0),
            OriginStrongholdId = 1,
            TargetStrongholdId = 1,
            CargoFoodGo = 500,
            CargoMoney = 1200,
            PorterCount = 10,
            EscortSoldierCount = 5,
            Status = Domain.Entities.Types.SupplyConvoyStatus.Moving
        };

        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta);
        var mapped = dto.SupplyConvoys.Single(c => c.Id == 99);

        Assert.Equal(1200, mapped.Money);
        Assert.Equal(500, mapped.Food);
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
