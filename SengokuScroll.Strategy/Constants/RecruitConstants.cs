namespace SengokuScroll.Strategy.Constants;

/// <summary>募兵/征兵任务常数。</summary>
public static class RecruitConstants
{
    /// <summary>1 贯 = 1000 文。</summary>
    public const int MoneyPerKan = 1000;

    /// <summary>每名士兵消耗金钱（文）；1 贯约可募 10 人。</summary>
    public const int MoneyCostPerSoldier = 100;

    /// <summary>每名士兵消耗人口。</summary>
    public const int PopulationCostPerSoldier = 2;

    /// <summary>任务总期限（日）。</summary>
    public const int TaskDeadlineDays = 60;

    /// <summary>抵达目标后的执行天数。</summary>
    public const int ExecutionDays = 20;

    /// <summary>完成任务后增加的声望（功勋）。</summary>
    public const int MeritRewardOnComplete = 3;

    /// <summary>征兵：魅力每达此值每日多征 1 人（至少 1 人/日）。</summary>
    public const int ConscriptCharmDivisor = 5;

    /// <summary>征兵：每征若干人降低 1 点民心。</summary>
    public const int ConscriptPopularFeelingsSoldiersPerPoint = 25;

    /// <summary>征兵：每征若干人降低 1 点治安。</summary>
    public const int ConscriptStabilitySoldiersPerPoint = 30;

    /// <summary>募兵单次最低预算（文）。</summary>
    public const int MinMercenaryBudget = MoneyCostPerSoldier;

    public static int SoldiersAffordableByMoney(int budgetMoney)
        => budgetMoney / MoneyCostPerSoldier;

    public static int KanFromMoney(int money)
        => money / MoneyPerKan;
}
