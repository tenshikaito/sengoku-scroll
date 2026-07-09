namespace SengokuScroll.Domain.Entities.Abstraction;

public interface IMovable : IHasLocation, IHasForce
{
    bool IsUnit { get; }

    bool IsReadyToMove { get; }

    bool IsMilitary { get; }
}
