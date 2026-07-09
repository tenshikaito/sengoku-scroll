using SengokuScroll.Domain;
using SengokuScroll.Domain.Systems;

namespace SengokuScroll.Strategy.Time;

/// <summary>
/// 策略模式时间控制器：管理暂停/继续，并负责「推进一日」的编排。
/// 单机 M1-b：每次 <see cref="AdvanceDay"/> 将日期 +1 并执行一日系统链。
/// </summary>
public class StrategyTimeController
{
    /// <summary>当前时间状态。</summary>
    public StrategyTimeState State { get; private set; } = StrategyTimeState.Paused;

    /// <summary>暂停时间推进。</summary>
    public void Pause() => State = StrategyTimeState.Paused;

    /// <summary>恢复时间推进（M1-b 仅改状态，自动 tick 在 M2 联调）。</summary>
    public void Resume() => State = StrategyTimeState.Running;

    /// <summary>
    /// 推进 1 天：更新世界日期并调用引擎执行各 System 的日循环。
    /// </summary>
    /// <param name="world">当前游戏世界。</param>
    /// <param name="engine">策略模式游戏引擎（含单位移动、后勤、信使等系统）。</param>
    public void AdvanceDay(GameWorld world, IGameEngine engine)
    {
        world.GameData.GameDate = world.GameData.GameDate.AddDays(1);
        engine.NextTime();
    }
}
