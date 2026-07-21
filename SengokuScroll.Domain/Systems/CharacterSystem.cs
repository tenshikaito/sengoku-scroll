using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Domain.Systems;

/// <summary>角色日推进：地图独立角色移动与 AP 恢复（RPG/策略共用 Domain 层）。</summary>
public interface ICharacterSystem : IGameSystem
{
}

/// <summary>
/// 日推进处理所有 <see cref="Character"/>：先沿路径移动（<see cref="CharacterMoveAction"/>），再恢复 AP。
/// 策略模式由 <see cref="SengokuScroll.Strategy.Systems.StrategyMessengerSystem"/> 等后续系统投递信使/战报。
/// </summary>
public class CharacterSystem(
    IGameContext context,
    CharacterMoveAction moveAction,
    GameRuleConfig gameRuleConfig)
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
            UpdateNext(o);
        }
    }

    /// <summary>日末恢复角色行动力（与军事单位日初恢复对称，供 RPG 回合制复用）。</summary>
    private void UpdateNext(Character o)
    {
        o.Ap += gameRuleConfig.NextTurnApRecovery;
    }
}