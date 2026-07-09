namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 防具类型
/// </summary>
public class ArmorDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// 防御加成（百分比或固定值，语义由使用方决定）
    /// </summary>
    public int DefenseBonus { get; set; }
}
