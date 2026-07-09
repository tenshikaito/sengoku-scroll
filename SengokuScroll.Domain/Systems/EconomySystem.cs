using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

public interface IEconomySystem : IGameSystem
{

}

public class EconomySystem(IGameContext context) : IEconomySystem
{
    public int Order { get; } = 10;

    public void Update()
    {
    }
}