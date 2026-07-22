using Microsoft.Extensions.Logging;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Policies.CharacterAi;

public interface ICharacterAiActionHandler
{
    CharacterAiActionKind Kind { get; }

    bool Execute(
        IGameWorldContext worldContext,
        Character character,
        CharacterAiEvaluation evaluation,
        GameData gameData,
        StrategyScenarioMeta scenarioMeta,
        IPathfindingService pathfinding,
        ILogger logger);
}

internal sealed class RestCharacterAiActionHandler : ICharacterAiActionHandler
{
    public static readonly RestCharacterAiActionHandler Instance = new();
    public CharacterAiActionKind Kind => CharacterAiActionKind.Rest;

    public bool Execute(
        IGameWorldContext worldContext,
        Character character,
        CharacterAiEvaluation evaluation,
        GameData gameData,
        StrategyScenarioMeta scenarioMeta,
        IPathfindingService pathfinding,
        ILogger logger)
    {
        if (!CharacterAiMovementHelper.TryBeginRest(character))
            return false;

        logger.LogDebug(
            "[CharacterAI] {Name}#{Id} Rest score={Score} reason={Reason}",
            character.Name, character.Id, evaluation.Score, evaluation.Reason);
        return true;
    }
}

internal sealed class StrongholdRouteCharacterAiActionHandler : ICharacterAiActionHandler
{
    public CharacterAiActionKind Kind { get; }

    public StrongholdRouteCharacterAiActionHandler(CharacterAiActionKind kind)
        => Kind = kind;

    public bool Execute(
        IGameWorldContext worldContext,
        Character character,
        CharacterAiEvaluation evaluation,
        GameData gameData,
        StrategyScenarioMeta scenarioMeta,
        IPathfindingService pathfinding,
        ILogger logger)
    {
        if (evaluation.TargetStrongholdId <= 0
            || !gameData.Strongholds.TryGetValue(evaluation.TargetStrongholdId, out var target))
            return false;

        if (!CharacterAiMovementHelper.TryRouteToStronghold(
                worldContext,
                character,
                target,
                scenarioMeta,
                pathfinding))
            return false;

        logger.LogDebug(
            "[CharacterAI] {Name}#{Id} {Kind}→{Target} score={Score} reason={Reason}",
            character.Name,
            character.Id,
            evaluation.Kind,
            target.Name,
            evaluation.Score,
            evaluation.Reason);
        return true;
    }
}

public static class CharacterAiActionHandlerRegistry
{
    private static readonly Dictionary<CharacterAiActionKind, ICharacterAiActionHandler> ByKind =
        new ICharacterAiActionHandler[]
        {
            RestCharacterAiActionHandler.Instance,
            new StrongholdRouteCharacterAiActionHandler(CharacterAiActionKind.TaskRun),
            new StrongholdRouteCharacterAiActionHandler(CharacterAiActionKind.Visit)
        }.ToDictionary(h => h.Kind);

    public static bool Execute(
        CharacterAiActionKind kind,
        IGameWorldContext worldContext,
        Character character,
        CharacterAiEvaluation evaluation,
        GameData gameData,
        StrategyScenarioMeta scenarioMeta,
        IPathfindingService pathfinding,
        ILogger logger)
        => ByKind.TryGetValue(kind, out var handler)
           && handler.Execute(
               worldContext,
               character,
               evaluation,
               gameData,
               scenarioMeta,
               pathfinding,
               logger);
}
