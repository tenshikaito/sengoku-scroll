using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Systems;

/// <summary>军事单位日推进：移动中单位沿路径消耗 AP 前进。</summary>
public interface IUnitSystem : IGameSystem
{

}

/// <summary>
/// Domain 层单位移动入口；策略模式由 <see cref="SengokuScroll.Strategy.Systems.StrategyUnitSystem"/> 继承并注入相同 Order。
/// </summary>
public class UnitSystem(
    IGameContext context,
    UnitMoveAction moveAction)
    : IUnitSystem
{
    /// <summary>在 AI 决策之后、接敌/战斗之前推进路径。</summary>
    public int Order { get; } = 20;

    /// <inheritdoc />
    public void Update()
    {
        UpdateMove();
    }

    /// <summary>仅处理 <see cref="UnitStatus.Moving"/> 单位，逐格校验并扣 AP。</summary>
    private void UpdateMove()
    {
        foreach (var o in context.GameWorldContext.EachUnit().Where(oo => oo.Status == UnitStatus.Moving))
            moveAction.Update(o);
    }
}