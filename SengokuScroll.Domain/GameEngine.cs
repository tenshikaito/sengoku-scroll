using SengokuScroll.Domain.Systems;

namespace SengokuScroll.Domain;

public interface IGameEngine
{
    void NextTime();
}

public abstract class GameEngineBase : IGameEngine
{
    protected readonly List<IGameSystem> systemList = [];

    public void NextTime()
    {
        foreach (var system in systemList)
            system.Update();
    }
}

public class ConfigurableGameEngine : GameEngineBase
{
    public ConfigurableGameEngine(IEnumerable<IGameSystem> systems)
    {
        systemList.AddRange(systems);
        systemList.Sort((a, b) => a.Order - b.Order);
    }
}

public class RpgGameEngine(
    IClimateSystem climateSystem,
    IEconomySystem economySystem,
    ICharacterSystem characterSystem,
    IUnitSystem unitSystem,
    IAISystem aiSystem)
    : ConfigurableGameEngine(
    [
        climateSystem,
        economySystem,
        characterSystem,
        unitSystem,
        aiSystem
    ])
{
}

public class StrategyGameEngine(
    IClimateSystem climateSystem,
    IEconomySystem economySystem,
    ICharacterSystem characterSystem,
    IUnitSystem unitSystem,
    IAISystem aiSystem)
    : ConfigurableGameEngine(
    [
        climateSystem,
        economySystem,
        characterSystem,
        unitSystem,
        aiSystem
    ])
{
}
