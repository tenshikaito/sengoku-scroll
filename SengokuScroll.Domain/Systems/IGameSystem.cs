namespace SengokuScroll.Domain.Systems;

public interface IGameSystem
{
    int Order { get; }

    void Update();
}