using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Domain.Systems;

public interface ICharacterSystem : IGameSystem
{
}

public class CharacterSystem(
    IGameContext context,
    CharacterMoveAction moveAction,
    GameRuleConfig gameRuleConfig)
    : ICharacterSystem
{
    public int Order { get; } = 30;

    public void Update()
    {
        // 如果角色在移动状态
        foreach (var o in context.GameWorldContext.EachCharacter())
        {
            moveAction.Update(o);
            UpdateNext(o);
        }
    }

    private void UpdateNext(Character o)
    {
        o.Ap += gameRuleConfig.NextTurnApRecovery;
    }
}