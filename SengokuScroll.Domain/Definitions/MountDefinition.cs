namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// ≥Àæﬂ¿‡–Õ
/// </summary>
public class MountDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int MovementBonus { get; set; }
}
