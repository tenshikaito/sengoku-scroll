using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>创建/加入/关闭地图战场容器；同步单位 BattlefieldId 与对峙状态。</summary>
public static class BattlefieldContainerRules
{
    /// <summary>敌对双方同格时确保存在战场并双方入场。</summary>
    public static Battlefield EnsureFieldBattlefield(Unit unitA, Unit unitB, GameData gameData)
    {
        var loc = new Point2(unitA.Location.X, unitA.Location.Y);
        var existing = FindOpenAt(loc, gameData, BattlefieldKind.Field);
        if (existing is not null)
        {
            AddUnitToBattlefield(existing, unitA, gameData);
            AddUnitToBattlefield(existing, unitB, gameData);
            return existing;
        }

        var war = WarRules.EnsureWarBetween(
            gameData,
            unitA.ForceId,
            unitB.ForceId,
            gameData.GameDate);

        var bf = new Battlefield
        {
            Id = gameData.NextBattlefieldId++,
            Kind = BattlefieldKind.Field,
            Location = loc,
            WarId = war.Id,
            StrongholdId = 0,
            StandoffDays = 0,
            IsClosed = false,
        };

        gameData.Battlefields[bf.Id] = bf;
        AddUnitToBattlefield(bf, unitA, gameData);
        AddUnitToBattlefield(bf, unitB, gameData);
        return bf;
    }

    /// <summary>攻城令单位与据点同格时确保 Siege 战场。</summary>
    public static Battlefield EnsureSiegeBattlefield(Unit siegingUnit, Stronghold stronghold, GameData gameData)
    {
        var loc = new Point2(stronghold.Location.X, stronghold.Location.Y);
        var existing = FindOpenAt(loc, gameData, BattlefieldKind.Siege);
        if (existing is not null)
        {
            AddUnitToBattlefield(existing, siegingUnit, gameData);
            return existing;
        }

        var war = WarRules.EnsureWarBetween(
            gameData,
            siegingUnit.ForceId,
            stronghold.ForceId,
            gameData.GameDate);

        var bf = new Battlefield
        {
            Id = gameData.NextBattlefieldId++,
            Kind = BattlefieldKind.Siege,
            Location = loc,
            WarId = war.Id,
            StrongholdId = stronghold.Id,
            StandoffDays = 0,
            IsClosed = false,
        };

        gameData.Battlefields[bf.Id] = bf;
        AddUnitToBattlefield(bf, siegingUnit, gameData);
        return bf;
    }

    public static Battlefield? FindOpenAt(Point2 location, GameData gameData, BattlefieldKind? kind = null)
    {
        foreach (var bf in gameData.Battlefields.Values)
        {
            if (bf.IsClosed)
                continue;
            if (bf.Location.X != location.X || bf.Location.Y != location.Y)
                continue;
            if (kind is not null && bf.Kind != kind)
                continue;
            return bf;
        }

        return null;
    }

    public static void AddUnitToBattlefield(Battlefield bf, Unit unit, GameData gameData)
    {
        if (bf.IsClosed || unit.Soldier <= 0)
            return;

        if (!gameData.Wars.TryGetValue(bf.WarId, out var war))
            return;

        List<int> side;
        bool isSideA;
        if (WarRules.IsOnAggressorSide(war, unit.ForceId))
        {
            side = bf.SideAUnitIds;
            isSideA = true;
        }
        else if (WarRules.IsParticipant(war, unit.ForceId))
        {
            side = bf.SideBUnitIds;
            isSideA = false;
        }
        else
        {
            // 业务：尚非参战名单但外交敌对时，视为对侧加入名单
            if (WarRules.IsOnAggressorSide(war, war.AggressorForceId)
                && unit.ForceId != war.AggressorForceId)
            {
                WarRules.TryJoinWar(war, unit.ForceId, joinAggressorSide: false);
                side = bf.SideBUnitIds;
                isSideA = false;
            }
            else
            {
                WarRules.TryJoinWar(war, unit.ForceId, joinAggressorSide: true);
                side = bf.SideAUnitIds;
                isSideA = true;
            }
        }

        if (!side.Contains(unit.Id))
        {
            // 业务：首支入侧为主战
            if (side.Count == 0)
            {
                if (isSideA)
                    bf.MainCombatantAUnitId = unit.Id;
                else
                    bf.MainCombatantBUnitId = unit.Id;
            }

            side.Add(unit.Id);
        }

        if (unit.BattlefieldId == 0)
        {
            unit.BattlefieldEntryFrom = null;
        }

        unit.BattlefieldId = bf.Id;

        // 业务：溃逃/混乱不进入正常对峙皮
        if (unit.Status is not (UnitStatus.Chaos or UnitStatus.Fearful or UnitStatus.BeingSurround or UnitStatus.Routing))
        {
            unit.Status = UnitStatus.Standoff;
            unit.Stance = UnitStance.Attacking;
        }
    }

