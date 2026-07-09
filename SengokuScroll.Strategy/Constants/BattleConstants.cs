namespace SengokuScroll.Strategy.Constants;

/// <summary>瞬间战数值边界（M3-a 简化版）。</summary>
public static class BattleConstants
{
    /// <summary>胜率预览与结算的下限（%）。</summary>
    public const int MinWinRatePercent = 5;

    /// <summary>胜率预览与结算的上限（%）。</summary>
    public const int MaxWinRatePercent = 95;

    /// <summary>单位 Attack/Defense 未配置时的默认攻防。</summary>
    public const int DefaultCombatStat = 10;
}
