namespace SengokuScroll.Strategy.Constants;

/// <summary>野战 / 自动战斗数值边界（M3）。</summary>
public static class BattleConstants
{
    public const int MinWinRatePercent = 5;
    public const int MaxWinRatePercent = 95;
    public const int DefaultCombatStat = 10;

    /// <summary>两军合计兵数不低于此值时，当日更倾向「对峙」而非立即决战（仍每日接敌）。</summary>
    public const int LargeArmySoldierThreshold = 6000;

    /// <summary>一方调整后胜率 ≥ 此值时，AI 认为适合当日强袭。</summary>
    public const int CommitAssaultWinRateThreshold = 58;

    /// <summary>攻城时进攻方（占领/劫掠方针）强袭阈值略低。</summary>
    public const int SiegeCommitWinRateThreshold = 52;

    /// <summary>对峙累计达此天数时强制决战。</summary>
    public const int StandoffForceBattleDays = 30;

    /// <summary>AI 在同格对峙超过此天数且胜率偏低时主动脱离并改撤退。</summary>
    public const int AiStandoffBreakRetreatDays = 12;

    public const int RetreatApBonusBase = 3;
    public const int RetreatMoraleBonusThreshold = 70;

    /// <summary>低于此士气不可主动接敌/强袭。</summary>
    public const int LowMoraleEngageThreshold = 35;

    public const int WinnerMoraleGain = 12;
    public const int LoserMoraleLoss = 18;
    public const int InspiringMoraleThreshold = 75;
    public const int FearfulMoraleThreshold = 30;

    /// <summary>小股接敌后默认试探天数（不含当日），期满后才倾向决战。</summary>
    public const int SmallArmyProbeDays = 2;

    /// <summary>对峙期间仅在这些日数向当主发战报信使（非决战日）。</summary>
    public static readonly int[] StandoffReportDays = [3, 5, 10, 15, 20, 30];

    /// <summary>评估强袭胜率 ≥ 此值时可提出劝降。</summary>
    public const int SurrenderOfferWinRateThreshold = 82;

    /// <summary>胜率 ≥ 此值时即使兵力比略低也可劝降。</summary>
    public const int SurrenderAbsoluteWinRateThreshold = 90;

    /// <summary>劝降所需最低兵力比（攻/守）。</summary>
    public const double SurrenderMinStrengthRatio = 1.8;

    /// <summary>劝降接受率基础值（%）。</summary>
    public const int SurrenderAcceptBasePercent = 42;

    /// <summary>预计接受率低于此值则不浪费劝降。</summary>
    public const int SurrenderMinAcceptChanceToOffer = 28;

    /// <summary>AI 邻敌胜率低于此值时倾向撤退而非硬接。</summary>
    public const int AiRetreatCommitWinRateThreshold = 38;

    public static bool IsStandoffReportDay(int standoffDays)
        => StandoffReportDays.Contains(standoffDays);
}
