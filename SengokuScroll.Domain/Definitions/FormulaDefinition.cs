namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 配方
/// </summary>
public class FormulaDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public Dictionary<int, int>? Ingredients { get; set; }

    public int ResultItemId { get; set; }

    /// <summary>
    /// 成功率
    /// </summary>
    public byte SuccessRate { get; set; }
}
