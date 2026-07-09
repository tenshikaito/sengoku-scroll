using SengokuScroll.Domain;

namespace SengokuScroll.Application.Data;

public interface IGameDataProcessor
{
    public GameWorld Load(string gameWorldName);

    public void save(GameWorld gw);
}
