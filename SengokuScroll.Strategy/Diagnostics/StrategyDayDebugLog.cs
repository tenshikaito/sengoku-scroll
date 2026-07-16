using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SengokuScroll.Localization;
using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 日推进 debug 日志：按日收集 AI/移动/接敌/战斗等细节，并追加写入文件。
/// </summary>
public sealed class StrategyDayDebugLog : IStrategyDayDebugLog
{
    private readonly StrategyDayDebugOptions options;
    private readonly ITextLocalizer localizer;
    private readonly ILogger<StrategyDayDebugLog> logger;
    private readonly ConcurrentQueue<StrategyDayDebugEntry> entries = new();
    private readonly object fileLock = new();

    private int sequence;
    private int? gameYear;
    private int? gameMonth;
    private int? gameDay;
    private string? scenarioId;
    private string? lastWrittenFilePath;

    public StrategyDayDebugLog(
        IOptions<StrategyDayDebugOptions> options,
        ITextLocalizer localizer,
        ILogger<StrategyDayDebugLog> logger)
    {
        this.options = options.Value;
        this.localizer = localizer;
        this.logger = logger;
    }

    public bool IsEnabled => options.Enabled;

    public string? LastWrittenFilePath => lastWrittenFilePath;

    public void BeginDay(int year, int month, int day, string? scenarioId = null)
    {
        if (!options.Enabled)
            return;

        entries.Clear();
        sequence = 0;
        gameYear = year;
        gameMonth = month;
        gameDay = day;
        this.scenarioId = scenarioId;

        var header = localizer.Format(
            LocalizationKeys.Debug.DayBegin,
            year, month, day);

        Append("Day", header);
        if (!string.IsNullOrWhiteSpace(scenarioId))
            Append("Day", $"scenario={scenarioId}");
    }

    public void LogSystemStart(string systemName, int order)
    {
        if (!options.Enabled)
            return;

        Append("System", localizer.Format(LocalizationKeys.Debug.SystemStart, order, systemName));
    }

    public void LogSystemEnd(string systemName, int order)
    {
        if (!options.Enabled)
            return;

        Append("System", localizer.Format(LocalizationKeys.Debug.SystemEnd, order, systemName));
    }

    public void LogLine(string category, string message)
    {
        if (!options.Enabled)
            return;

        Append(category, message);
    }

    public void LogLocalized(string category, string localizationKey, params object?[] args)
    {
        if (!options.Enabled)
            return;

        Append(category, localizer.Format(localizationKey, args));
    }

    public void EndDay(int battleCount, int eventCount)
    {
        if (!options.Enabled)
            return;

        var footer = localizer.Format(
            LocalizationKeys.Debug.DayEnd,
            gameYear ?? 0,
            gameMonth ?? 0,
            gameDay ?? 0,
            battleCount,
            eventCount);

        Append("Day", footer);

        if (options.WriteToFile)
            FlushToFile();
    }

    public IReadOnlyList<StrategyDayDebugEntry> Snapshot()
        => [.. entries];

    public void Clear()
    {
        entries.Clear();
        sequence = 0;
        lastWrittenFilePath = null;
    }

    private void Append(string category, string message)
    {
        var entry = new StrategyDayDebugEntry(
            Interlocked.Increment(ref sequence),
            DateTimeOffset.Now,
            gameYear,
            gameMonth,
            gameDay,
            category,
            message);

        entries.Enqueue(entry);
        while (entries.Count > options.MaxInMemoryEntries && entries.TryDequeue(out _))
        {
        }

        logger.LogDebug("[StrategyDayDebug] {Category} {Message}", category, message);
    }

    private void FlushToFile()
    {
        var snapshot = Snapshot();
        if (snapshot.Count == 0)
            return;

        try
        {
            var directory = Path.GetFullPath(options.OutputDirectory);
            Directory.CreateDirectory(directory);

            var stamp = DateTime.Now.ToString("yyyyMMdd");
            var gamePart = gameYear is int y && gameMonth is int m && gameDay is int d
                ? $"_{y:D4}{m:D2}{d:D2}"
                : string.Empty;
            var fileName = $"strategy-day_{stamp}{gamePart}.log";
            var path = Path.Combine(directory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"--- flush {DateTimeOffset.Now:O} scenario={scenarioId ?? "-"} entries={snapshot.Count} ---");
            foreach (var entry in snapshot)
            {
                sb.Append('[').Append(entry.At.ToString("HH:mm:ss.fff")).Append("] ");
                if (entry.GameYear is int gy && entry.GameMonth is int gm && entry.GameDay is int gd)
                    sb.Append('(').Append(gy).Append('-').Append(gm).Append('-').Append(gd).Append(") ");
                sb.Append('[').Append(entry.Category).Append("] ");
                sb.AppendLine(entry.Message);
            }

            sb.AppendLine();

            lock (fileLock)
            {
                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                lastWrittenFilePath = path;
            }

            logger.LogInformation("[StrategyDayDebug] 日推进日志已写入 {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[StrategyDayDebug] 写入日推进日志文件失败");
        }
    }
}
