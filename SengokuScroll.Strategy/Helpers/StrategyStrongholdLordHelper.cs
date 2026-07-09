using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点领主解析：LordId=0 为当主直辖；领主居城须与 LordId 一致。</summary>
public static class StrategyStrongholdLordHelper
{
    /// <summary>解析势力当主角色 Id；无则 0。</summary>
    public static int ResolveForceLordCharacterId(
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (meta.ForceLordCharacterIds.TryGetValue(forceId, out var lordId)
            && gameData.Characters.ContainsKey(lordId))
        {
            return lordId;
        }

        return 0;
    }

    /// <summary>解析势力当主显示名。</summary>
    public static string ResolveForceLordName(
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        var lordCharacterId = ResolveForceLordCharacterId(forceId, meta, gameData);
        if (lordCharacterId > 0
            && gameData.Characters.TryGetValue(lordCharacterId, out var lordCharacter))
        {
            return lordCharacter.Name;
        }

        if (forceId == meta.PlayerForceId && !string.IsNullOrWhiteSpace(meta.LordName))
            return meta.LordName.Trim();

        return "当主";
    }

    /// <summary>据点领主展示名；LordId=0 时返回势力当主名（必非空）。</summary>
    public static string ResolveStrongholdLordName(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (stronghold.LordId > 0
            && gameData.Characters.TryGetValue(stronghold.LordId, out var lord))
        {
            return lord.Name;
        }

        return ResolveForceLordName(stronghold.ForceId, meta, gameData);
    }

    /// <summary>是否当主直辖（LordId=0）。</summary>
    public static bool IsDirectRule(Stronghold stronghold) => stronghold.LordId == 0;

    /// <summary>将领主角色驻留于居城（LordId 对应据点）。</summary>
    public static void EnsureLordResidence(Stronghold stronghold, Character lord)
    {
        lord.ForceId = stronghold.ForceId;
        lord.StrongholdId = stronghold.Id;
        lord.Location = stronghold.Location;
        lord.LocationType = CharacterLocationType.Stronghold;
    }
}
