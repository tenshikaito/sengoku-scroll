using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式军事单位系统接口。</summary>
public interface IStrategyUnitSystem : IUnitSystem
{
}

/// <summary>
/// 策略模式军事单位系统：每日推进处于移动状态的单位沿路径前进一格（或多格直至 AP 不足）。
/// 移动校验由 Domain <see cref="UnitMoveAction"/> 与 Evaluator 负责。
/// </summary>
public class StrategyUnitSystem(
    IGameContext context,
    UnitMoveAction moveAction) : IStrategyUnitSystem
{
    /// <summary>与 Domain 单位系统同级，在后勤之后、信使之前。</summary>
    public int Order { get; } = 20;

    /// <inheritdoc />
    public void Update()
    {
        // 阶段1：推进所有移动中单位沿路径消耗 AP 前进
        foreach (var unit in context.GameWorldContext.EachUnit().Where(u =>
                     u.Status == UnitStatus.Moving && u.IsReadyToMove))
            moveAction.Update(unit);
    }
}
