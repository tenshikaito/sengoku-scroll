namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 通用任务状态。具体任务可继承该类型，同时统一提供分类、目标、状态与剩余量。
/// </summary>
public class GameTask
{
    /// <summary>Personal | Life | Force | PartTime。</summary>
    public required string TaskCategory { get; set; }

    public required string Name { get; set; }

    public required string Target { get; set; }

    public required string Status { get; set; }

    public required string Remaining { get; set; }

    /// <summary>稳定任务键；便于 UI、存档迁移和去重。</summary>
    public string Key => $"{TaskCategory}:{Name}:{Target}";

    public bool IsCompleted => Status is "完成" or "已完成";

    public void Complete()
    {
        Status = "完成";
        Remaining = "0";
    }
}
