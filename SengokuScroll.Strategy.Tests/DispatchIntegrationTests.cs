using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>运输队自动派遣与到达卸粮的集成测试（M1-c）。</summary>
public class SupplyConvoyDispatchIntegrationTests
{
    [Fact]
    public void AdvanceDay_AutoDispatchesConvoy_WhenUnitFoodLow()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var transport = Assert.Single(
            ctx.World.GameData.Units.Values,
            TransportUnitRules.IsTransportUnit);
        Assert.Equal(1, transport.TransportTargetUnitId);
        Assert.Equal(UnitStatus.Moving, transport.Status);
        Assert.False(string.IsNullOrWhiteSpace(transport.Name));
        Assert.Contains("粮运", transport.Name);
        Assert.Equal(0, transport.Ap);
        Assert.Equal(new Common.Types.Point3(0, 0), transport.Location);
        var movementTrace = ctx.Services.GetRequiredService<StrategyMovementTrace>();
        Assert.DoesNotContain(
            movementTrace.Snapshot(),
            entry => entry.UnitId == transport.Id && entry.Phase == "MoveEval");

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        Assert.Equal(new Common.Types.Point3(1, 0), transport.Location);
        Assert.Equal(LogisticsConstants.DefaultConvoyCargoGo - 270, transport.Food);
    }

    [Fact]
    public void AdvanceDay_DeliversFood_AndReturnsConvoyToOrigin()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var unit = ctx.World.GameData.Units[1];
        var strongholdFoodBefore = ctx.World.GameData.Strongholds[1].ForceActor.Food;

        for (var day = 0; day < 4; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var transport = Assert.Single(
            ctx.World.GameData.Units.Values,
            TransportUnitRules.IsTransportUnit);
        Assert.True(unit.Food > 100);
        Assert.True(transport.IsReturningToOrigin);
        Assert.Equal(UnitStatus.Moving, transport.Status);
        Assert.Equal(
            strongholdFoodBefore - LogisticsConstants.DefaultConvoyCargoGo,
            ctx.World.GameData.Strongholds[1].ForceActor.Food);

        for (var day = 0; day < 4; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.DoesNotContain(
            ctx.World.GameData.Units.Values,
            TransportUnitRules.IsTransportUnit);
    }
}

/// <summary>信使同格即时 / 异格派遣的集成测试（M1-c）。</summary>
public class MessengerDispatchIntegrationTests
{
    [Fact]
    public void IssuePolicyChange_SameTile_AppliesImmediately()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld(
            unitLocation: new Common.Types.Point3(0, 0),
            unitFood: 2000);

        var helper = ctx.Services.GetRequiredService<MessageCarrierDispatchHelper>();
        var unit = ctx.World.GameData.Units[1];
        unit.Directive = UnitDirective.Move;

        var outcome = helper.IssuePolicyChange(
            new Common.Types.Point3(0, 0),
            sourceStrongholdId: 1,
            unit,
            UnitDirective.Retreat);

        Assert.Equal(MessageCarrierDispatchOutcome.AppliedImmediately, outcome);
        Assert.Equal(UnitDirective.Retreat, unit.Directive);
        Assert.Empty(ctx.World.GameData.MessageCarriers);
    }

    [Fact]
    public void IssuePolicyChange_RemoteTile_DispatchesMessengerAndDelivers()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld(
            unitLocation: new Common.Types.Point3(5, 0),
            unitFood: 2000);

        var helper = ctx.Services.GetRequiredService<MessageCarrierDispatchHelper>();
        var unit = ctx.World.GameData.Units[1];
        unit.Directive = UnitDirective.Move;

        var outcome = helper.IssuePolicyChange(
            new Common.Types.Point3(0, 0),
            sourceStrongholdId: 1,
            unit,
            UnitDirective.Occupy);

        Assert.Equal(MessageCarrierDispatchOutcome.CarrierDispatched, outcome);
        Assert.Equal(UnitDirective.Move, unit.Directive);
        var MessageCarrier = Assert.Single(ctx.World.GameData.MessageCarriers.Values);
        Assert.False(string.IsNullOrWhiteSpace(MessageCarrier.Name));
        Assert.Contains("文书", MessageCarrier.Name);
        Assert.Equal(LogisticsConstants.DefaultMessengerCourierCount, MessageCarrier.CourierCount);
        Assert.Equal(LogisticsConstants.DefaultMessengerEscortCount, MessageCarrier.EscortSoldierCount);
        Assert.False(MessageCarrier.IsMilitary);

        for (var day = 0; day < 5; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(UnitDirective.Occupy, unit.Directive);
        Assert.Empty(ctx.World.GameData.MessageCarriers);
    }
}
