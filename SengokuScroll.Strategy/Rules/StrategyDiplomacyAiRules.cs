using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;

namespace SengokuScroll.Strategy.Rules;

/// <summary>AI 的最低限度战争退出判断，防止弱势势力无限死战。</summary>
public static class StrategyDiplomacyAiRules
{
    public const int StaleWarDays = 360;

    public static int? SelectPeaceTarget(int forceId, GameData gameData)
    {
        var ownPower = CalculateMilitaryPower(forceId, gameData);
        var candidates = new List<(int TargetId, int OpponentPower, int WarAgeDays)>();

        foreach (var war in gameData.Wars.Values.Where(x => !x.IsEnded && WarRules.IsParticipant(x, forceId)))
        {
            var ownIsAggressor = WarRules.IsOnAggressorSide(war, forceId);
            var opponents = ownIsAggressor ? war.DefenderForceIds : war.AggressorForceIds;
            foreach (var targetId in opponents.Distinct())
            {
                if (!gameData.Forces.ContainsKey(targetId))
                    continue;

                candidates.Add((
                    targetId,
                    CalculateMilitaryPower(targetId, gameData),
                    Math.Max(0, gameData.GameDate.TotalDays - war.StartDate.TotalDays)));
            }
        }

        return candidates
            .Where(x => x.WarAgeDays >= StaleWarDays || ownPower * 100 < x.OpponentPower * 60)
            .OrderByDescending(x => x.OpponentPower)
            .ThenBy(x => x.TargetId)
            .Select(x => (int?)x.TargetId)
            .FirstOrDefault();
    }

    public static int CalculateMilitaryPower(int forceId, GameData gameData)
    {
        var field = gameData.Units.Values
            .Where(x => x.ForceId == forceId && x.IsMilitary)
            .Sum(x => Math.Max(0, x.Soldier));
        var garrison = gameData.Strongholds.Values
            .Where(x => x.ForceId == forceId)
            .Sum(x => Math.Max(0, x.ForceActor.Soldier));
        return field + garrison;
    }
}
