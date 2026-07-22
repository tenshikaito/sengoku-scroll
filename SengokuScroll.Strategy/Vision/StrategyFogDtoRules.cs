using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

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
    {
        var options = meta.StartOptions;
        if (options.FogMode == StrategyFogMode.None)
            return unit.IsMilitary && unit.Soldier > 0 ? UnitFogPlacement.Map : UnitFogPlacement.Exclude;

        if (!unit.IsMilitary || unit.Soldier <= 0)
            return UnitFogPlacement.Exclude;

        var visible = visibility.VisibleCells.Contains((unit.Location.X, unit.Location.Y));
        if (IsDirectPlayerForce(unit.ForceId, meta.PlayerForceId))
            // 业务：自势力单位在迷雾外仍出现在 roster，进 visible 格才上地图
            return visible ? UnitFogPlacement.Map : UnitFogPlacement.Roster;

        if (!visible)
            return UnitFogPlacement.Exclude;

        return UnitFogPlacement.Map;
    }

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
    {
        var options = meta.StartOptions;
        if (options.FogMode == StrategyFogMode.None)
            return dto with { VisibilityTier = "Visible" };

        // 直接本家与内藩等封地：完整数值情报（与 EspionageIntelRules 自势力圈一致）。
        if (IsDirectPlayerForce(dto.ForceId, meta.PlayerForceId))
            return dto with { VisibilityTier = "Visible" };

        if (gameData is not null
            && IsOwnRealmForce(dto.ForceId, meta.PlayerForceId, gameData))
            return dto with { VisibilityTier = "Visible" };

        if (visibility.VisibleCells.Contains((dto.X, dto.Y)))
            return dto with { VisibilityTier = "Visible" };

        if (visibility.KnownStrongholdIds.Contains(dto.Id))
            return MaskAsKnownStronghold(dto);

        // 已探索但未在当前视野内：地图上保留据点与名称，具体数值情报隐藏。
        if (visibility.IsExplored(dto.X, dto.Y, mapWidth))
            return MaskAsKnownStronghold(dto);

        return null;
    }

    private static StrategyStrongholdStateDto MaskAsKnownStronghold(StrategyStrongholdStateDto dto)
        => dto with
        {
            VisibilityTier = "Known",
            Food = 0,
            Population = 0,
            GarrisonSoldiers = 0,
            GarrisonWounded = 0,
            Money = 0,
            Morale = 0,
            Training = 0,
            Defense = 0,
            DefenseFacilities = [],
            EconomyFacilities = [],
            SiegeThreat = null
        };

    public static bool IsMapEntityVisible(
        int x,
        int y,
        StrategyScenarioMeta meta,
        ForceVisibilityState visibility)
    {
        if (meta.StartOptions.FogMode == StrategyFogMode.None)
            return true;

        return visibility.VisibleCells.Contains((x, y));
    }

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
}
