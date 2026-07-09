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
    /// <summary>移动军事单位并同步 <see cref="GameMapData.Units"/> 索引。</summary>
    public static void SetUnitLocation(IGameWorldContext context, Unit unit, Point3 newLocation)
        => SetEntityLocation(
            context.GameWorld,
            unit.Id,
            newLocation,
            unit.Location,
            loc => unit.Location = loc,
            world => world.GameMapData.Units);

    /// <summary>移动角色并同步 <see cref="GameMapData.Characters"/> 索引。</summary>
    public static void SetCharacterLocation(IGameWorldContext context, Character character, Point3 newLocation)
        => SetEntityLocation(
            context.GameWorld,
            character.Id,
            newLocation,
            character.Location,
            loc => character.Location = loc,
            world => world.GameMapData.Characters);

    /// <summary>移动据点并同步 <see cref="GameMapData.Strongholds"/> 索引。</summary>
    public static void SetStrongholdLocation(IGameWorldContext context, Stronghold stronghold, Point3 newLocation)
        => SetEntityLocation(
            context.GameWorld,
            stronghold.Id,
            newLocation,
            stronghold.Location,
            loc => stronghold.Location = loc,
            world => world.GameMapData.Strongholds);

    /// <summary>将军事单位登记到其当前坐标对应的格点索引（世界构建时调用）。</summary>
    public static void RegisterUnit(GameWorld world, Unit unit)
        => RegisterAtCurrentLocation(world, unit.Id, unit.Location, world.GameMapData.Units);

    /// <summary>将角色登记到其当前坐标对应的格点索引（世界构建时调用）。</summary>
    public static void RegisterCharacter(GameWorld world, Character character)
        => RegisterAtCurrentLocation(world, character.Id, character.Location, world.GameMapData.Characters);

    /// <summary>将据点登记到其当前坐标对应的格点索引（世界构建时调用）。</summary>
    public static void RegisterStronghold(GameWorld world, Stronghold stronghold)
        => RegisterAtCurrentLocation(world, stronghold.Id, stronghold.Location, world.GameMapData.Strongholds);

    private static void SetEntityLocation(
        GameWorld world,
        int entityId,
        Point3 newLocation,
        Point3 currentLocation,
        Action<Point3> setLocation,
        Func<GameWorld, Dictionary<int, int>> getIndex)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var index = getIndex(world);

        RemoveFromTileIndex(index, tileMap, currentLocation, entityId);
        setLocation(newLocation);
        AddToTileIndex(index, tileMap, newLocation, entityId);
    }

    private static void RegisterAtCurrentLocation(
        GameWorld world,
        int entityId,
        Point3 location,
        Dictionary<int, int> index)
        => AddToTileIndex(index, world.GameMapMasterData.TileMap, location, entityId);

    private static void RemoveFromTileIndex(
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

    private static void AddToTileIndex(
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
