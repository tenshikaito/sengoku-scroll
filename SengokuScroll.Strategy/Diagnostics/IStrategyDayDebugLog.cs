namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>策略模式日推进结构化 debug 日志（内存 + 可选文件）。</summary>
public interface IStrategyDayDebugLog
{
    bool IsEnabled { get; }

    /// <summary>最近一次写入文件的绝对路径（若有）。</summary>
    string? LastWrittenFilePath { get; }

    /// <summary>日初：清空当日缓冲并写入日头标记。</summary>
    void BeginDay(int year, int month, int day, string? scenarioId = null);

    /// <summary>系统链开始执行某一 System。</summary>
    void LogSystemStart(string systemName, int order);

    /// <summary>系统链结束执行某一 System。</summary>
    void LogSystemEnd(string systemName, int order);

    /// <summary>自由文本（已格式化的单行消息）。</summary>
    void LogLine(string category, string message);

    /// <summary>使用本地化 key 写入一行（占位符参数可选）。</summary>
    void LogLocalized(string category, string localizationKey, params object?[] args);

    /// <summary>日末：汇总并可选刷盘。</summary>
    void EndDay(int battleCount, int eventCount);

    IReadOnlyList<StrategyDayDebugEntry> Snapshot();

    void Clear();
}
