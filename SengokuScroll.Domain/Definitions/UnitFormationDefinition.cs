namespace SengokuScroll.Domain.Definitions;


public class UnitFormationDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public byte Attack { get; set; }

    public byte Defense { get; set; }

    public byte Movement { get; set; }
}
