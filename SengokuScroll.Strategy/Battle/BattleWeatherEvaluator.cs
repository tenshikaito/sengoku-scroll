using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Battle;

/// <summary>当日天气快照（ClimateSystem 实装前的区域+季节 hook）。</summary>
public readonly record struct BattleWeatherSnapshot(
    bool IsRainy,
    bool IsCold,
    bool IsHot,
    int AttackerWinRateDelta,
    int DefenderWinRateDelta,
    double ArcherMatchlockScale,
    string Label);

/// <summary>由日期、区域气候推导的野战天气修正。</summary>
public static class BattleWeatherEvaluator
{
    /// <summary>由日期与区域气候推导当日野战天气，返回胜率与远程衰减修正。</summary>
    public static BattleWeatherSnapshot Evaluate(GameDate date, GameMapMasterData? mapMaster, Point3 location)
    {
        var month = date.Month;
        var region = ResolveRegion(mapMaster, location);

        // 业务：梅雨/台风季、高暴雨率区域判定雨天
        var isRainy = month is 6 or 7 or 8 || region?.StormRate >= 40 || region?.FloodRate >= 35;
        // 业务：冬季与高寒潮率区域判定严寒
        var isCold = month is 11 or 12 or 1 or 2 || region?.ColdWaveRate >= 40 || region?.SnowstormRate >= 35;
        // 业务：盛夏与高干旱率区域判定酷暑
        var isHot = month is 7 or 8 || region?.DroughtRate >= 45;

        var atkDelta = 0;
        var defDelta = 0;
        var rangedScale = 1.0;
        var labelParts = new List<string>();

        if (isRainy)
        {
            // 业务：雨天远程 ×0.88，双方胜率略降
            rangedScale *= 0.88;
            atkDelta -= 2;
            defDelta -= 1;
            labelParts.Add("雨");
        }

        if (isCold)
        {
            atkDelta -= 3;
            defDelta -= 2;
            labelParts.Add("寒");
        }

        if (isHot)
        {
            atkDelta -= 1;
            labelParts.Add("暑");
        }

        if (region is not null && labelParts.Count == 0)
            labelParts.Add(region.Name);

        var label = labelParts.Count == 0 ? "晴" : string.Join("·", labelParts);
        return new BattleWeatherSnapshot(isRainy, isCold, isHot, atkDelta, defDelta, rangedScale, label);
    }

    private static RegionDefinition? ResolveRegion(GameMapMasterData? mapMaster, Point3 location)
    {
        if (mapMaster is null)
            return null;

        if (mapMaster.TileMap.IsOutOfBounds(location))
            return null;

        var regionId = mapMaster.TileMap.GetRegion(location);
        return regionId > 0 ? mapMaster.Regions.GetValueOrDefault(regionId) : null;
    }
}
