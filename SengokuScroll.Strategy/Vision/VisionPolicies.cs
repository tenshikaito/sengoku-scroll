using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>无战争迷雾：全图可见，用于 Easy/调试。</summary>
public sealed class NoFogVisionPolicy : IVisionPolicy
{
    public HashSet<(int X, int Y)> ComputeVisibleTiles(
        GameWorld world,
        StrategyScenarioMeta meta,
        int observerForceId,
        GameStartOptions options)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var visible = new HashSet<(int X, int Y)>();
        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
                visible.Add((x, y));
        }

        return visible;
    }
}

/// <summary>
/// 势力视野：本藩据点 + 军事 Unit + 运输队 + 可选角色；玩家当主任何外出状态恒提供视野。
/// </summary>
public sealed class ForceVisionPolicy : IVisionPolicy
{
    public HashSet<(int X, int Y)> ComputeVisibleTiles(
        GameWorld world,
        StrategyScenarioMeta meta,
        int observerForceId,
        GameStartOptions options)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var visible = new HashSet<(int X, int Y)>();
        var data = world.GameData;
        var visionForceIds = StrategyVisionRules.EnumerateVisionForceIds(
                observerForceId,
                data,
                options.AllySharedVision)
            .ToHashSet();

        foreach (var stronghold in data.Strongholds.Values)
        {
            if (!visionForceIds.Contains(stronghold.ForceId))
                continue;

            StrategyVisionRules.AddSightBox(
                visible,
                stronghold.Location,
                StrategyStrongholdSightRanges.Default,
                tileMap.Width,
                tileMap.Height);
        }

        StrategyVisionRules.AddRealmUnitVision(
            visible,
            world,
            visionForceIds,
            observerForceId,
            data,
            tileMap.Width,
            tileMap.Height);

        StrategyVisionRules.AddRealmConvoyVision(
            visible,
            visionForceIds,
            observerForceId,
            data,
            tileMap.Width,
            tileMap.Height);

        StrategyVisionRules.AddRealmUnitEscortCarrierVision(
            visible,
            visionForceIds,
            observerForceId,
            data,
            tileMap.Width,
            tileMap.Height);

        StrategyVisionRules.AddForceModeCharacterVision(
            visible,
            world,
            meta,
            visionForceIds,
            data,
            options,
            tileMap.Width,
            tileMap.Height);

        return visible;
    }
}

/// <summary>
/// 角色视野（Normal/Hard）：以当主所在格为主视野源，内藩部队与运输队共享视野。
/// </summary>
public sealed class CharacterVisionPolicy : IVisionPolicy
{
    public HashSet<(int X, int Y)> ComputeVisibleTiles(
        GameWorld world,
        StrategyScenarioMeta meta,
        int observerForceId,
        GameStartOptions options)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var visible = new HashSet<(int X, int Y)>();
        var data = world.GameData;
        var lordLocation = StrategyLordHelper.ResolveLocation(data, meta);
        var sight = StrategyTroopSightRanges.Default;

        if (meta.LordUnitId is int lordUnitId
            && data.Units.TryGetValue(lordUnitId, out var lordUnit))
            sight = StrategyVisionRules.ResolveUnitSightRange(lordUnit, world);

        StrategyVisionRules.AddSightBox(
            visible,
            lordLocation,
            sight,
            tileMap.Width,
            tileMap.Height);

        foreach (var unit in data.Units.Values)
        {
            if (unit.ForceId == observerForceId)
                continue;

            if (!StrategyVisionRules.IsSameRealmForce(unit.ForceId, observerForceId, data))
                continue;

            if (!StrategyVisionRules.IsControllableVisionUnit(unit, observerForceId, data))
                continue;

            StrategyVisionRules.AddSightBox(
                visible,
                unit.Location,
                StrategyVisionRules.ResolveUnitSightRange(unit, world),
                tileMap.Width,
                tileMap.Height);
        }

        StrategyVisionRules.AddRealmConvoyVision(
            visible,
            StrategyVisionRules.EnumerateVisionForceIds(observerForceId, data, allySharedVision: false).ToHashSet(),
            observerForceId,
            data,
            tileMap.Width,
            tileMap.Height);

        return visible;
    }
}
