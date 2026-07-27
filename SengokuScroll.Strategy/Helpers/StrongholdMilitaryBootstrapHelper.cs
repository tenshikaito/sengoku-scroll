using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点驻军：农兵池与驻城专业 SubUnit 初始化、出征编组。</summary>
public static class StrongholdMilitaryBootstrapHelper
{
    /// <summary>将部分城内兵数拆为驻城 SubUnit（专业队 + 清洲双足轻队）。</summary>
    public static void InitializeGarrisonComposition(Stronghold stronghold, GameData gameData)
    {
        var forceActor = stronghold.ForceActor;
        if (forceActor.SubUnitIds.Count > 0 || forceActor.Soldier <= 0)
            return;

        forceActor.Matchlock = Math.Max(forceActor.Matchlock, Math.Max(10, stronghold.Population / 120));

        var total = forceActor.Soldier;
        var cavalry = total / 8;
        var matchlock = Math.Min(total / 12, forceActor.Matchlock);
        var archers = Math.Min(total / 10, Math.Max(0, total - cavalry - matchlock) / 2);
        var professional = cavalry + matchlock + archers;
        var nextId = gameData.SubUnits.Keys.DefaultIfEmpty(0).Max() + 1;

        if (professional > 0)
        {
            forceActor.Soldier = total - professional;

            if (cavalry > 0)
                nextId = AddGarrisonSubUnit(gameData, stronghold, forceActor, nextId, StrategyTroopTypes.Cavalry, cavalry, "骑兵");

            if (matchlock > 0)
                nextId = AddGarrisonSubUnit(gameData, stronghold, forceActor, nextId, StrategyTroopTypes.Matchlock, matchlock, "铁炮");

            if (archers > 0)
                nextId = AddGarrisonSubUnit(gameData, stronghold, forceActor, nextId, StrategyTroopTypes.Archer, archers, "弓兵");
        }

        SplitAshigaruMilitiaTeams(stronghold, gameData, ref nextId);
    }

    /// <summary>清洲：将剩余农兵拆为两个足轻 SubUnit。</summary>
    private static void SplitAshigaruMilitiaTeams(Stronghold stronghold, GameData gameData, ref int nextId)
    {
        if (stronghold.Id != 1)
            return;

        var forceActor = stronghold.ForceActor;
        if (forceActor.Soldier <= 0)
            return;

        var militia = forceActor.Soldier;
        forceActor.Soldier = 0;
        var first = militia / 2;
        var second = militia - first;

        if (first > 0)
            nextId = AddGarrisonSubUnit(
                gameData,
                stronghold,
                forceActor,
                nextId,
                StrategyTroopTypes.Ashigaru,
                first,
                "足轻一");

        if (second > 0)
            _ = AddGarrisonSubUnit(
                gameData,
                stronghold,
                forceActor,
                nextId,
                StrategyTroopTypes.Ashigaru,
                second,
                "足轻二");
    }

    public static IReadOnlyList<GarrisonTroopPoolEntry> ListGarrisonTroopPools(
        Stronghold stronghold,
        GameData gameData)
    {
        var pools = new Dictionary<int, int>
        {
            [StrategyTroopTypes.Ashigaru] = Math.Max(0, stronghold.ForceActor.Soldier)
        };

        foreach (var subId in stronghold.ForceActor.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.UnitId != 0)
                continue;

            pools[sub.TypeId] = pools.GetValueOrDefault(sub.TypeId) + Math.Max(0, sub.Soldier);
        }

