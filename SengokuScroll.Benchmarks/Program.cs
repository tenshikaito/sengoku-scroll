using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;

var roomCount = 4;
var days = 60;
int? degree = null;
for (var i = 0; i < args.Length; i += 2)
{
    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out var value))
        throw new ArgumentException("Usage: --rooms 1..16 --days 1..3650 --degree 1..1024");
    switch (args[i])
    {
        case "--rooms" when value is >= 1 and <= 16: roomCount = value; break;
        case "--days" when value is >= 1 and <= 3650: days = value; break;
        case "--degree" when value is >= 1 and <= 1024: degree = value; break;
        default: throw new ArgumentException("Invalid benchmark argument: " + args[i]);
    }
}
// Set before the scheduler is first used; this changes only this benchmark process.
if (degree.HasValue)
    Environment.SetEnvironmentVariable("SENGOKU_MAX_PARALLELISM", degree.Value.ToString());

static StrategySimulationHost CreateWorld()
{
    var host = new StrategySimulationHost();
    if (host.LoadScenario("mini_kanto", new StrategyLoadOptions { AllForcesAiControlled = true }).IsSuccess)
        return host;
    host.Dispose();
    throw new InvalidOperationException("Scenario failed to load");
}

using (var warmup = CreateWorld())
    if (!warmup.AdvanceDays(5).IsSuccess) throw new InvalidOperationException("Warmup failed");

var worlds = new List<StrategySimulationHost>();
try
{
    for (var i = 0; i < roomCount; i++) worlds.Add(CreateWorld());
    using var barrier = new Barrier(roomCount + 1);
    var runs = worlds.Select(host => Task.Factory.StartNew(() =>
    {
        var samples = new double[days];
        barrier.SignalAndWait();
        for (var day = 0; day < days; day++)
        {
            var start = Stopwatch.GetTimestamp();
            var result = host.AdvanceDay();
            samples[day] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (!result.IsSuccess) throw new InvalidOperationException("Advance failed");
        }
        return samples;
    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

    using var process = Process.GetCurrentProcess();
    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
    var cpuBefore = process.TotalProcessorTime;
    var timer = Stopwatch.StartNew();
    barrier.SignalAndWait();
    var samples = (await Task.WhenAll(runs)).SelectMany(values => values).Order().ToArray();
    timer.Stop();
    var cpuSeconds = (process.TotalProcessorTime - cpuBefore).TotalSeconds;
    var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

    // Exclude save serialization from timings; verify equal authoritative outcomes.
    var hashes = worlds.Select(host =>
    {
        var captured = host.CaptureSave();
        if (!captured.IsSuccess) throw new InvalidOperationException("Capture failed");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            StrategySimulationHost.SerializeSave(captured.Value))));
    }).ToArray();
    var sameOutcome = hashes.Distinct(StringComparer.Ordinal).Count() == 1;
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        scenario = "mini_kanto",
        roomCount,
        days,
        logicalProcessors = Environment.ProcessorCount,
        parallelDegree = StrategyParallelWork.MaxDegreeOfParallelism,
        wallSeconds = timer.Elapsed.TotalSeconds,
        cpuSeconds,
        daysPerSecond = samples.Length / timer.Elapsed.TotalSeconds,
        p50DayMs = samples[(int)Math.Ceiling(samples.Length * 0.50) - 1],
        p95DayMs = samples[(int)Math.Ceiling(samples.Length * 0.95) - 1],
        allocatedBytes,
        sameOutcome,
        hashes
    }, new JsonSerializerOptions { WriteIndented = true }));
    if (!sameOutcome) Environment.ExitCode = 1;
}
finally
{
    foreach (var world in worlds) world.Dispose();
}
