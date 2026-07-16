using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 按难度启用的规则开关。新难度档位在此追加，避免散落 if。
/// </summary>
public static class StrategyDifficultyRules
{
    /// <summary>解析字符串难度；无法识别时默认标准。</summary>
    public static StrategyDifficulty Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return StrategyDifficulty.Normal;

        return Enum.TryParse<StrategyDifficulty>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : StrategyDifficulty.Normal;
    }

    /// <summary>
    /// 是否允许玩家势力在战斗当日（不等信使）解锁完整战报。
    /// 仅简易开启——写实模式下前线溃灭、主帅不知情是合理的。
    /// </summary>
    public static bool AllowImmediateBattleReport(StrategyDifficulty difficulty)
        => difficulty == StrategyDifficulty.Easy;

    /// <summary>败方败退后强制保留的最低残部比例（与 <see cref="BattleCasualtyRules.MinDefeatSurvivorRatio"/> 一致）。</summary>
    public static double DefeatResidualSoldierRatio(StrategyDifficulty difficulty)
        => BattleCasualtyRules.MinDefeatSurvivorRatio(difficulty);

    /// <summary>追击决战时，撤退方脱离成功率（百分点）。</summary>
    public static int PursuitDisengageChancePercent(StrategyDifficulty difficulty)
        => difficulty switch
        {
            StrategyDifficulty.Easy => 55,
            StrategyDifficulty.Normal => 40,
            StrategyDifficulty.Hard => 28,
            StrategyDifficulty.Legendary => 18,
            _ => 40
        };
}
