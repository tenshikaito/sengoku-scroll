using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Character;

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
/// 势力视野：本藩据点 + 可控部队/地图角色曼哈顿菱形视野；可选同盟共享视野。
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

        foreach (var unit in data.Units.Values)
        {
            if (!visionForceIds.Contains(unit.ForceId))
                continue;

            if (!StrategyVisionRules.IsControllableVisionUnit(unit, observerForceId, data))
                continue;

            var sight = StrategyVisionRules.ResolveUnitSightRange(unit, world);
            StrategyVisionRules.AddSightBox(
                visible,
                unit.Location,
                sight,
                tileMap.Width,
                tileMap.Height);
        }

        foreach (var character in data.Characters.Values)
        {
            if (!visionForceIds.Contains(character.ForceId))
                continue;

            if (!StrategyVisionRules.IsControllableVisionCharacter(character, observerForceId, data))
                continue;

            var location = ResolveCharacterMapLocation(character, data);
            StrategyVisionRules.AddSightBox(
                visible,
                location,
                StrategyTroopSightRanges.Default,
                tileMap.Width,
                tileMap.Height);
        }

        return visible;
    }

    private static Common.Types.Point3 ResolveCharacterMapLocation(Character character, GameData data)
    {
        // 业务：将领编入部队时，视野中心跟随部队格而非角色抽象坐标
        if (character.LocationType == CharacterLocationType.Unit)
        {
            var unit = data.Units.Values.FirstOrDefault(u =>
                u.LeaderId == character.Id
                || u.SubUnitIds.Any(id =>
                    data.SubUnits.TryGetValue(id, out var sub) && sub.LeaderId == character.Id));

            if (unit is not null)
                return unit.Location;
        }

        return character.Location;
    }
}

/// <summary>
/// 角色视野（Normal/Hard）：以当主所在格为主视野源，内藩部队共享视野但不替代当主主格。
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

        // 内藩等单位：共享部队视野；本家部队仍仅当主格提供（见上方 lordLocation）。
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

        return visible;
    }
}
