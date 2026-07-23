using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>情报界面人物展示字段（不受战争迷雾 DTO 过滤影响）。</summary>
public static class CharacterIntelDisplayHelper
{
    public static string ResolveHomeStrongholdName(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            foreach (var actor in stronghold.MerchantActors)
            {
                if (actor.CharacterIds.Contains(character.Id))
                    return stronghold.Name;
            }

            foreach (var actor in stronghold.ReligionActors)
            {
                if (actor.CharacterIds.Contains(character.Id))
                    return stronghold.Name;
            }

            if (stronghold.CivilianActor.CharacterIds.Contains(character.Id))
                return stronghold.Name;
        }

        var strongholdId = character.LocationType == CharacterLocationType.Stronghold
            ? character.LocationStrongholdId
            : character.StrongholdId;
        if (strongholdId > 0
            && gameData.Strongholds.TryGetValue(strongholdId, out var home))
        {
            return home.Name;
        }

        if (character.ForceId > 0
            && !OrganizationForceHelper.IsOrganizationForceId(gameData, character.ForceId))
        {
            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                character.ForceId,
                gameData,
                meta);
            if (residenceId > 0
                && gameData.Strongholds.TryGetValue(residenceId, out var residence))
            {
                return residence.Name;
            }
        }

        return string.Empty;
    }
}
