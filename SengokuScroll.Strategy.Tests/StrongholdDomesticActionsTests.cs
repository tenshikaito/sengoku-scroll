using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdDomesticActionsTests
{
    [Fact]
    public void SetTaxRates_DirectRuleRemoteStronghold_DispatchesMessenger()
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

        var result = host.SetStrongholdTaxRates(
            remote.Id,
            pollTaxRate: (byte)Math.Min(100, remote.PollTaxRate + 5),
            agricultureTaxRate: null,
            commerceTaxRate: null,
            tariffTaxRate: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("CarrierDispatched", result.Value!.Outcome);
        Assert.Single(result.Value.State.MessageCarriers);
    }

    [Fact]
    public void SetTaxRates_AppointedLordTerritory_ReturnsError()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.GetFullPath(path));
        var stronghold = loaded.World.GameData.Strongholds.Values
            .First(s => s.ForceId == loaded.Meta.PlayerForceId);
        stronghold.LordId = 99;

        Assert.False(StrongholdDomesticRules.CanPlayerAdjustTaxRates(
            stronghold, loaded.Meta, loaded.World.GameData));
    }

    [Fact]
    public void AdministrationEfficiency_AppointedLordTerritory_IsFull()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.GetFullPath(path));
        var appointed = loaded.World.GameData.Strongholds.Values
            .First(s => s.ForceId == loaded.Meta.PlayerForceId);
        appointed.LordId = 99;

        var efficiency = AdministrationCalculator.CalculateAdministrativeEfficiencyPercent(
            appointed,
            loaded.World.GameData,
            loaded.Meta);

        Assert.Equal(100, efficiency);
    }

    [Fact]
    public void IssueTaxRateChange_DirectRuleRemote_DispatchesMessenger()
    {
        var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var gameData = ctx.World.GameData;
        var residence = gameData.Strongholds[1];
        var remote = StrategyTestWorldBuilder.CreateTestStronghold(2, 1, new Point3(5, 0));
        remote.LordId = 0;
        gameData.Strongholds[2] = remote;
        Domain.Actions.MapLocationActions.RegisterStronghold(ctx.World, remote);

        var helper = ctx.Services.GetRequiredService<MessageCarrierDispatchHelper>();
        var change = new PendingStrongholdTaxChange { PollTaxRate = 20 };

        var outcome = helper.IssueTaxRateChange(
            residence.Location,
            residence.Id,
            remote,
            change);

        Assert.Equal(MessageCarrierDispatchOutcome.CarrierDispatched, outcome);
        var MessageCarrier = Assert.Single(gameData.MessageCarriers.Values);
        Assert.Equal(Domain.Entities.Types.MessagePayloadType.TaxRateChange, MessageCarrier.Payload.Type);
        Assert.Equal(remote.Id, MessageCarrier.Payload.TargetStrongholdId);
    }
}
