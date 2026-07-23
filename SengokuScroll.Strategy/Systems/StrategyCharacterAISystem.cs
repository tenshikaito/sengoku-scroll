using Microsoft.Extensions.Logging;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Policies.CharacterAi;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式角色 AI 系统接口。</summary>
public interface IStrategyCharacterAISystem : IGameSystem
{
}

/// <summary>
/// 角色 AI：综合体力、年龄、疾病、心情与任务状态，执行 Visit / TaskRun（休息则就地 Resting）。
/// 运行于军事 AI 之前（Order 17）。
/// </summary>
public class StrategyCharacterAISystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    IPathfindingService pathfinding,
    ILogger<StrategyCharacterAISystem> logger) : IStrategyCharacterAISystem
{
    /// <summary>军事 AI 之前决策角色行动。</summary>
    public int Order { get; } = 17;

    public void Update()
    {
        var worldContext = context.GameWorldContext;
        var gameData = worldContext.GameWorld.GameData;
        var gameDate = gameData.GameDate;
        var playerForceId = scenarioMeta.PlayerForceId;
        var allAi = scenarioMeta.AllForcesAiControlled;

        foreach (var character in worldContext.EachCharacter())
        {
            if (character.ActionStatus == CharacterActionStatus.Resting)
            {
                ProcessResting(character, gameDate);
                continue;
            }

            if (CharacterAiRules.ShouldSkipDailyAi(character))
                continue;

            if (!allAi
                && character.ForceId == playerForceId
                && CharacterAiRules.IsPlayerLord(character, scenarioMeta, gameData))
            {
                continue;
            }

            if (character.LastAiCheckDate == gameDate)
                continue;

            character.LastAiCheckDate = gameDate;

            var evaluation = CharacterAiRules.EvaluateDailyAction(
                character,
                gameData,
                scenarioMeta,
                gameDate);

            if (evaluation.Kind == CharacterAiActionKind.None)
                continue;

            CharacterAiActionHandlerRegistry.Execute(
                evaluation.Kind,
                worldContext,
                character,
                evaluation,
                gameData,
                scenarioMeta,
                pathfinding,
                logger);
        }
    }

    private static void ProcessResting(Character character, GameDate gameDate)
    {
        var recovery = CharacterStaminaRules.ResolveRestRecovery(character, gameDate);
        character.Hp = Math.Min(100, character.Hp + recovery);
        character.Emotion = Math.Min(100, character.Emotion + 2);

        CharacterStaminaRules.TryRecoverFromIllness(character);

        if (character.Hp >= 75 && !character.IsSick)
            character.ActionStatus = CharacterActionStatus.Waiting;
    }
}
