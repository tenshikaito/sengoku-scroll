namespace SengokuScroll.Domain.Entities.Abstraction;

public interface IHasForce : IHasLeader
{
    int ForceId { get; }
}