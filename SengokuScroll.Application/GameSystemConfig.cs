namespace SengokuScroll.Application;

/// <summary>应用层可调参数：如 RPG 移动后推进间隔（毫秒）。</summary>
public class GameSystemConfig
{
    /// <summary>RPG 事件循环：移动等行为后触发日推进的间隔（默认 800ms）。</summary>
    public int MovingNextTurnIntervalMilisecond { get; set; } = 800;
}
