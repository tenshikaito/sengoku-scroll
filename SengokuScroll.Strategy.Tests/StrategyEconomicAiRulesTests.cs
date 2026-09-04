using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

public sealed class StrategyEconomicAiRulesTests
{
    private static readonly string MiniKantoPath = Path.Combine(
        AppContext.BaseDirectory,
        "Maps",
        "mini_kanto.json");

    [Fact]
    public void Evaluate_WhenFoodOrPopularFeelingsAreCritical_LowersTaxBurden()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var stronghold = loaded.World.GameData.Strongholds[1];
        var force = loaded.World.GameData.Forces[stronghold.ForceId];
        stronghold.CivilianActor.PopularFeelings = 30;
        stronghold.CivilianActor.Food = 0;
        var oldBurden = StrongholdDomesticActions.CalculateTaxBurdenScore(stronghold);

        var decision = StrategyEconomicAiRules.Evaluate(stronghold, force, loaded.World.GameData);

        Assert.NotNull(decision);
        Assert.Equal("Relief", decision.Policy);
        StrongholdDomesticActions.ApplyTaxRateChange(stronghold, decision.Change);
        Assert.True(StrongholdDomesticActions.CalculateTaxBurdenScore(stronghold) < oldBurden);
        Assert.True(stronghold.CivilianActor.PopularFeelings > 30);
    }

    [Fact]
    public void Evaluate_WhenTreasuryIsLowAndRealmStable_RaisesTaxGradually()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var stronghold = loaded.World.GameData.Strongholds[1];
        var force = loaded.World.GameData.Forces[stronghold.ForceId];
        stronghold.CivilianActor.PopularFeelings = 80;
        stronghold.CivilianActor.Food = 10_000_000;
        stronghold.PollTaxRate = 10;
        stronghold.AgricultureTaxRate = 20;
        stronghold.CommerceTaxRate = 10;
        stronghold.TariffTaxRate = 5;
        force.Money = 0;

        var decision = StrategyEconomicAiRules.Evaluate(stronghold, force, loaded.World.GameData);

        Assert.NotNull(decision);
        Assert.Equal("Revenue", decision.Policy);
        Assert.Equal((byte?)11, decision.Change.PollTaxRate);
        Assert.Equal((byte?)21, decision.Change.AgricultureTaxRate);
        Assert.Equal((byte?)11, decision.Change.CommerceTaxRate);
        Assert.Equal((byte?)6, decision.Change.TariffTaxRate);
    }
}
