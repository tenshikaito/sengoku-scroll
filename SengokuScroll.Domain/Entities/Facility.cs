using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

public sealed class Facility
{
    public FacilityType TypeId { get; set; }

    public required string Name { get; set; }
}
