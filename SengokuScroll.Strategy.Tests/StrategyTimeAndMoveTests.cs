using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Time;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>策略模式时间控制器（<see cref="StrategyTimeController"/>）的集成测试。</summary>
public class StrategyTimeControllerTests
{
    [Fact]
    public void AdvanceDay_IncrementsGameDate()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var before = ctx.World.GameData.GameDate;

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(before.AddDays(1), ctx.World.GameData.GameDate);
    }

    [Fact]
    public void PauseAndResume_UpdateState()
    {
        var controller = new StrategyTimeController();

        controller.Resume();
        Assert.Equal(StrategyTimeState.Running, controller.State);

        controller.Pause();
        Assert.Equal(StrategyTimeState.Paused, controller.State);
    }
}

/// <summary>策略模式军事单位移动与日推进的集成测试。</summary>
public class StrategyUnitMoveTests
{
    [Fact]
    public void AdvanceDay_MovesUnitAlongRoute()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var unit = ctx.World.GameData.Units[1];

        // 设定移动目标：向东一格；进入移动状态
        unit.Status = UnitStatus.Moving;
        unit.ActionTarget.RoutePoints.Enqueue(new Common.Types.Point2(1, 0));
        unit.Ap = 10;
        unit.IsReadyToMove = true;

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(new Common.Types.Point3(1, 0), unit.Location);
    }

    [Fact]
    public void UnitMoveEvaluator_RejectsOutOfMapDestination()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var gameContext = ctx.Services.GetRequiredService<Domain.Contexts.IGameContext>();
        var evaluator = ctx.Services.GetRequiredService<UnitMoveEvaluator>();
        var unit = ctx.World.GameData.Units[1];

        // 地图 10×10，(10,0) 超出边界
        var result = evaluator.Evaluate(unit, new Common.Types.Point2(10, 0));

        Assert.False(result.IsSuccess);
    }
}
