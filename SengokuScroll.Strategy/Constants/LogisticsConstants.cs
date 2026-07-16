namespace SengokuScroll.Strategy.Constants;

/// <summary>
/// 战国时代兵站·输送相关的设计常数（史实简化参照，见 strategy-development-plan §6.1.1）。
/// 本类仅存放常量，不含方法；计算逻辑见 <see cref="Calculators.LogisticsCalculator"/>。
/// 口粮使用毫合（milli-go）整型，1000 = 1 合。
/// </summary>
public static class LogisticsConstants
{
    /// <summary>1 石 = 1000 合。</summary>
    public const int GoPerKoku = 1000;

    /// <summary>出征兵士日粮（毫合/人/日）；3000 ≈ 一日三合。</summary>
    public const int DailySoldierRationMilliGo = 3000;

    /// <summary>市民日粮（毫合/人/日）；2000 = 2 合。</summary>
    public const int DailyCivilianRationMilliGo = 2000;

    /// <summary>输送人夫日粮（毫合/人/日）；1500 = 1.5 合。</summary>
    public const int PorterDailyRationMilliGo = 1500;

    /// <summary>默认运输队人夫数。</summary>
    public const int DefaultPorterCount = 50;

    /// <summary>默认运输队护卫兵数。</summary>
    public const int DefaultEscortSoldierCount = 20;

    /// <summary>默认新队载粮（合），约 5 石。</summary>
    public const int DefaultConvoyCargoGo = 5000;

    /// <summary>运输队每日移动力（慢于战斗单位）。</summary>
    public const int ConvoyDailyAp = 4;

    /// <summary>信使每日移动力。</summary>
    public const int MessengerDailyAp = 6;

    /// <summary>默认信使传令兵（NPC）人数。</summary>
    public const int DefaultMessengerCourierCount = 2;

    /// <summary>默认信使护卫兵（NPC）人数。</summary>
    public const int DefaultMessengerEscortCount = 8;

    /// <summary>假情报导致运输队原地停留天数（M3 简化）。</summary>
    public const int FalseIntelligenceHoldDays = 3;
}
