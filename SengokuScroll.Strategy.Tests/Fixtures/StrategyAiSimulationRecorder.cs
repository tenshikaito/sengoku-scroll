using System.Text;
using SengokuScroll.Domain;
using SengokuScroll.Strategy.Diagnostics;

namespace SengokuScroll.Strategy.Tests.Fixtures;

/// <summary>100 天 AI 仿真日志汇总与分析。</summary>
public sealed class StrategyAiSimulationRecorder
{
    private readonly List<DayRecord> days = [];
    private readonly List<StrategyAiDecisionTraceEntry> aiTrace = [];

    public IReadOnlyList<DayRecord> Days => days;

    public void RecordDay(
        int year,
        int month,
        int day,
        IReadOnlyList<StrategyDayDebugEntry> debugEntries,
        IReadOnlyList<StrategyAiDecisionTraceEntry> aiEntries,
        GameWorld world)
    {
        days.Add(new DayRecord(
            year,
            month,
            day,
            [.. debugEntries],
            SnapshotUnits(world)));

        var startSeq = aiTrace.Count > 0 ? aiTrace[^1].Sequence : 0;
        foreach (var entry in aiEntries.Where(e => e.Sequence > startSeq))
            aiTrace.Add(entry);
    }

    public StrategyAiSimulationAnalysis Analyze()
    {
        var directiveChanges = aiTrace.Count(e => e.Phase == "Directive" && e.ActedOrChanged);
        var actions = aiTrace.Where(e => e.Phase == "Action").ToList();
        var successfulActions = actions.Count(e => e.ActedOrChanged);
        var holdIdle = actions.Count(e => e.Code == "Hold" && !e.ActedOrChanged);
        var skips = aiTrace.Count(e => e.Phase == "Skip");
        var standoffSkips = aiTrace.Count(e =>
            e.Phase == "Skip" && e.Message.Contains("对峙", StringComparison.Ordinal));
        var standoffBreaks = aiTrace.Count(e => e.Code is "StandoffBreakRetreat" or "StandoffOpponentLeft" or "StandoffOpponentGone");

        var actionCodes = actions
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Count());

        var playerHoldIdle = actions.Count(e =>
            e.ForceId == 1 && e.Code == "Hold" && !e.ActedOrChanged);

        var uniquePositions = days
            .SelectMany(d => d.Units)
            .GroupBy(u => u.UnitId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(u => u.Location).Distinct().Count());

        var strongholdChanges = CountStrongholdOwnershipChanges();

