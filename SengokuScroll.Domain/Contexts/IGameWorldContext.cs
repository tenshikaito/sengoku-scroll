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

    /// <summary>取格点上任一军事单位（兼容旧调用；堆叠时取列表首个）。</summary>
    Unit? GetUnitOrDefault(Point2 location);

    /// <summary>取格点上全部军事单位。</summary>
    IReadOnlyList<Unit> GetUnitsAt(Point2 location);

    Character? GetCharacterOrDefault(int characterId);

    IEnumerable<Character> EachCharacter(bool isIncludeDead = false);

    IEnumerable<Stronghold> EachStronghold();

    IEnumerable<Force> EachForce();

    IEnumerable<Unit> EachUnit();
}
