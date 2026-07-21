using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 难度解析与情报相关开关。战斗伤亡/追击成功率不随难度变化。
/// </summary>
public static class StrategyDifficultyRules
{
    /// <summary>败退后至少保留的残部比例（全难度一致）。</summary>
    public const double DefaultDefeatSurvivorRatio = 0.50;

    /// <summary>追击脱离成功率（百分点，全难度一致）。</summary>
    public const int DefaultPursuitDisengageChancePercent = 40;

    /// <summary>解析字符串难度；无法识别时默认标准。</summary>
    public static StrategyDifficulty Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return StrategyDifficulty.Normal;

        var trimmed = value.Trim();
        if (trimmed.Equals("Legendary", StringComparison.OrdinalIgnoreCase))
            return StrategyDifficulty.Hard;

        return Enum.TryParse<StrategyDifficulty>(trimmed, ignoreCase: true, out var parsed)
            ? parsed
            : StrategyDifficulty.Normal;
    }

    /// <summary>
    /// 是否开启即时事件摘要（UI 通道；信使/Message 权威通道不变）。
    /// Easy 预设强制开启；其它读 <see cref="GameStartOptions"/>。
    /// </summary>
    public static bool InstantEventMessages(StrategyDifficulty difficulty, GameStartOptions? options = null)
    {
        if (difficulty == StrategyDifficulty.Easy)
            return true;

        return options?.InstantEventMessages ?? false;
    }

    /// <summary>兼容旧测试名。</summary>
    [Obsolete("Use InstantEventMessages with GameStartOptions.")]
    public static bool AllowImmediateBattleReport(StrategyDifficulty difficulty)
        => InstantEventMessages(difficulty);

    /// <summary>败方败退后强制保留的最低残部比例（与难度无关）。</summary>
    public static double DefeatResidualSoldierRatio(StrategyDifficulty difficulty)
        => DefaultDefeatSurvivorRatio;

    /// <summary>追击决战时，撤退方脱离成功率（与难度无关）。</summary>
    public static int PursuitDisengageChancePercent(StrategyDifficulty difficulty)
        => DefaultPursuitDisengageChancePercent;
}