        return pools
            .Where(p => p.Value > 0)
            .Select(p => new GarrisonTroopPoolEntry(p.Key, StrategyTroopTypes.ResolveName(p.Key, null), p.Value))
            .OrderBy(p => p.TypeId)
            .ToList();
    }

    public static bool TryAllocateGarrisonTroops(
        Stronghold stronghold,
        GameData gameData,
        int typeId,
        int soldiers,
        int unitId,
        IList<SubUnit> allocated)
    {
        if (soldiers <= 0)
            return true;

        if (typeId == StrategyTroopTypes.Ashigaru)
            return TryAllocateAshigaruTroops(stronghold, gameData, soldiers, unitId, allocated);

        var remaining = soldiers;
        foreach (var subId in stronghold.ForceActor.SubUnitIds.ToList())
        {
            if (remaining <= 0)
                break;

            if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.UnitId != 0 || sub.TypeId != typeId)
                continue;

            remaining -= TakeFromGarrisonSubUnit(stronghold, gameData, sub, remaining, unitId, allocated);
        }

        return remaining <= 0;
    }

    private static bool TryAllocateAshigaruTroops(
        Stronghold stronghold,
        GameData gameData,
        int soldiers,
        int unitId,
        IList<SubUnit> allocated)
    {
        var remaining = soldiers;
        var fromPool = Math.Min(remaining, stronghold.ForceActor.Soldier);
        if (fromPool > 0)
        {
            stronghold.ForceActor.Soldier -= fromPool;
            allocated.Add(CreateSubUnit(gameData, stronghold, unitId, StrategyTroopTypes.Ashigaru, fromPool, stronghold.ForceActor));
            remaining -= fromPool;
        }

        foreach (var subId in stronghold.ForceActor.SubUnitIds.ToList())
        {
            if (remaining <= 0)
                break;

            if (!gameData.SubUnits.TryGetValue(subId, out var sub)
                || sub.UnitId != 0
                || sub.TypeId != StrategyTroopTypes.Ashigaru)
                continue;

            remaining -= TakeFromGarrisonSubUnit(stronghold, gameData, sub, remaining, unitId, allocated);
        }

        return remaining <= 0;
    }

    private static int TakeFromGarrisonSubUnit(
        Stronghold stronghold,
        GameData gameData,
        SubUnit sub,
        int requested,
        int unitId,
        IList<SubUnit> allocated)
    {
        var take = Math.Min(requested, sub.Soldier);
        if (take <= 0)
            return 0;

        if (take == sub.Soldier)
        {
            sub.UnitId = unitId;
            stronghold.ForceActor.SubUnitIds.Remove(sub.Id);
            allocated.Add(sub);
        }
        else
        {
            sub.Soldier -= take;
            allocated.Add(CreateSubUnit(gameData, stronghold, unitId, sub.TypeId, take, stronghold.ForceActor, sub));
        }

        return take;
    }

    public static void ReturnSubUnitsToGarrison(Stronghold stronghold, GameData gameData, IEnumerable<SubUnit> subs)
    {
        foreach (var sub in subs)
        {
            if (sub.Soldier <= 0)
                continue;

            if (sub.TypeId == StrategyTroopTypes.Ashigaru && string.IsNullOrWhiteSpace(sub.UnitName))
            {
                stronghold.ForceActor.Soldier += sub.Soldier;
                gameData.SubUnits.Remove(sub.Id);
                continue;
            }

            sub.UnitId = 0;
            if (!stronghold.ForceActor.SubUnitIds.Contains(sub.Id))
                stronghold.ForceActor.SubUnitIds.Add(sub.Id);
        }
    }

    private static int AddGarrisonSubUnit(
        GameData gameData,
        Stronghold stronghold,
        StrongholdActor forceActor,
        int nextId,
        byte typeId,
        int soldiers,
        string unitName,
        bool isMounted = false)
    {
        var sub = CreateSubUnit(gameData, stronghold, unitId: 0, typeId, soldiers, forceActor, isMounted: isMounted, unitName: unitName);
        sub.Id = nextId;
        gameData.SubUnits[nextId] = sub;
        forceActor.SubUnitIds.Add(nextId);
        return nextId + 1;
    }

    private static SubUnit CreateSubUnit(
        GameData gameData,
        Stronghold stronghold,
        int unitId,
        int typeId,
        int soldiers,
        StrongholdActor forceActor,
        SubUnit? template = null,
        bool isMounted = false,
        string unitName = "")
    {
        var id = gameData.SubUnits.Keys.DefaultIfEmpty(0).Max() + 1;
        var sub = new SubUnit
        {
            Id = id,
            TypeId = (byte)typeId,
            TypeName = StrategyTroopTypes.ResolveName(typeId, template?.TypeName),
            UnitName = string.IsNullOrWhiteSpace(unitName) ? template?.UnitName ?? "" : unitName,
            Morale = template?.Morale ?? forceActor.Morale,
            Training = template?.Training ?? forceActor.Training,
            IsMounted = template?.IsMounted ?? isMounted || typeId == StrategyTroopTypes.Cavalry,
            ForceId = stronghold.ForceId,
            StrongholdId = stronghold.Id,
            UnitId = unitId,
            Soldier = soldiers
        };
        gameData.SubUnits[id] = sub;
        return sub;
    }
}

public sealed record GarrisonTroopPoolEntry(int TypeId, string TypeName, int Soldiers);
