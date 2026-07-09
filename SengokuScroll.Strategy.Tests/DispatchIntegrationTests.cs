using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>运输队自动派遣与到达卸粮的集成测试（M1-c）。</summary>
public class SupplyConvoyDispatchIntegrationTests
{
    [Fact]
    public void AdvanceDay_AutoDispatchesConvoy_WhenUnitFoodLow()
    {
        // 据点 (0,0) 有粮，单位 (3,0) 仅 100 合，低于 500 阈值
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var convoy = Assert.Single(ctx.World.GameData.SupplyConvoys.Values);
        Assert.Equal(1, convoy.TargetUnitId);
        Assert.Equal(SupplyConvoyStatus.Moving, convoy.Status);
        Assert.False(string.IsNullOrWhiteSpace(convoy.Name));
        Assert.Contains("粮运", convoy.Name);
        Assert.Equal(0, convoy.Ap);
        // 派遣当日 AP=0，不移动；仍停留在出发格
        Assert.Equal(new Common.Types.Point3(0, 0), convoy.Location);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        // 次日恢复 AP 后推进一格（两日各扣一日在途自耗）
        Assert.Equal(new Common.Types.Point3(1, 0), convoy.Location);
        Assert.Equal(LogisticsConstants.DefaultConvoyCargoGo - 230, convoy.CargoFoodGo);
    }

    [Fact]
    public void AdvanceDay_DeliversFood_AndReturnsConvoyToOrigin()
    {
        // 单位在 (3,0)，运输队需 1 日整备 + 3 日抵达并卸粮，再 1 日整备 + 3 日返程至据点 (0,0)
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var unit = ctx.World.GameData.Units[1];
        var strongholdFoodBefore = ctx.World.GameData.Strongholds[1].ForceActor.Food;

        for (var day = 0; day < 4; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var convoy = Assert.Single(ctx.World.GameData.SupplyConvoys.Values);
        Assert.True(unit.Food > 100);
        Assert.True(convoy.IsReturningToOrigin);
        Assert.Equal(SupplyConvoyStatus.Moving, convoy.Status);
        Assert.Equal(
            strongholdFoodBefore - LogisticsConstants.DefaultConvoyCargoGo,
            ctx.World.GameData.Strongholds[1].ForceActor.Food);

        for (var day = 0; day < 4; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Empty(ctx.World.GameData.SupplyConvoys);
    }
}

/// <summary>信使同格即时 / 异格派遣的集成测试（M1-c）。</summary>
public class MessengerDispatchIntegrationTests
{
    [Fact]
    public void IssuePolicyChange_SameTile_AppliesImmediately()
    {
        // 君主与单位同在 (0,0)，免信使
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld(
            unitLocation: new Common.Types.Point3(0, 0),
            unitFood: 2000);

        var helper = ctx.Services.GetRequiredService<MessengerDispatchHelper>();
        var unit = ctx.World.GameData.Units[1];
        unit.Directive = UnitDirective.Move;

        var outcome = helper.IssuePolicyChange(
            new Common.Types.Point3(0, 0),
            sourceStrongholdId: 1,
            unit,
            UnitDirective.Retreat);

        Assert.Equal(MessengerDispatchOutcome.AppliedImmediately, outcome);
        Assert.Equal(UnitDirective.Retreat, unit.Directive);
        Assert.Empty(ctx.World.GameData.Messengers);
    }

    [Fact]
    public void IssuePolicyChange_RemoteTile_DispatchesMessengerAndDelivers()
    {
        // 单位在 (5,0)，异格需信使；粮充足避免触发运输队派遣
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld(
            unitLocation: new Common.Types.Point3(5, 0),
            unitFood: 2000);

        var helper = ctx.Services.GetRequiredService<MessengerDispatchHelper>();
        var unit = ctx.World.GameData.Units[1];
        unit.Directive = UnitDirective.Move;

        var outcome = helper.IssuePolicyChange(
            new Common.Types.Point3(0, 0),
            sourceStrongholdId: 1,
            unit,
            UnitDirective.Occupy);

        Assert.Equal(MessengerDispatchOutcome.MessengerDispatched, outcome);
        Assert.Equal(UnitDirective.Move, unit.Directive);
        var messenger = Assert.Single(ctx.World.GameData.Messengers.Values);
        Assert.False(string.IsNullOrWhiteSpace(messenger.Name));
        Assert.Contains("信使", messenger.Name);
        Assert.Equal(LogisticsConstants.DefaultMessengerCourierCount, messenger.CourierCount);
        Assert.Equal(LogisticsConstants.DefaultMessengerEscortCount, messenger.EscortSoldierCount);
        Assert.False(messenger.IsMilitary);

        for (var day = 0; day < 5; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(UnitDirective.Occupy, unit.Directive);
        Assert.Empty(ctx.World.GameData.Messengers);
    }
}
