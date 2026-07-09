namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// ÎÄ»¯
/// </summary>
public class CultureDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required int CultureGroupId { get; set; }
}
