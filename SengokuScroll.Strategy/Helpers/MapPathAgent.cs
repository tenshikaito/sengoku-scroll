using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 寻路用的临时地图单位代理（仅提供位置与移动接口，不参与战斗）。
/// </summary>
internal sealed class MapPathAgent : IHasForce, IHasLocation, IMovable
{
    public MapPathAgent(Point3 location, int forceId)
    {
        Location = location;
        ForceId = forceId;
    }

    public int LeaderId { get; set; }

    public int ForceId { get; set; }

    public Point3 Location { get; set; }

    public Direction4 Direction { get; set; }

    public int Ap { get; set; } = 999;

    public bool IsUnit => false;

    public bool IsReadyToMove => true;

    public bool IsMilitary => false;
}
