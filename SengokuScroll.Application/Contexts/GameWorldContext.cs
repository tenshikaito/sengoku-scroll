using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Application.Contexts;

public class GameWorldContext(GameWorld gameWorld) : IGameWorldContext
{
    public GameWorld GameWorld { get; } = gameWorld;

    public GameMasterData GameMasterData => GameWorld.GameMasterData;

    public GameMapMasterData GameMapMasterData => GameWorld.GameMapMasterData;

    public Force? GetForce(IHasForce hasForce)
        => GameWorld.GameData.Forces.GetValueOrDefault(hasForce.ForceId);

    public TerrainDefinition? GetTerrainOrDefault(Point3 location)
    {
        var t = GameWorld.GameMapMasterData.TileMap.GetTerrain(location);

        return GameWorld.GameMapMasterData.Terrains.GetValueOrDefault(t);
    }

    public Stronghold? GetStrongholdOrDefault(Point2 location)
    {
        var i = GameWorld.GameMapMasterData.TileMap.GetIndex(location);

        var id = GameWorld.GameMapData.Strongholds.GetValueOrDefault(i);

        return GameWorld.GameData.Strongholds.GetValueOrDefault(id);
    }

    public Unit? GetUnitOrDefault(Point2 location)
    {
        var i = GameWorld.GameMapMasterData.TileMap.GetIndex(location);

        var id = GameWorld.GameMapData.Units.GetValueOrDefault(i);

        return GameWorld.GameData.Units.GetValueOrDefault(id);
    }

    public Character? GetCharacterOrDefault(int id)
        => GameWorld.GameData.Characters.GetValueOrDefault(id);

    public IEnumerable<Character> EachCharacter(bool isIncludeDead = false)
        => GameWorld.GameData.Characters.Values;

    public IEnumerable<Stronghold> EachStronghold()
        => GameWorld.GameData.Strongholds.Values;

    public IEnumerable<Force> EachForce()
        => GameWorld.GameData.Forces.Values;
    
    public IEnumerable<Unit> EachUnit()
        => GameWorld.GameData.Units.Values;
    
}
