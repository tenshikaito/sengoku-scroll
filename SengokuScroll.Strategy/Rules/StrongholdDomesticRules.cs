using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>据点内政指令（税率等）校验。</summary>
public static class StrongholdDomesticRules
{
    /// <summary>玩家本家据点（非内藩势力据点）。</summary>
    public static bool IsPlayerRealmStronghold(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (stronghold.ForceId != meta.PlayerForceId)
            return false;

        if (!gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
            return false;

        return force.Status != Force.ForceStatus.InnerVassal;
    }

    /// <summary>当主可否调整税率：仅直辖城（LordId=0）；已任命领主领地由领主自决。</summary>
    public static bool CanPlayerAdjustTaxRates(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
        => IsPlayerRealmStronghold(stronghold, meta, gameData)
           && StrategyStrongholdLordHelper.IsDirectRule(stronghold);

    /// <summary>当主角色是否驻留于本家居城据点内。</summary>
    public static bool IsLordAtResidence(StrategyScenarioMeta meta, GameData gameData)
    {
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            meta.PlayerForceId,
            gameData,
            meta);
        if (residenceId <= 0
            || !gameData.Strongholds.TryGetValue(residenceId, out var residence))
        {
            return false;
        }

        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        if (lordId <= 0 || !gameData.Characters.TryGetValue(lordId, out var lord))
            return false;

        if (lord.LocationType == CharacterLocationType.Stronghold)
        {
            var atId = lord.LocationStrongholdId > 0 ? lord.LocationStrongholdId : lord.StrongholdId;
            if (atId == residenceId)
                return true;
        }

        if (lord.LocationType == CharacterLocationType.Unit)
        {
            var ledUnit = gameData.Units.Values.FirstOrDefault(u => u.LeaderId == lord.Id);
            if (ledUnit is not null
                && ledUnit.Location.X == residence.Location.X
                && ledUnit.Location.Y == residence.Location.Y)
            {
                return true;
            }
        }

        // 业务：与居城同格（在城 / 地图 / 领兵）均视为驻留居城
        return lord.Location.X == residence.Location.X
               && lord.Location.Y == residence.Location.Y;
    }

    /// <summary>当主是否位于指定据点格（在城 / 地图同格 / 领兵同格）。</summary>
    public static bool IsLordPresentAtStronghold(
        StrategyScenarioMeta meta,
        GameData gameData,
        Stronghold stronghold)
    {
        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        if (lordId <= 0 || !gameData.Characters.TryGetValue(lordId, out var lord))
            return false;

        var loc = stronghold.Location;

        if (lord.LocationType == CharacterLocationType.Stronghold)
        {
            var atId = lord.LocationStrongholdId > 0 ? lord.LocationStrongholdId : lord.StrongholdId;
            return atId == stronghold.Id;
        }

        if (lord.LocationType == CharacterLocationType.Unit)
        {
            var ledUnit = gameData.Units.Values.FirstOrDefault(u => u.LeaderId == lord.Id);
            return ledUnit is not null && ledUnit.Location.IsSameTile(loc);
        }

        return lord.Location.X == loc.X && lord.Location.Y == loc.Y;
    }

    /// <summary>当主可否对该据点下达内政/军事指令：驻居城可遥控本家全境，否则须亲赴该据点格。</summary>
    public static bool CanLordCommandAtStronghold(
        StrategyScenarioMeta meta,
        GameData gameData,
        Stronghold stronghold)
    {
        if (!IsPlayerRealmStronghold(stronghold, meta, gameData))
            return false;

        if (IsLordAtResidence(meta, gameData))
            return true;

        return IsLordPresentAtStronghold(meta, gameData, stronghold);
    }

    /// <summary>直辖据点税率变更须经在途载体自居城传达（同格除外）。</summary>
    public static bool RequiresInTransitDeliveryForTaxChange(
        Point3 issuerLocation,
        Stronghold target)
        => issuerLocation.X != target.Location.X || issuerLocation.Y != target.Location.Y;
}
