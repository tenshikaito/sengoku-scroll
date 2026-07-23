using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>据点向角色发布的募兵/征兵任务令（尚未开始执行）。</summary>
public sealed class CharacterRecruitAssignment
{
    public CharacterRecruitTaskKind Kind { get; set; }

    /// <summary>执行目标据点。</summary>
    public int StrongholdId { get; set; }

    /// <summary>据点拨付的募兵预算（文）；征兵为 0。</summary>
    public int BudgetMoney { get; set; }
}
