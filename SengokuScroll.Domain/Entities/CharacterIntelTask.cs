namespace SengokuScroll.Domain.Entities;

/// <summary>角色当前任务（情报 · 任务 Tab；Personal / Life / Force / PartTime）。</summary>
public sealed class CharacterIntelTask
{
    /// <summary>Personal | Life | Force | PartTime</summary>
    public required string TaskCategory { get; set; }

    public required string Name { get; set; }

    public required string Target { get; set; }

    public required string Status { get; set; }

    public required string Remaining { get; set; }
}
