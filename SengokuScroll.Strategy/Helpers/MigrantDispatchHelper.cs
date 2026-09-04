using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>移民队生成与派遣。</summary>
public class MigrantDispatchHelper(
    IGameContext context,
    IPathfindingService pathfindingService)
{
    public int EvaluateAndDispatchDailyMigrations()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var created = 0;

        foreach (var origin in gameData.Strongholds.Values)
        {
            if (!ShouldEmigrate(origin))
                continue;

            if (TransportUnitRules.HasActiveMigrantFromOrigin(gameData, origin.Id))
                continue;

            var destination = FindBestDestination(origin, gameData);
            if (destination is null)
                continue;

            if (TryCreateMigrantConvoy(origin, destination, gameData))
                created++;
        }

        return created;
    }

    private static bool ShouldEmigrate(Stronghold stronghold)
    {
        if (stronghold.Population < 1000)
            return false;

        return stronghold.CivilianActor.PopularFeelings < MigrantConstants.EmigrationPopularFeelingsThreshold
               || stronghold.Stability < MigrantConstants.EmigrationStabilityThreshold;
    }

    private Stronghold? FindBestDestination(Stronghold origin, GameData gameData)
    {
        Stronghold? best = null;
        var bestScore = int.MinValue;

        foreach (var candidate in gameData.Strongholds.Values)
        {
            if (candidate.Id == origin.Id)
                continue;

            var score = ScoreDestination(origin, candidate);
            if (score <= bestScore)
                continue;

            var path = pathfindingService.CalculatePath(
                new MapPathAgent(origin.Location, origin.ForceId),
                candidate.Location);
            if (path is null || path.Count <= 1)
                continue;

            bestScore = score;
            best = candidate;
        }

        return best;
    }

    private static int ScoreDestination(Stronghold origin, Stronghold candidate)
    {
        var feelingsScore = candidate.CivilianActor.PopularFeelings * 2 + candidate.Stability;
        var distance = Math.Abs(origin.Location.X - candidate.Location.X)
                       + Math.Abs(origin.Location.Y - candidate.Location.Y);
        return feelingsScore - distance;
    }

    private bool TryCreateMigrantConvoy(
        Stronghold origin,
        Stronghold destination,
        GameData gameData)
    {
        if (GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData))
            return false;

        var migrants = Math.Min(
            MigrantConstants.MaxMigrantsPerConvoy,
            origin.Population * MigrantConstants.EmigrationPopulationRateBp / 10_000);
        migrants = Math.Max(100, migrants);

        if (origin.Population <= migrants + 500)
            return false;

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin.Location, origin.ForceId),
            destination.Location);
        if (path is null || path.Count <= 1)
            return false;

        MigrantConvoyActions.ApplyOriginDepartureEffects(origin, migrants);

        ConvoyUnitFactory.CreateTransportUnit(
            context.GameWorldContext.GameWorld,
            $"{origin.Name}移民→{destination.Name}",
            forceId: 0,
            leaderId: 0,
            origin.Location,
            origin.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo: 0,
            moneyCargo: 0,
            cargoPopulation: migrants,
            TransportPurpose.Migrant,
            RouteCalculator.ToDailyRouteQueuePoint2(path));

        return true;
    }
}
