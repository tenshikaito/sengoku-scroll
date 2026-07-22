using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Vision;

/// <summary>战争迷雾下的 DTO 过滤与据点 tier。</summary>
public static class StrategyFogDtoRules
{
    public enum UnitFogPlacement
    {
        /// <summary>不进入 DTO（完全未知）。</summary>
        Exclude,
        /// <summary>地图上显示（可见格或自势力）。</summary>
        Map,
        /// <summary>仅出现在可操作列表，地图不画单位（自势力在迷雾外）。</summary>
        Roster
    }

    /// <summary>是否为玩家直接操控的本家势力（非内藩）。</summary>
    public static bool IsDirectPlayerForce(int forceId, int playerForceId) => forceId == playerForceId;

    /// <summary>判定单位在迷雾 DTO 中的展示档位。</summary>
    public static UnitFogPlacement ClassifyUnit(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility)
        => ResolveFog(meta).ClassifyUnit(unit, meta, gameData, visibility);

    public static bool ShouldIncludeUnit(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility,
        out bool mapVisible)
    {
        var placement = ClassifyUnit(unit, meta, gameData, visibility);
        mapVisible = placement == UnitFogPlacement.Map;
        return placement != UnitFogPlacement.Exclude;
    }

    public static StrategyStrongholdStateDto? ApplyStrongholdFog(
        StrategyStrongholdStateDto dto,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility,
        int mapWidth)
        => ResolveFog(meta).ApplyStrongholdFog(dto, meta, gameData, visibility, mapWidth);

    public static bool IsMapEntityVisible(
        int x,
        int y,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility)
        => ResolveFog(meta).IsMapEntityVisible(x, y, meta, visibility);

    /// <summary>运输队等地图实体：仅在当前视野格显示。</summary>
    public static bool IsMapMobileEntityVisible(
        int x,
        int y,
        int forceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        ForceVisibilityState visibility)
    {
        _ = forceId;
        _ = gameData;
        return IsMapEntityVisible(x, y, meta, visibility);
    }

    /// <summary>文书载体是否在地图上显示（仅亮格；与单位动态态势规则一致）。</summary>
    public static bool IsMessageCarrierMapVisible(
        MessageCarrier carrier,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility)
        => IsMapEntityVisible(carrier.Location.X, carrier.Location.Y, meta, visibility);

    public static bool IsOwnRealmForce(int forceId, int playerForceId, GameData gameData)
        => TributeRoutingHelper.ResolveRealmRootForceId(forceId, gameData)
           == TributeRoutingHelper.ResolveRealmRootForceId(playerForceId, gameData);

    public static GameStartOptionsDto ToOptionsDto(GameStartOptions options)
        => GameStartOptionsMapper.ToDto(options);

    private static IFogModeBehavior ResolveFog(StrategyScenarioMeta meta)
        => GameStartOptionsProfile.Create(meta.StartOptions, meta.Difficulty).Fog;
}
