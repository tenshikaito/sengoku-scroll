using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Domain.Actions;

/// <summary>
/// 地图实体坐标与 <see cref="GameMapData"/> 格点索引的同步变更。
/// 所有会改变 Unit/Character/Stronghold 位置的代码路径应经本类，避免索引与实体脱节。
/// </summary>
public static class MapLocationActions
{
    /// <summary>移动军事单位并同步 <see cref="GameMapData.Units"/> 多单位列表索引。</summary>
    public static void SetUnitLocation(IGameWorldContext context, Unit unit, Point3 newLocation)
    {
        if (unit.InStronghold)
        {
            unit.Location = newLocation;
            return;
        }

        var world = context.GameWorld;
        var tileMap = world.GameMapMasterData.TileMap;
        var index = world.GameMapData.Units;

        RemoveUnitFromTileIndex(index, tileMap, unit.Location, unit.Id);
        unit.Location = newLocation;
        AddUnitToTileIndex(index, tileMap, newLocation, unit.Id);
    }

    /// <summary>移动角色并同步 <see cref="GameMapData.Characters"/> 索引。</summary>
    public static void SetCharacterLocation(IGameWorldContext context, Character character, Point3 newLocation)
        => SetSingleEntityLocation(
            context.GameWorld,
            character.Id,
            newLocation,
            character.Location,
            loc => character.Location = loc,
            world => world.GameMapData.Characters);

    /// <summary>移动据点并同步 <see cref="GameMapData.Strongholds"/> 索引。</summary>
    public static void SetStrongholdLocation(IGameWorldContext context, Stronghold stronghold, Point3 newLocation)
        => SetSingleEntityLocation(
            context.GameWorld,
            stronghold.Id,
            newLocation,
            stronghold.Location,
            loc => stronghold.Location = loc,
            world => world.GameMapData.Strongholds);

    /// <summary>将军事单位登记到其当前坐标对应的格点列表（世界构建时调用）。</summary>
    public static void RegisterUnit(GameWorld world, Unit unit)
    {
        if (unit.InStronghold)
            return;

        AddUnitToTileIndex(
            world.GameMapData.Units,
            world.GameMapMasterData.TileMap,
            unit.Location,
            unit.Id);
    }

    /// <summary>将城内单位登记到地图索引（出城时调用）。</summary>
    public static void RegisterUnitOnMap(IGameWorldContext context, Unit unit)
        => AddUnitToTileIndex(
            context.GameWorld.GameMapData.Units,
            context.GameWorld.GameMapMasterData.TileMap,
            unit.Location,
            unit.Id);

    /// <summary>将单位从地图格索引移除（入城时调用；不销毁实体）。</summary>
    public static void UnregisterUnitFromMap(IGameWorldContext context, Unit unit)
        => RemoveUnitFromTileIndex(
            context.GameWorld.GameMapData.Units,
            context.GameWorld.GameMapMasterData.TileMap,
            unit.Location,
            unit.Id);

    /// <summary>从地图与数据中移除军事单位（溃灭/解散）。</summary>
    public static void RemoveUnit(IGameWorldContext context, Unit unit)
    {
        var world = context.GameWorld;
        if (!unit.InStronghold)
        {
            RemoveUnitFromTileIndex(
                world.GameMapData.Units,
                world.GameMapMasterData.TileMap,
                unit.Location,
                unit.Id);
        }

        world.GameData.Units.Remove(unit.Id);
    }

    /// <summary>将角色登记到其当前坐标对应的格点索引（世界构建时调用）。</summary>
    public static void RegisterCharacter(GameWorld world, Character character)
        => AddSingleToTileIndex(
            world.GameMapData.Characters,
            world.GameMapMasterData.TileMap,
            character.Location,
            character.Id);

    /// <summary>将据点登记到其当前坐标对应的格点索引（世界构建时调用）。</summary>
    public static void RegisterStronghold(GameWorld world, Stronghold stronghold)
        => AddSingleToTileIndex(
            world.GameMapData.Strongholds,
            world.GameMapMasterData.TileMap,
            stronghold.Location,
            stronghold.Id);

    private static void SetSingleEntityLocation(
        GameWorld world,
        int entityId,
        Point3 newLocation,
        Point3 currentLocation,
        Action<Point3> setLocation,
        Func<GameWorld, Dictionary<int, int>> getIndex)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var index = getIndex(world);

        RemoveSingleFromTileIndex(index, tileMap, currentLocation, entityId);
        setLocation(newLocation);
        AddSingleToTileIndex(index, tileMap, newLocation, entityId);
    }

    private static void RemoveUnitFromTileIndex(
        Dictionary<int, List<int>> index,
        TileMap tileMap,
        Point3 location,
        int entityId)
    {
        if (tileMap.IsOutOfBounds(location))
            return;

        var tileIndex = tileMap.GetIndex(location);
        if (!index.TryGetValue(tileIndex, out var list))
            return;

        list.Remove(entityId);
        if (list.Count == 0)
            index.Remove(tileIndex);
    }

    private static void AddUnitToTileIndex(
        Dictionary<int, List<int>> index,
        TileMap tileMap,
        Point3 location,
        int entityId)
    {
        if (tileMap.IsOutOfBounds(location))
            throw new ArgumentOutOfRangeException(nameof(location), $"坐标 {location} 超出地图边界。");

        var tileIndex = tileMap.GetIndex(location);
        if (!index.TryGetValue(tileIndex, out var list))
        {
            list = [];
            index[tileIndex] = list;
        }

        if (!list.Contains(entityId))
            list.Add(entityId);
    }

    private static void RemoveSingleFromTileIndex(
        Dictionary<int, int> index,
        TileMap tileMap,
        Point3 location,
        int entityId)
    {
        if (tileMap.IsOutOfBounds(location))
            return;

        var tileIndex = tileMap.GetIndex(location);
        if (index.TryGetValue(tileIndex, out var existingId) && existingId == entityId)
            index.Remove(tileIndex);
    }

    private static void AddSingleToTileIndex(
        Dictionary<int, int> index,
        TileMap tileMap,
        Point3 location,
        int entityId)
    {
        if (tileMap.IsOutOfBounds(location))
            throw new ArgumentOutOfRangeException(nameof(location), $"坐标 {location} 超出地图边界。");

        index[tileMap.GetIndex(location)] = entityId;
    }
}
