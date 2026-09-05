using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public sealed class StrategyParallelWorkTests
{
    [Fact]
    public void MapOrdered_PreservesInputOrderAndMapsEveryItem()
    {
        var source = Enumerable.Range(0, 10_000).Reverse().ToArray();

        var mapped = StrategyParallelWork.MapOrdered(
            source,
            value => value * 3,
            minimumParallelCount: 1);

        Assert.Equal(source.Select(value => value * 3), mapped);
    }

    [Fact]
    public void SingleBatch_StaysOnCallerThreadInsteadOfFanningOut()
    {
        var callerThread = Environment.CurrentManagedThreadId;
        var input = Enumerable.Range(0, 63).ToArray();
        var output = StrategyParallelWork.MapOrdered(input, value =>
        {
            Assert.Equal(callerThread, Environment.CurrentManagedThreadId);
            return value;
        }, minimumParallelCount: 32);
        Assert.Equal(input, output);
        StrategyParallelWork.ForEachIndex(63,
            _ => Assert.Equal(callerThread, Environment.CurrentManagedThreadId), 32);
    }

    [Fact]
    public void SameSeed_TwoLongRuns_ProduceIdenticalWorldSave()
    {
        var first = RunAndCapture(days: 365);
        var second = RunAndCapture(days: 365);

        Assert.Equal(
            JsonSerializer.Serialize(first),
            JsonSerializer.Serialize(second));
    }

    [Fact]
    public async Task ConcurrentWorlds_MatchSequentialSaveIncludingRuntimeServices()
    {
        static string Run()
        {
            using var host = new SengokuScroll.Strategy.Hosting.StrategySimulationHost();
            Assert.True(host.LoadScenario("mini_kanto", new SengokuScroll.Strategy.Models.StrategyLoadOptions
            { AllForcesAiControlled = true }).IsSuccess);
            Assert.True(host.AdvanceDays(31).IsSuccess);
            return SengokuScroll.Strategy.Hosting.StrategySimulationHost.SerializeSave(host.CaptureSave().Value!);
        }

        var expected = Run();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return Run();
        }, TestContext.Current.CancellationToken)).ToArray();
        start.SetResult();
        foreach (var actual in await Task.WhenAll(runs)) Assert.Equal(expected, actual);
    }

    private static StrategySaveDocument RunAndCapture(int days)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = CreateAllAiMeta(loaded.Meta);
        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);
        StrategyAiBootstrapHelper.BootstrapAggressiveDirectives(context.World, meta);

        for (var day = 0; day < days; day++)
            context.TimeController.AdvanceDay(context.World, context.Engine);

        return StrategyWorldSaveService.Capture(
            context.World,
            "mini_kanto",
            meta.PlayerForceId,
            context.Services.GetRequiredService<StrategyVisibilityLedger>(),
            meta);
    }

    private static StrategyScenarioMeta CreateAllAiMeta(StrategyScenarioMeta source)
        => new()
        {
            PlayerForceId = source.PlayerForceId,
            AllForcesAiControlled = true,
            Difficulty = source.Difficulty,
            StartOptions = source.StartOptions,
            KnownStrongholdIds = source.KnownStrongholdIds,
            LordName = source.LordName,
            LordUnitId = source.LordUnitId,
            LordStrongholdId = source.LordStrongholdId,
            ForceLordCharacterIds = source.ForceLordCharacterIds,
            Intel = source.Intel,
            RegionHarvestProfiles = source.RegionHarvestProfiles
        };
}
