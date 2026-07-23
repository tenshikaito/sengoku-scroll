using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>将领执行的募兵/征兵任务状态。</summary>
public sealed class CharacterRecruitTask
{
    public CharacterRecruitTaskKind Kind { get; set; }

    /// <summary>募兵/征兵目标据点。</summary>
    public int StrongholdId { get; set; }

    /// <summary>汇报目的地（当主居城）。</summary>
    public int ReportStrongholdId { get; set; }

    public CharacterRecruitTaskPhase Phase { get; set; } = CharacterRecruitTaskPhase.Travel;

    /// <summary>任务总期限剩余天数（自派发日起 60 日）。</summary>
    public int DeadlineDaysRemaining { get; set; }

    /// <summary>执行阶段剩余天数（抵达目标后 20 日）。</summary>
    public int ExecutionDaysRemaining { get; set; }

    /// <summary>募兵预算（文）；征兵为 0。</summary>
    public int BudgetMoney { get; set; }

    /// <summary>募兵预算是否由执行者个人金库出资（否则由目标据点府库拨付）。</summary>
    public bool UsesPersonalFunds { get; set; }

    /// <summary>募兵剩余可用资金（文）。</summary>
    public int MoneyRemaining { get; set; }

    /// <summary>已征募人数（结算前累计）。</summary>
    public int SoldiersRecruited { get; set; }
}
