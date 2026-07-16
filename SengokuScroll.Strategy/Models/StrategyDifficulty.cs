namespace SengokuScroll.Strategy.Models;

/// <summary>
/// 策略模式难度。后续可为各档补全规则表；当前先提供框架与关键开关。
/// </summary>
public enum StrategyDifficulty : byte
{
    /// <summary>简易：前线战报可当日解锁（便利情报，牺牲写实）。</summary>
    Easy = 0,

    /// <summary>标准：异格战报须信使抵达后解锁；同格仍可目击。</summary>
    Normal = 1,

    /// <summary>困难：预留（AI 更进取、补给更严等）。</summary>
    Hard = 2,

    /// <summary>铁人/传奇：预留（存档限制、情报更苛刻等）。</summary>
    Legendary = 3
}
