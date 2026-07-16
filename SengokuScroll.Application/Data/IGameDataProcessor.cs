using SengokuScroll.Domain;

namespace SengokuScroll.Application.Data;

/// <summary>剧本/存档加载与持久化入口。</summary>
public interface IGameDataProcessor
{
    /// <summary>按名称加载完整游戏世界。</summary>
    public GameWorld Load(string gameWorldName);

    /// <summary>保存当前世界状态。</summary>
    public void save(GameWorld gw);
}
