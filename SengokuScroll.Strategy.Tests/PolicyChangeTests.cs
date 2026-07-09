using SengokuScroll.Strategy.Hosting;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>方针变更 Host 联调（M3-b）。</summary>
public class PolicyChangeHostTests
{
    [Fact]
    public void LoadScenario_ExposesLordState()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var lord = host.GetState().Value!.Lord;
        Assert.Equal("织田信长", lord.Name);
        Assert.Equal(1, lord.X);
        Assert.Equal(4, lord.Y);
    }

    [Fact]
    public void OrderUnitDirective_WhenLordRemote_DispatchesMessengerFromLord()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var state = host.GetState().Value!;
        var lord = state.Lord;
        var unit = state.Units.First(u => u.Id == 1);

        Assert.NotEqual(lord.X, unit.X);

        var result = host.OrderUnitDirective(1, UnitDirective.Occupy);

        Assert.True(result.IsSuccess);
        Assert.Equal("MessengerDispatched", result.Value!.Outcome);
        Assert.Equal("Move", result.Value.State.Units.First(u => u.Id == 1).Directive);
        Assert.Single(result.Value.State.Messengers);
        Assert.Equal("Occupy", result.Value.State.Messengers[0].PendingDirective);
        Assert.Equal(lord.X, result.Value.State.Messengers[0].X);
        Assert.Equal(lord.Y, result.Value.State.Messengers[0].Y);
    }
}
