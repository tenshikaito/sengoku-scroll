using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 解析当主（势力君主）所在格：领兵时在部队格，否则在据点格。
/// </summary>
/// <remarks>
/// 业务用途：远程方针/战报信使的出发与送达参照点；玩家不可直接操控信使，但需知道当主位置。
/// </remarks>
public static class StrategyLordHelper
{
    public static Point3 ResolveLocation(GameData gameData, StrategyScenarioMeta meta)
    {
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
            .First()
            .Id;
    }

    public static Point3 ResolvePolicyIssuerLocation(Unit targetUnit, GameData gameData, StrategyScenarioMeta meta)
    {
        if (targetUnit.ForceId == meta.PlayerForceId)
            return ResolveLocation(gameData, meta);

        return targetUnit.Location;
    }

    /// <summary>解析势力当主居城据点 Id。</summary>
    public static int ResolveLordResidenceStrongholdId(
        int forceId,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (forceId == meta.PlayerForceId
            && meta.LordStrongholdId is int playerShId
            && gameData.Strongholds.ContainsKey(playerShId))
        {
            return playerShId;
        }

        var lordCharacterId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            forceId,
            meta,
            gameData);

        if (lordCharacterId > 0
            && gameData.Characters.TryGetValue(lordCharacterId, out var lord)
            && lord.StrongholdId > 0
            && gameData.Strongholds.ContainsKey(lord.StrongholdId))
        {
            return lord.StrongholdId;
        }

        return gameData.Strongholds.Values
            .Where(s => s.ForceId == forceId)
            .OrderBy(s => s.Id)
            .First()
            .Id;
    }
}
