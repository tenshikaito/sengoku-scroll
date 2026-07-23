using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式角色体力：执行命令时扣体力并判定生病/死亡。</summary>
public interface IStrategyCharacterStaminaSystem : IGameSystem
{
}

/// <summary>在角色移动之后处理本日命令体力消耗（Order 31）。</summary>
public class StrategyCharacterStaminaSystem(IGameContext context) : IStrategyCharacterStaminaSystem
{
    public int Order { get; } = 31;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        foreach (var character in context.GameWorldContext.EachCharacter())
        {
            if (character.IsDead)
                continue;

            CharacterStaminaRules.ApplyCommandFatigue(character, gameData);
        }
    }
}
