using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Entities;


public class StrongholdPoint
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public Point3 Location { get; set; }
}