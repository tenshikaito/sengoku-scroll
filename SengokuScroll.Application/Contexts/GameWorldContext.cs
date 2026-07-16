using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Application.Contexts;

/// <summary>世界数据访问：按格/Id 解析势力、地形、据点与地图单位。</summary>
public class GameWorldContext(GameWorld gameWorld) : IGameWorldContext
{
    public GameWorld GameWorld { get; } = gameWorld;

    public GameMasterData GameMasterData => GameWorld.GameMasterData;

    public GameMapMasterData GameMapMasterData => GameWorld.GameMapMasterData;

    /// <summary>解析实体所属势力（官府）。</summary>
    public Force? GetForce(IHasForce hasForce)
        => GameWorld.GameData.Forces.GetValueOrDefault(hasForce.ForceId);

    /// <summary>取格点地形定义；无则 null。</summary>
    public TerrainDefinition? GetTerrainOrDefault(Point3 location)
    {
        var t = GameWorld.GameMapMasterData.TileMap.GetTerrain(location);

        return GameWorld.GameMapMasterData.Terrains.GetValueOrDefault(t);
    }

    /// <summary>取格点上的据点（通过地图索引表）。</summary>
    public Stronghold? GetStrongholdOrDefault(Point2 location)
    {
        var i = GameWorld.GameMapMasterData.TileMap.GetIndex(location);

        var id = GameWorld.GameMapData.Strongholds.GetValueOrDefault(i);

        return GameWorld.GameData.Strongholds.GetValueOrDefault(id);
    }

    /// <summary>取格点上任一军事单位（堆叠时取列表首个，兼容旧调用）。</summary>
    public Unit? GetUnitOrDefault(Point2 location)
    {
        var units = GetUnitsAt(location);
        return units.Count > 0 ? units[0] : null;
    }

    /// <summary>取格点上全部军事单位。</summary>
    public IReadOnlyList<Unit> GetUnitsAt(Point2 location)
    {
        var i = GameWorld.GameMapMasterData.TileMap.GetIndex(location);
        if (!GameWorld.GameMapData.Units.TryGetValue(i, out var ids) || ids.Count == 0)
            return [];

        var result = new List<Unit>(ids.Count);
        foreach (var id in ids)
        {
            if (GameWorld.GameData.Units.TryGetValue(id, out var unit))
                result.Add(unit);
        }

        return result;
    }

    /// <summary>按 Id 取角色（武将/当主等）。</summary>
    public Character? GetCharacterOrDefault(int id)
        => GameWorld.GameData.Characters.GetValueOrDefault(id);

    /// <summary>遍历全部角色。</summary>
    public IEnumerable<Character> EachCharacter(bool isIncludeDead = false)
        => GameWorld.GameData.Characters.Values;

    /// <summary>遍历全部据点。</summary>
    public IEnumerable<Stronghold> EachStronghold()
        => GameWorld.GameData.Strongholds.Values;

    /// <summary>遍历全部势力。</summary>
    public IEnumerable<Force> EachForce()
        => GameWorld.GameData.Forces.Values;
    
    /// <summary>遍历全部地图军事单位。</summary>
    public IEnumerable<Unit> EachUnit()
        => GameWorld.GameData.Units.Values;
    
}
