using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Rules;

/// <summary>农业劳力：农兵外出降低可用劳力。</summary>
public static class AgricultureLaborRules
{
    public static int CalculateLaborCapacity(Stronghold stronghold)
        => Math.Max(1, stronghold.Population / AgricultureConstants.PopulationPerFarmer);

    /// <summary>自本据点派出的农兵（足轻）在外人数。</summary>
    public static int CountMilitiaAway(Stronghold stronghold, GameData gameData)
    {
        var away = 0;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (unit.SubUnitIds.Count == 0)
            {
                if (unit.ActionTarget.StrongholdId == stronghold.Id)
                    away += unit.Soldier;

                continue;
            }

            foreach (var subId in unit.SubUnitIds)
            {
                if (!gameData.SubUnits.TryGetValue(subId, out var sub))
                    continue;

                if (sub.StrongholdId != stronghold.Id)
                    continue;

                if (sub.TypeId == StrategyTroopTypes.Ashigaru)
                    away += Math.Max(0, sub.Soldier);
            }
        }

        return away;
    }

    /// <summary>可用劳力比（0–10000）。</summary>
    public static int CalculateLaborRatioBp(Stronghold stronghold, GameData gameData)
    {
        var capacity = CalculateLaborCapacity(stronghold);
        var away = Math.Min(capacity, CountMilitiaAway(stronghold, gameData));
        var available = Math.Max(0, capacity - away);
        return available * AgricultureConstants.ProgressBasisPoints / capacity;
    }

    public static int CalculateLaborAvailable(Stronghold stronghold, GameData gameData)
    {
        var capacity = CalculateLaborCapacity(stronghold);
        var away = Math.Min(capacity, CountMilitiaAway(stronghold, gameData));
        return Math.Max(0, capacity - away);
    }
}