        return new StrategyAiSimulationAnalysis(
            TotalDays: days.Count,
            AiTraceEntries: aiTrace.Count,
            DirectiveChanges: directiveChanges,
            SuccessfulActions: successfulActions,
            HoldIdleCount: holdIdle,
            PlayerHoldIdleCount: playerHoldIdle,
            SkipCount: skips,
            StandoffSkipCount: standoffSkips,
            StandoffBreakCount: standoffBreaks,
            ActionCodeCounts: actionCodes,
            UniquePositionsByUnit: uniquePositions,
            StrongholdOwnershipChanges: strongholdChanges);
    }

    private int CountStrongholdOwnershipChanges()
    {
        if (days.Count < 2)
            return 0;

        var first = days[0].Units
            .Where(u => u.IsStrongholdSnapshot)
            .ToDictionary(u => u.StrongholdId!.Value, u => u.ForceId);

        var changes = 0;
        foreach (var day in days.Skip(1))
        {
            foreach (var sh in day.Units.Where(u => u.IsStrongholdSnapshot))
            {
                if (first.TryGetValue(sh.StrongholdId!.Value, out var originalForce)
                    && sh.ForceId != originalForce)
                {
                    changes++;
                    first[sh.StrongholdId.Value] = sh.ForceId;
                }
            }
        }

        return changes;
    }

    public void WriteReport(string path, StrategyAiSimulationAnalysis analysis)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var sb = new StringBuilder();
        sb.AppendLine("# Strategy AI 100-Day Simulation Report");
        sb.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Days simulated: {analysis.TotalDays}");
        sb.AppendLine($"- AI trace entries: {analysis.AiTraceEntries}");
        sb.AppendLine($"- Directive changes: {analysis.DirectiveChanges}");
        sb.AppendLine($"- Successful actions: {analysis.SuccessfulActions}");
        sb.AppendLine($"- Hold/idle failures: {analysis.HoldIdleCount} (player force: {analysis.PlayerHoldIdleCount})");
        sb.AppendLine($"- Skipped units: {analysis.SkipCount} (standoff: {analysis.StandoffSkipCount})");
        sb.AppendLine($"- Standoff breaks: {analysis.StandoffBreakCount}");
        sb.AppendLine($"- Stronghold ownership changes: {analysis.StrongholdOwnershipChanges}");
        sb.AppendLine("- Action codes:");
        foreach (var (code, count) in analysis.ActionCodeCounts.OrderByDescending(kv => kv.Value))
            sb.AppendLine($"  - {code}: {count}");
        sb.AppendLine("- Unit position diversity (unique tiles visited):");
        foreach (var (unitId, count) in analysis.UniquePositionsByUnit.OrderBy(kv => kv.Key))
            sb.AppendLine($"  - Unit #{unitId}: {count} tiles");

        sb.AppendLine();
        sb.AppendLine("## Daily Log");
        foreach (var day in days)
        {
            sb.AppendLine();
            sb.AppendLine($"### {day.Year}-{day.Month:D2}-{day.Day:D2}");
            sb.AppendLine("Units:");
            foreach (var u in day.Units.Where(x => !x.IsStrongholdSnapshot).OrderBy(x => x.UnitId))
            {
                sb.AppendLine(
                    $"  #{u.UnitId} {u.Name} force={u.ForceId} ({u.Location}) " +
                    $"soldiers={u.Soldiers} directive={u.Directive} status={u.Status} siege={u.SiegeMode}");
            }

            sb.AppendLine("Strongholds:");
            foreach (var sh in day.Units.Where(x => x.IsStrongholdSnapshot).OrderBy(x => x.StrongholdId))
            {
                sb.AppendLine(
                    $"  #{sh.StrongholdId} {sh.Name} force={sh.ForceId} ({sh.Location}) garrison={sh.GarrisonSoldiers}");
            }

            sb.AppendLine("AI / Debug:");
            foreach (var entry in day.DebugEntries.Where(e => e.Category is "AI" or "Garrison" or "Battle" or "Engage" or "Move"))
                sb.AppendLine($"  [{entry.Category}] {entry.Message}");
        }

        sb.AppendLine();
        sb.AppendLine("## Full AI Trace");
        foreach (var entry in aiTrace)
        {
            sb.AppendLine(
                $"#{entry.Sequence} [{entry.Phase}] force={entry.ForceId} unit={entry.UnitId} {entry.UnitName} " +
                $"code={entry.Code} ok={entry.ActedOrChanged} msg={entry.Message}");
            foreach (var step in entry.Steps)
                sb.AppendLine($"    thought: {step}");
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static IReadOnlyList<UnitSnapshot> SnapshotUnits(GameWorld world)
    {
        var list = new List<UnitSnapshot>();
        foreach (var u in world.GameData.Units.Values.OrderBy(u => u.Id))
        {
            list.Add(new UnitSnapshot(
                u.Id,
                u.Name,
                u.ForceId,
                $"({u.Location.X},{u.Location.Y})",
                u.Soldier,
                u.Directive.ToString(),
                u.Status.ToString(),
                u.SiegeMode.ToString(),
                IsStrongholdSnapshot: false,
                StrongholdId: null,
                GarrisonSoldiers: null));
        }

        foreach (var sh in world.GameData.Strongholds.Values.OrderBy(s => s.Id))
        {
            list.Add(new UnitSnapshot(
                sh.Id,
                sh.Name,
                sh.ForceId,
                $"({sh.Location.X},{sh.Location.Y})",
                0,
                "-",
                "-",
                "-",
                IsStrongholdSnapshot: true,
                StrongholdId: sh.Id,
                GarrisonSoldiers: sh.ForceActor.Soldier));
        }

        return list;
    }

    public sealed record DayRecord(
        int Year,
        int Month,
        int Day,
        IReadOnlyList<StrategyDayDebugEntry> DebugEntries,
        IReadOnlyList<UnitSnapshot> Units);

    public sealed record UnitSnapshot(
        int UnitId,
        string Name,
        int ForceId,
        string Location,
        int Soldiers,
        string Directive,
        string Status,
        string SiegeMode,
        bool IsStrongholdSnapshot,
        int? StrongholdId,
        int? GarrisonSoldiers);
}

public sealed record StrategyAiSimulationAnalysis(
    int TotalDays,
    int AiTraceEntries,
    int DirectiveChanges,
    int SuccessfulActions,
    int HoldIdleCount,
    int PlayerHoldIdleCount,
    int SkipCount,
    int StandoffSkipCount,
    int StandoffBreakCount,
    IReadOnlyDictionary<string, int> ActionCodeCounts,
    IReadOnlyDictionary<int, int> UniquePositionsByUnit,
    int StrongholdOwnershipChanges);