    /// <summary>记录入场方向并设置挑战目标为主战对手（兼容既有结算系统）。</summary>
    public static void BindEngagementTargets(Battlefield bf, Unit challenger, Point2? entryFrom, GameData gameData)
    {
        if (entryFrom is not null)
            challenger.BattlefieldEntryFrom = entryFrom;

        if (!gameData.Wars.TryGetValue(bf.WarId, out var war))
            return;

        var challengerOnA = WarRules.IsOnAggressorSide(war, challenger.ForceId);
        var opponentMain = challengerOnA ? bf.MainCombatantBUnitId : bf.MainCombatantAUnitId;
        if (opponentMain <= 0)
        {
            var oppSide = challengerOnA ? bf.SideBUnitIds : bf.SideAUnitIds;
            opponentMain = oppSide.FirstOrDefault();
        }

        if (opponentMain > 0)
            challenger.ActionTarget.UnitId = opponentMain;
    }

    public static void CloseBattlefield(Battlefield bf, GameData gameData)
    {
        bf.IsClosed = true;
        foreach (var id in bf.SideAUnitIds.Concat(bf.SideBUnitIds).Distinct())
        {
            if (!gameData.Units.TryGetValue(id, out var u))
                continue;

            if (u.BattlefieldId != bf.Id)
                continue;

            LeaveBattlefield(u);
        }
    }

    /// <summary>据点陷落或攻城结束时，关闭该格及关联 Siege 战场。</summary>
    public static void CloseBattlefieldsForStronghold(Stronghold stronghold, GameData gameData)
    {
        foreach (var bf in gameData.Battlefields.Values.ToList())
        {
            if (bf.IsClosed)
                continue;

            var atTile = bf.Location.X == stronghold.Location.X && bf.Location.Y == stronghold.Location.Y;
            var siegeTarget = bf.Kind == BattlefieldKind.Siege && bf.StrongholdId == stronghold.Id;
            if (atTile || siegeTarget)
                CloseBattlefield(bf, gameData);
        }
    }

    /// <summary>关闭无参战单位或已无攻城令的开放战场（避免「围」标记残留）。</summary>
    public static void PruneOpenBattlefields(GameData gameData)
    {
        foreach (var bf in gameData.Battlefields.Values.ToList())
        {
            if (bf.IsClosed)
                continue;

            if (bf.Kind == BattlefieldKind.Siege && bf.StrongholdId > 0)
            {
                var underSiege = gameData.Units.Values.Any(u =>
                    u.IsMilitary
                    && u.Soldier > 0
                    && u.SiegeMode != UnitSiegeMode.None
                    && u.ActionTarget.StrongholdId == bf.StrongholdId);

                if (!underSiege)
                {
                    CloseBattlefield(bf, gameData);
                    continue;
                }
            }

            var hasCombatants = bf.SideAUnitIds
                .Concat(bf.SideBUnitIds)
                .Distinct()
                .Any(id => gameData.Units.TryGetValue(id, out var u)
                           && u.Soldier > 0
                           && u.BattlefieldId == bf.Id);

            if (!hasCombatants)
                CloseBattlefield(bf, gameData);
        }
    }

    public static void LeaveBattlefield(Unit unit)
    {
        unit.BattlefieldId = 0;
        unit.BattlefieldEntryFrom = null;
        unit.Stance = UnitStance.Normal;
        unit.ActionTarget.UnitId = 0;

        if (unit.Status == UnitStatus.Standoff)
            unit.Status = UnitStatus.Waiting;
    }

    /// <summary>围城同格兵力充分度 0~1（持攻城令且对城敌对的同格军，含共战盟友）。</summary>
    public static double GetSiegePressure(Stronghold stronghold, GameData gameData, int requiredSoldiers)
    {
        if (requiredSoldiers <= 0)
            return 1;

        var soldiers = 0;
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;
            if (unit.SiegeMode == UnitSiegeMode.None)
                continue;
            if (!unit.Location.IsSameTile(stronghold.Location))
                continue;
            if (unit.ForceId == stronghold.ForceId)
                continue;
            if (!gameData.Forces.TryGetValue(unit.ForceId, out var uf)
                || !gameData.Forces.TryGetValue(stronghold.ForceId, out var hf))
                continue;

            var hostile = DiplomacyRules.IsEnemy(uf, hf).IsSuccess
                          || WarRules.AreWarEnemies(unit.ForceId, stronghold.ForceId, gameData);
            if (!hostile)
                continue;

            soldiers += unit.Soldier;
        }

        return Math.Clamp(soldiers / (double)requiredSoldiers, 0, 1);
    }

    /// <summary>据点规模必要围城兵力（创造式门槛，暂按等级近似）。</summary>
    public static int GetRequiredSiegeSoldiers(Stronghold stronghold)
    {
        // 业务：规模门槛占位——城内兵+等级感；后续可挂配置表
        var baseNeed = 3000;
        var garrison = stronghold.ForceActor?.Soldier ?? 0;
        return Math.Max(baseNeed, garrison * 2);
    }
}
