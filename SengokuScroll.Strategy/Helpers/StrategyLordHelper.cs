using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 解析当主（势力君主）所在格：领兵时在部队格，否则在据点格。
/// </summary>
/// <remarks>
/// 业务用途：远程方针/战报信使的出发与送达参照点；玩家不可直接操控信使，但需知道当主位置。
/// </remarks>
public static class StrategyLordHelper
{
    /// <summary>当主当前所在格：优先跟随当主角色状态，其次剧本绑定的部队/据点。</summary>
    public static Point3 ResolveLocation(GameData gameData, StrategyScenarioMeta meta)
    {
        if (TryResolveLordCharacterLocation(gameData, meta, out var fromCharacter))
            return fromCharacter;

        if (meta.LordUnitId is int unitId
            && gameData.Units.TryGetValue(unitId, out var lordUnit))
            return lordUnit.Location;

        if (meta.LordStrongholdId is int strongholdId
            && gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
            return stronghold.Location;

        var fallback = gameData.Strongholds.Values
            .Where(s => s.ForceId == meta.PlayerForceId)
            .OrderBy(s => s.Id)
            .FirstOrDefault();

        return fallback?.Location ?? new Point3(0, 0);
    }

    private static bool TryResolveLordCharacterLocation(
        GameData gameData,
        StrategyScenarioMeta meta,
        out Point3 location)
    {
        location = default;
        var lordCharacterId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        if (lordCharacterId <= 0
            || !gameData.Characters.TryGetValue(lordCharacterId, out var lordCharacter))
        {
            return false;
        }

        if (lordCharacter.LocationType == CharacterLocationType.Unit)
        {
            var ledUnit = gameData.Units.Values.FirstOrDefault(u => u.LeaderId == lordCharacter.Id);
            if (ledUnit is not null)
            {
                location = ledUnit.Location;
                return true;
            }

            // 业务：部队已不在地图时，仍用将领当前格（溃逃/回城途中），避免误回落居城
            location = lordCharacter.Location;
            return true;
        }

        if (lordCharacter.LocationType == CharacterLocationType.Stronghold)
        {
            var strongholdId = lordCharacter.LocationStrongholdId > 0
                ? lordCharacter.LocationStrongholdId
                : lordCharacter.StrongholdId;
            if (strongholdId > 0
                && gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
            {
                location = stronghold.Location;
                return true;
            }
        }

        if (lordCharacter.LocationType == CharacterLocationType.Map)
        {
            location = lordCharacter.Location;
            return true;
        }

        return false;
    }

    /// <summary>信使出发据点 Id：优先取当主所在格据点，否则取势力首个据点。</summary>
    public static int ResolveSourceStrongholdId(GameData gameData, StrategyScenarioMeta meta, Point3 issuer)
    {
        var atIssuer = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == meta.PlayerForceId
            && s.Location.X == issuer.X
            && s.Location.Y == issuer.Y);
        if (atIssuer is not null)
            return atIssuer.Id;

        return gameData.Strongholds.Values
            .Where(s => s.ForceId == meta.PlayerForceId)
            .OrderBy(s => s.Id)
            .FirstOrDefault()?.Id ?? 0;
    }

    /// <summary>方针指令下达方所在格：玩家势力取当主位置，AI 取目标单位当前格。</summary>
    public static Point3 ResolvePolicyIssuerLocation(Unit targetUnit, GameData gameData, StrategyScenarioMeta meta)
    {
        if (targetUnit.ForceId == meta.PlayerForceId)
            return ResolveLocation(gameData, meta);

        return targetUnit.Location;
    }

    /// <summary>解析势力当主名义居城据点 Id（剧本固定；不随当主出访其它据点而改变）。</summary>
    public static int ResolveLordResidenceStrongholdId(
        int forceId,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (OrganizationForceHelper.IsOrganizationForceId(gameData, forceId))
        {
            var headquarters = OrganizationForceHelper.EnumerateShops(gameData, forceId)
                .OrderBy(shop => shop.StrongholdId)
                .ThenBy(shop => shop.Id)
                .FirstOrDefault();
            if (headquarters != null)
                return headquarters.StrongholdId;
        }

        if (meta.ForceLordResidenceStrongholdIds.TryGetValue(forceId, out var residenceId)
            && residenceId > 0
            && gameData.Strongholds.TryGetValue(residenceId, out var residence)
            && residence.ForceId == forceId)
        {
            return residenceId;
        }

        if (forceId == meta.PlayerForceId
            && meta.LordStrongholdId is int playerResidence
            && gameData.Strongholds.TryGetValue(playerResidence, out var playerStronghold)
            && playerStronghold.ForceId == forceId)
        {
            return playerResidence;
        }

        var lordCharacterId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            forceId,
            meta,
            gameData);

        if (lordCharacterId <= 0
            || !gameData.Characters.TryGetValue(lordCharacterId, out var lord)
            || lord.StrongholdId <= 0
            || !gameData.Strongholds.ContainsKey(lord.StrongholdId))
        {
            return 0;
        }

        return lord.StrongholdId;
    }
}
