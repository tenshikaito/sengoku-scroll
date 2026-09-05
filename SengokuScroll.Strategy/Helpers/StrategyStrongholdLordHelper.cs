using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点领主解析：LordId=0 为当主直辖；领主居城须与 LordId 一致。</summary>
public static class StrategyStrongholdLordHelper
{
    /// <summary>解析势力当主角色 Id；无则 0。</summary>
    public static int ResolveForceLordCharacterId(
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        StrategyForceLordRegistry? lordRegistry = null)
    {
        if (lordRegistry?.TryGetLordCharacterId(forceId, out var runtimeLordId) == true
            && IsValidLord(runtimeLordId))
        {
            return runtimeLordId;
        }

        if (gameData.Forces.TryGetValue(forceId, out var force)
            && force.LordCharacterId is int currentLordId && IsValidLord(currentLordId))
            return currentLordId;

        // 仅无有效运行时当主时兼容旧剧本。
        if (meta.ForceLordCharacterIds.TryGetValue(forceId, out var lordId)
            && IsValidLord(lordId))
        {
            return lordId;
        }

        return 0;

        bool IsValidLord(int id) => gameData.Characters.TryGetValue(id, out var character)
            && !character.IsDead && character.ForceId == forceId;
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

        // 业务：AI/他势力无当主角色时显示通用称谓
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

    /// <summary>是否势力当主名义居城（剧本固定，不随出访改变）。</summary>
    public static bool IsForceLordResidence(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            stronghold.ForceId,
            gameData,
            meta);
        return residenceId > 0 && stronghold.Id == residenceId;
    }

    /// <summary>
    /// 是否显示「居城」：当主居城，或已任命领主（LordId&gt;0）之据点。
    /// 其余 LordId=0 且非当主居城者为「直辖」。
    /// </summary>
    public static bool IsGovernanceResidence(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (IsForceLordResidence(stronghold, meta, gameData))
            return true;

        // 业务：已任命领主（LordId>0）的据点也视为「居城」
        return stronghold.LordId > 0;
    }

    /// <summary>将领主角色驻留于居城（LordId 对应据点）。</summary>
    public static void EnsureLordResidence(Stronghold stronghold, Character lord)
    {
        lord.ForceId = stronghold.ForceId;
        lord.StrongholdId = stronghold.Id;
        lord.Location = stronghold.Location;
        lord.LocationType = CharacterLocationType.Stronghold;
        lord.LocationStrongholdId = stronghold.Id;
    }
}
