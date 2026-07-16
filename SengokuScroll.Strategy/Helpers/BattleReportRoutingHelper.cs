using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>战报信使投递目标：当主所在、居城、参战将领领地等。</summary>
public static class BattleReportRoutingHelper
{
    public readonly record struct BattleReportDestination(Point3 Location, int SourceStrongholdId, string Label);

    /// <summary>
    /// 解析势力战报应送达的格点（去重）。
    /// 玩家势力：当主当前位置 + 当主居城 + 本势力参战部队将领领地。
    /// </summary>
    public static IReadOnlyList<BattleReportDestination> ResolveDestinations(
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        Unit attacker,
        Unit defender)
    {
        var seen = new HashSet<(int X, int Y)>();
        var destinations = new List<BattleReportDestination>();

        void Add(Point3 location, int sourceStrongholdId, string label)
        {
            if (!seen.Add((location.X, location.Y)))
                return;

            destinations.Add(new BattleReportDestination(location, sourceStrongholdId, label));
        }

        if (forceId == meta.PlayerForceId)
        {
            var lordLocation = StrategyLordHelper.ResolveLocation(gameData, meta);
            var lordStrongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, meta, lordLocation);
            Add(lordLocation, lordStrongholdId, "当主");

            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(forceId, gameData, meta);
            if (residenceId > 0
                && gameData.Strongholds.TryGetValue(residenceId, out var residence))
            {
                Add(residence.Location, residence.Id, "居城");
            }
        }
        else
        {
            var forceMeta = BuildForceMeta(forceId, gameData);
            var lordLocation = StrategyLordHelper.ResolveLocation(gameData, forceMeta);
            var lordStrongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, forceMeta, lordLocation);
            Add(lordLocation, lordStrongholdId, "当主");
        }

        foreach (var unit in new[] { attacker, defender })
        {
            if (unit.ForceId != forceId || unit.LeaderId <= 0)
                continue;

            if (!gameData.Characters.TryGetValue(unit.LeaderId, out var commander))
                continue;

            if (commander.StrongholdId <= 0
                || !gameData.Strongholds.TryGetValue(commander.StrongholdId, out var territory))
                continue;

            Add(territory.Location, territory.Id, $"{commander.Name}领地");
        }

        return destinations;
    }

    private static StrategyScenarioMeta BuildForceMeta(int forceId, GameData gameData)
    {
        var stronghold = gameData.Strongholds.Values
            .Where(s => s.ForceId == forceId)
            .OrderBy(s => s.Id)
            .FirstOrDefault();

        return new StrategyScenarioMeta
        {
            PlayerForceId = forceId,
            LordStrongholdId = stronghold?.Id,
            LordName = stronghold?.Name ?? "当主"
        };
    }
}
