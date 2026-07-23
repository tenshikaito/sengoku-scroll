namespace SengokuScroll.Domain.Entities.Types;

/// <summary>募兵/征兵任务阶段。</summary>
public enum CharacterRecruitTaskPhase
{
    /// <summary>前往目标据点。</summary>
    Travel,

    /// <summary>在目标据点执行募兵/征兵（20 日）。</summary>
    Execute,

    /// <summary>回居城汇报并结算。</summary>
    Report,
}
