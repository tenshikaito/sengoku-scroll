using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Time;

namespace SengokuScroll.Strategy.Tests;

/// <summary>诊断 mini_kanto 今川先锋 vs 三河凑守军接敌时机。</summary>
public class MikawaEngagementDiagTests
{
    [Fact]
    public void AdvanceDays_LogImagawaVanguardVsMikawaGarrison()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var debugOpts = new StrategyDayDebugOptions { Enabled = true, WriteToFile = false };
        using var scope = StrategySimulationBootstrap.CreateScope(loaded.World, loaded.Meta, debugOpts);
        var debug = scope.Services.GetRequiredService<IStrategyDayDebugLog>();
        var tc = new StrategyTimeController();

        void DumpUnits(string label)
        {
            var date = scope.World.GameData.GameDate;
            _output.WriteLine($"\n=== {label} {date.Year}-{date.Month}-{date.Day} ===");
            foreach (var u in scope.World.GameData.Units.Values.OrderBy(u => u.Id))
            {
                _output.WriteLine(
                    $"  #{u.Id} {u.Name} force={u.ForceId} ({u.Location.X},{u.Location.Y}) " +
                    $"soldiers={u.Soldier} status={u.Status} directive={u.Directive} " +
                    $"stance={u.Stance} bf={u.BattlefieldId} targetUnit={u.ActionTarget.UnitId}");
            }

            var mikawa = scope.World.GameData.Strongholds.Values.First(s => s.Name == "三河凑");
            _output.WriteLine(
                $"  三河凑城内兵={mikawa.ForceActor.Soldier} 据点({mikawa.Location.X},{mikawa.Location.Y})");
        }

        DumpUnits("初始");

        for (var day = 1; day <= 8; day++)
        {
            var upcoming = scope.World.GameData.GameDate.AddDays(1);
            debug.BeginDay(upcoming.Year, upcoming.Month, upcoming.Day, "mini_kanto");
            tc.AdvanceDay(scope.World, scope.Engine);
            debug.EndDay(0, 0);
            DumpUnits($"第{day}日后");

            _output.WriteLine("--- debug log ---");
            foreach (var e in debug.Snapshot())
            {
                if (e.Category is "Move" or "Engage" or "Garrison" or "Battle" or "AI" or "Day")
                    _output.WriteLine($"[{e.Category}] {e.Message}");
            }
        }
    }

    private readonly ITestOutputHelper _output;

    public MikawaEngagementDiagTests(ITestOutputHelper output) => _output = output;
}
