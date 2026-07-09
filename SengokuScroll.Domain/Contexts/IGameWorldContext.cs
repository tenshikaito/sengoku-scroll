using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Contexts;

public interface IGameWorldContext
{
    GameWorld GameWorld { get; }

    GameMasterData GameMasterData => GameWorld.GameMasterData;

    GameMapMasterData GameMapMasterData => GameWorld.GameMapMasterData;

    Force? GetForce(IHasForce hasForce);

    TerrainDefinition? GetTerrainOrDefault(Point3 location);

    Stronghold? GetStrongholdOrDefault(Point2 location);

    Unit? GetUnitOrDefault(Point2 location);

    Character? GetCharacterOrDefault(int characterId);

    IEnumerable<Character> EachCharacter(bool isIncludeDead = false);

    IEnumerable<Stronghold> EachStronghold();

    IEnumerable<Force> EachForce();

    IEnumerable<Unit> EachUnit();
}
