using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// Æøºò
/// </summary>
public sealed class ClimateDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ClimateFactor SpringClimate { get; set; }

    public ClimateFactor SummerClimate { get; set; }

    public ClimateFactor AutumnClimate { get; set; }

    public ClimateFactor WinterClimate { get; set; }
}
