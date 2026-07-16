using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>地图地标（与 playable <see cref="Stronghold"/> 分离）。</summary>
public class Landmark
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public Point3 Location { get; set; }
}
