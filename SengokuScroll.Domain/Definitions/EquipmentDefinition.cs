namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// ±ø×°
/// </summary>
public class EquipmentDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int Weight { get; set; }

    public required string Description { get; set; }
}
