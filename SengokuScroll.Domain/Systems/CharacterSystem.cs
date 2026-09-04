using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Domain.Systems;

/// <summary>角色日推进：地图独立角色移动与 AP 恢复（RPG/策略共用 Domain 层）。</summary>
public interface ICharacterSystem : IGameSystem
{
}

/// <summary>
/// 日推进处理所有 <see cref="Character"/>：沿路径移动（<see cref="CharacterMoveAction"/>）。
/// 策略模式 AP 日初恢复由 <see cref="SengokuScroll.Strategy.Systems.StrategyTimeSystem"/> 负责。
/// </summary>
public class CharacterSystem(
    IGameContext context,
    CharacterMoveAction moveAction)
    : ICharacterSystem
{
    /// <summary>在单位/信使之后执行，便于同日移动后再恢复 AP。</summary>
    public int Order { get; } = 30;

    /// <inheritdoc />
    public void Update()
    {
        // 阶段1：推进移动中角色（溃逃将领、信使等）沿路径前进
        foreach (var o in context.GameWorldContext.EachCharacter())
        {
            moveAction.Update(o);

            // RPG 的事件循环同样按“日”推进；移动结算后恢复 AP，供下一日继续行动。
            // 战略模式使用独立的 StrategyTimeSystem，不会执行本通用系统。
            o.Ap = Math.Min(
                context.GameRuleConfig.MilitaryMaxMovement,
                o.Ap + context.GameRuleConfig.NextTurnApRecovery);
        }
    }
}
