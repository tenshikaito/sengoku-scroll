namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// ÎäÆ÷ÀàĞÍ
/// </summary>
public class WeaponDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int AttackBonus { get; set; }
}
