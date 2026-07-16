using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Hosting;

var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
var loaded = StrategyScenarioLoader.LoadFromFile(path);
var opts = Options.Create(new StrategyDayDebugOptions { Enabled = true, WriteToFile = false });
using var scope = StrategySimulationBootstrap.CreateScope(loaded.World, loaded.Meta, opts.Value);
var engine = scope.Engine;
var world = scope.World;
var debug = scope.Services.GetRequiredService<IStrategyDayDebugLog>();
var tc = new SengokuScroll.Strategy.Time.StrategyTimeController();

void DumpUnits(string label) {
    Console.WriteLine($"\n=== {label} date={world.GameData.GameDate.Year}-{world.GameData.GameDate.Month}-{world.GameData.GameDate.Day} ===");
    foreach (var u in world.GameData.Units.Values.OrderBy(u => u.Id)) {
        Console.WriteLine($"  #{u.Id} {u.Name} force={u.ForceId} ({u.Location.X},{u.Location.Y}) soldiers={u.Soldier} status={u.Status} directive={u.Directive} stance={u.Stance} targetUnit={u.ActionTarget.UnitId}");
    }
}

DumpUnits("初始");
for (int day = 1; day <= 6; day++) {
    var upcoming = world.GameData.GameDate.AddDays(1);
    debug.BeginDay(upcoming.Year, upcoming.Month, upcoming.Day, "mini_kanto");
    tc.AdvanceDay(world, engine);
    debug.EndDay(0, 0);
    DumpUnits($"第{day}日后");
    Console.WriteLine("--- debug log ---");
    foreach (var e in debug.Snapshot()) Console.WriteLine(e.Message);
}
