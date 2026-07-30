namespace SengokuScroll.Domain.Entities;

/// <summary>将领执行中的外交使节任务（同盟/宣战/议和）。</summary>
public sealed class CharacterDiplomacyMission
{
    /// <summary>Ally | War | Peace</summary>
    public required string Action { get; set; }

    public int TargetForceId { get; set; }

    public int RemainingDays { get; set; }

    public int SuccessChancePercent { get; set; }
}
