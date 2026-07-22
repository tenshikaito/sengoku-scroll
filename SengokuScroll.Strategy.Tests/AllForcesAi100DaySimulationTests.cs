using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Time;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// mini_kanto：全势力 AI 控制 100 日仿真，记录思考/行动日志并校验合理性。
/// </summary>
public class AllForcesAi100DaySimulationTests
{
    private const int SimulationDays = 100;

    [Fact]
    public void MiniKanto_AllForcesAi_Runs100Days_LogsAndBehavesReasonably()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = loaded.Meta.PlayerForceId,
            AllForcesAiControlled = true,
            Difficulty = loaded.Meta.Difficulty,
            StartOptions = loaded.Meta.StartOptions,
            KnownStrongholdIds = loaded.Meta.KnownStrongholdIds,
            LordName = loaded.Meta.LordName,
            LordUnitId = loaded.Meta.LordUnitId,
            LordStrongholdId = loaded.Meta.LordStrongholdId,
            ForceLordCharacterIds = loaded.Meta.ForceLordCharacterIds,
            Intel = loaded.Meta.Intel,
            RegionHarvestProfiles = loaded.Meta.RegionHarvestProfiles
        };

        var debugOpts = new StrategyDayDebugOptions
        {
            Enabled = true,
            WriteToFile = false,
            MaxInMemoryEntries = 50_000
        };
        var aiTraceOpts = new StrategyAiTraceOptions { MaxEntries = int.MaxValue };

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(
            loaded.World,
            meta,
            debugOpts,
            aiTraceOpts);

        StrategyAiBootstrapHelper.BootstrapAggressiveDirectives(ctx.World, meta);

        var debug = ctx.Services.GetRequiredService<IStrategyDayDebugLog>();
        var aiTrace = ctx.Services.GetRequiredService<StrategyAiDecisionTrace>();
        var recorder = new StrategyAiSimulationRecorder();

        var logDir = Path.Combine(AppContext.BaseDirectory, "Log", "ai-simulation");
        var reportPath = Path.Combine(logDir, $"mini_kanto_100d_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        // 记录初始态势
        recorder.RecordDay(
            ctx.World.GameData.GameDate.Year,
            ctx.World.GameData.GameDate.Month,
            ctx.World.GameData.GameDate.Day,
            debug.Snapshot(),
            aiTrace.Snapshot(),
            ctx.World);

        for (var day = 1; day <= SimulationDays; day++)
        {
            var upcoming = ctx.World.GameData.GameDate.AddDays(1);
            debug.BeginDay(upcoming.Year, upcoming.Month, upcoming.Day, "mini_kanto");
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
            debug.EndDay(0, 0);

            recorder.RecordDay(
                upcoming.Year,
                upcoming.Month,
                upcoming.Day,
                debug.Snapshot(),
                aiTrace.Snapshot(),
                ctx.World);
        }

        var analysis = recorder.Analyze();
        recorder.WriteReport(reportPath, analysis);

        _output.WriteLine($"Simulation report: {reportPath}");
        _output.WriteLine($"Days={analysis.TotalDays} AI entries={analysis.AiTraceEntries}");
        _output.WriteLine($"Actions OK={analysis.SuccessfulActions} Hold idle={analysis.HoldIdleCount} Skips={analysis.SkipCount} StandoffSkips={analysis.StandoffSkipCount} Breaks={analysis.StandoffBreakCount}");
        _output.WriteLine($"Stronghold changes={analysis.StrongholdOwnershipChanges}");
        foreach (var (code, count) in analysis.ActionCodeCounts.OrderByDescending(kv => kv.Value))
            _output.WriteLine($"  {code}: {count}");

        // 合理性：全势力 AI 模式下玩家部队不应长期 Hold 待机
        Assert.Equal(0, analysis.PlayerHoldIdleCount);

        // 合理性：至少应有方针调整或成功行动
        Assert.True(
            analysis.DirectiveChanges > 0 || analysis.SuccessfulActions > 0,
            "100 日内应出现 AI 方针变更或成功行动");

        // 合理性：地图上单位应发生移动（至少 2 个不同位置）
        Assert.True(
            analysis.UniquePositionsByUnit.Values.Any(c => c >= 2),
            "至少一支部队应在 100 日内移动过");

        // 合理性：对峙 Skip 不应占 AI 条目的大多数（脱困/清理后应能继续行动）
        Assert.True(
            analysis.StandoffSkipCount < analysis.AiTraceEntries / 2,
            $"对峙 Skip 过多：{analysis.StandoffSkipCount}/{analysis.AiTraceEntries}");

        // 合理性：敌对势力之间应产生战斗或占城（mini_kanto 默认多势力敌对）
        Assert.True(
            analysis.SuccessfulActions >= 3
            || analysis.StrongholdOwnershipChanges >= 1
            || analysis.ActionCodeCounts.ContainsKey("MarchEnemy")
            || analysis.ActionCodeCounts.ContainsKey("EngageAdjacent")
            || analysis.ActionCodeCounts.ContainsKey("SiegeAssault")
            || analysis.ActionCodeCounts.ContainsKey("SiegeEncircle"),
            "100 日仿真应出现行军、接敌或攻城等军事行为");
    }

    private readonly ITestOutputHelper _output;

    public AllForcesAi100DaySimulationTests(ITestOutputHelper output) => _output = output;
}
