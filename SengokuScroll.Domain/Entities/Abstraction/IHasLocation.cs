using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain.Entities.Abstraction;

public interface IHasLocation : IMapObject
{
    Direction4 Direction { get; }

    public int Ap { get; }
}