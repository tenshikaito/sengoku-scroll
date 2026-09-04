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

        // 合理性：三支初始野战军都必须真正参与战略移动，不能靠移民/商队通过验收。
        foreach (var unitId in new[] { 1, 2, 21 })
        {
            Assert.True(
                analysis.UniquePositionsByUnit.TryGetValue(unitId, out var positions) && positions >= 2,
                $"初始野战军 #{unitId} 在 100 日内应发生移动");
        }

        // 合理性：过滤非军事噪音后，对峙/攻城锁定允许占较大比例，但不能吞没绝大多数决策。
        Assert.True(
            analysis.StandoffSkipCount * 4 < analysis.AiTraceEntries * 3,
            $"对峙 Skip 过多：{analysis.StandoffSkipCount}/{analysis.AiTraceEntries}");

        // 合理性：不能用三次偶发行动糊弄验收；百日内应有持续行动并实际改变版图。
        Assert.True(
            analysis.SuccessfulActions >= 20,
            $"100 日有效军事行动过少：{analysis.SuccessfulActions}");
        Assert.True(
            analysis.StrongholdOwnershipChanges >= 1,
            "100 日仿真应至少发生一次据点易主");
        Assert.Contains("ContinueRoute", analysis.ActionCodeCounts.Keys);
        Assert.Contains("MarchAttack", analysis.ActionCodeCounts.Keys);
        Assert.Contains("SiegeAssault", analysis.ActionCodeCounts.Keys);
    }

    private readonly ITestOutputHelper _output;

    public AllForcesAi100DaySimulationTests(ITestOutputHelper output) => _output = output;
}
