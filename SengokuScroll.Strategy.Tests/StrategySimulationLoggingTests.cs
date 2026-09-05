using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Extensions;
using SengokuScroll.Strategy.Hosting;

namespace SengokuScroll.Strategy.Tests;

public sealed class StrategySimulationLoggingTests
{
    [Fact]
    public void ConfiguredDebugOptions_AreNotReplacedByDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<StrategyDayDebugOptions>(options =>
        {
            options.Enabled = true;
            options.WriteToFile = false;
            options.MaxInMemoryEntries = 7;
        });
        services.AddStrategySimulationHost();
        using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<StrategySimulationHost>();
        Assert.Equal(7, provider.GetRequiredService<IOptions<StrategyDayDebugOptions>>().Value.MaxInMemoryEntries);
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
        var advanced = host.AdvanceDay();
        Assert.True(advanced.IsSuccess);
        Assert.InRange(advanced.Value.DayDebugEntryCount, 1, 7);
        Assert.Null(advanced.Value.DayDebugLogPath);
    }

    [Fact]
    public void WorldDoesNotDisposeSharedApplicationLoggerFactory()
    {
        using var loggerFactory = new TrackingFactory();
        using (var host = new StrategySimulationHost(loggerFactory))
        {
            Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
            Assert.True(host.AdvanceDay().IsSuccess);
        }
        Assert.True(loggerFactory.CreatedCount > 0);
        Assert.False(loggerFactory.Disposed);
    }

    [Fact]
    public void IndependentWorlds_WriteDebugLogsToSeparateSessionFiles()
    {
        var directory = Directory.CreateTempSubdirectory("sengoku-log-test-");
        try
        {
            var options = Options.Create(new StrategyDayDebugOptions
            { Enabled = true, WriteToFile = true, OutputDirectory = directory.FullName });
            using var first = new StrategySimulationHost(dayDebugOptions: options);
            using var second = new StrategySimulationHost(dayDebugOptions: options);
            Assert.True(first.LoadScenario("mini_kanto").IsSuccess);
            Assert.True(second.LoadScenario("mini_kanto").IsSuccess);
            var firstPath = first.AdvanceDay().Value.DayDebugLogPath;
            var secondPath = second.AdvanceDay().Value.DayDebugLogPath;
            Assert.NotNull(firstPath);
            Assert.NotNull(secondPath);
            Assert.NotEqual(firstPath, secondPath);
            Assert.True(File.Exists(firstPath));
            Assert.True(File.Exists(secondPath));
        }
        finally { directory.Delete(recursive: true); }
    }

    private sealed class TrackingFactory : ILoggerFactory
    {
        public bool Disposed { get; private set; }
        public int CreatedCount { get; private set; }
        public ILogger CreateLogger(string categoryName)
        {
            CreatedCount++;
            return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() => Disposed = true;
    }
}
