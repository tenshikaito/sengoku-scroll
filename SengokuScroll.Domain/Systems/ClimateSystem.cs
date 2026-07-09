using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

public interface IClimateSystem : IGameSystem
{
}

public class ClimateSystem(IGameContext context) : IClimateSystem
{
    public int Order { get; } = 1;

    public void Update()
    {
    }
}