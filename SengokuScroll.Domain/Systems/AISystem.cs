using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

public interface IAISystem : IGameSystem
{
}

public class AISystem(IGameContext context) : IAISystem
{
    public int Order { get; } = 40;

    public void Update()
    {
    }
}