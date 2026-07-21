using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Vision;

/// <summary>视野半径、高度与天气修正。</summary>
public static class StrategyVisionRules
{
    public const int HeightAdvantageThreshold = 100;

    /// <summary>曼哈顿菱形视野：|dx| + |dy| ≤ range。</summary>
    public static void AddSightBox(
        HashSet<(int X, int Y)> visible,
        Point3 center,
        int range,
        int width,
        int height)
    {
        range = Math.Max(0, range);
        for (var dy = -range; dy <= range; dy++)
        {
            for (var dx = -range; dx <= range; dx++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) > range)
                    continue;

                var x = center.X + dx;
                var y = center.Y + dy;
                if (x < 0 || y < 0 || x >= width || y >= height)
                    continue;

                visible.Add((x, y));
            }
        }
    }

    [Obsolete("Use AddSightBox.")]
    public static void AddSightDisc(
        HashSet<(int X, int Y)> visible,
        Point3 center,
        int radius,
        int width,
        int height)
        => AddSightBox(visible, center, radius, width, height);

    /// <summary>取子编制类型视野的最大值作为部队视野半径。</summary>
    public static int ResolveUnitSightRange(Unit unit, GameWorld world)
    {
        var maxRange = StrategyTroopSightRanges.Default;
        foreach (var subUnitId in unit.SubUnitIds)
        {
            if (!world.GameData.SubUnits.TryGetValue(subUnitId, out var subUnit))
                continue;

            maxRange = Math.Max(maxRange, StrategyTroopSightRanges.Resolve(subUnit.TypeId));
        }

        return maxRange;
    }

    /// <summary>读取格点地形高度（用于高差情报优势判定）。</summary>
    public static int ResolveTerrainAltitude(GameWorld world, int x, int y)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        if (x < 0 || y < 0 || x >= tileMap.Width || y >= tileMap.Height)
            return 0;

        var terrainId = (int)tileMap.GetTerrain(new Point3(x, y));
        if (!world.GameMapMasterData.Terrains.TryGetValue(terrainId, out var terrain))
            return 0;

        return terrain.Altitude;
    }

    /// <summary>友方可见源格最高海拔比目标高 ≥100 时，视为高差情报优势（兵数可部分揭示）。</summary>
    public static bool HasHeightIntelAdvantage(
        GameWorld world,
        HashSet<(int X, int Y)> friendlyVisibleSources,
        int targetX,
        int targetY)
    {
        var targetAlt = ResolveTerrainAltitude(world, targetX, targetY);
        var bestObserverAlt = 0;

        foreach (var (x, y) in friendlyVisibleSources)
            bestObserverAlt = Math.Max(bestObserverAlt, ResolveTerrainAltitude(world, x, y));

        return bestObserverAlt >= targetAlt + HeightAdvantageThreshold;
    }

    /// <summary>枚举参与视野计算的势力 Id：本藩 + 可选同盟。</summary>
    public static IEnumerable<int> EnumerateVisionForceIds(int playerForceId, GameData gameData, bool allySharedVision)
    {
        var realmRoot = TributeRoutingHelper.ResolveRealmRootForceId(playerForceId, gameData);
        var yielded = new HashSet<int>();

        foreach (var force in gameData.Forces.Values)
        {
            if (TributeRoutingHelper.ResolveRealmRootForceId(force.Id, gameData) != realmRoot)
                continue;

            if (yielded.Add(force.Id))
                yield return force.Id;
        }

        if (!allySharedVision)
            yield break;

        foreach (var force in gameData.Forces.Values)
        {
            if (yielded.Contains(force.Id))
                continue;

            if (IsAllied(playerForceId, force.Id, gameData) && yielded.Add(force.Id))
                yield return force.Id;
        }
    }

    /// <summary>两势力是否同属一个贡纳/Realm 根势力（内藩共享规则）。</summary>
    public static bool IsSameRealmForce(int forceId, int playerForceId, GameData gameData)
        => TributeRoutingHelper.ResolveRealmRootForceId(forceId, gameData)
           == TributeRoutingHelper.ResolveRealmRootForceId(playerForceId, gameData);

    /// <summary>部队是否可作为视野源：有兵、同 Realm、主将非俘虏。</summary>
    public static bool IsControllableVisionUnit(Unit unit, int playerForceId, GameData gameData)
    {
        if (!unit.IsMilitary || unit.Soldier <= 0)
            return false;

        if (!IsSameRealmForce(unit.ForceId, playerForceId, gameData))
            return false;

        if (unit.LeaderId > 0
            && gameData.Characters.TryGetValue(unit.LeaderId, out var leader)
            && leader.ForceStatus == CharacterForceStatus.Prisoner)
            return false;

        return true;
    }

    /// <summary>地图/编入部队的角色是否提供视野（俘虏与城内角色除外）。</summary>
    public static bool IsControllableVisionCharacter(Character character, int playerForceId, GameData gameData)
    {
        if (character.ForceStatus == CharacterForceStatus.Prisoner)
            return false;

        if (character.LocationType != CharacterLocationType.Map
            && character.LocationType != CharacterLocationType.Unit)
            return false;

        return IsSameRealmForce(character.ForceId, playerForceId, gameData);
    }

    private static bool IsAllied(int playerForceId, int otherForceId, GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(playerForceId, out var playerForce))
            return false;

        return playerForce.Diplomacies.Any(d =>
            d.TargetForceId == otherForceId
            && d.Relation == Diplomacy.DiplomacyRelation.Allied);
    }
}
