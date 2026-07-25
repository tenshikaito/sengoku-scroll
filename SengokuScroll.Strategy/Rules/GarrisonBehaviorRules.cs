using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 据点守军行为：敌军逼近时占格守城；敌占城格则封锁出城；
/// 城外敌军弱时可抽象出击（不在城外格生成地图单位）。
/// </summary>
public static class GarrisonBehaviorRules
{
    /// <summary>据点威胁感知：敌对军事单位距据点曼哈顿距离不超过此值即触发守备编组。</summary>
    public const int ThreatManhattanDistance = 2;

    /// <summary>抽象出击：己方总守军至少为城外敌军此比例。</summary>
    public const double SallyMinStrengthRatio = 0.85;

    /// <summary>抽象出击：守军/城内士气下限。</summary>
    public const int MinMoraleForSally = 40;

    /// <summary>抽象出击：从城内抽出的最大比例（其余留城）。</summary>
    public const double MaxCitySoldiersSallyRatio = 0.65;

    /// <summary>桶狭间式奇袭：领主/当主胆量达到此值才在兵力劣势时仍冒险出城。</summary>
    public const int HighCourageSallyThreshold = 80;

    /// <summary>野战威胁下守军总兵力至少达到城外敌军此比例才占格/出击；否则笼城待援。</summary>
    public const double HoldInCityMinStrengthRatio = 0.85;

    /// <summary>日初处理据点守军：劣势撤回城内 → 优势占格 → 抽象出击。</summary>
    public static bool TryExecuteStrongholdDefense(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        out string? actionCode)
    {
        actionCode = null;

        if (TryRetreatGarrisonToCityWhenOutnumbered(context, stronghold, gameData, meta))
        {
            actionCode = "GarrisonHoldAwaitingRelief";
            return true;
        }

        if (TryAbstractSally(context, stronghold, gameData, meta, out var sallyMessage))
        {
            actionCode = "GarrisonAbstractSally";
            return true;
        }

        return false;
    }

    /// <summary>野战威胁下兵力劣势：将城格上的己方地图单位撤回 InStronghold。</summary>
    public static bool TryRetreatGarrisonToCityWhenOutnumbered(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var fieldUnit = gameData.Units.Values.FirstOrDefault(u =>
            UnitStrongholdPresenceRules.IsOwnerMapDefenderOnTile(u, stronghold));
        if (fieldUnit is null)
            return false;

        if (!ShouldHoldInCityAwaitingRelief(stronghold, gameData, meta))
            return false;

        return UnitStrongholdPresenceActions.EnterStronghold(context, fieldUnit, stronghold, gameData, meta)
            .IsSuccess;
    }

    [Obsolete("废除威胁占格 materialize；笼城须提前组建 InStronghold Unit。")]
    public static bool TryPrepareGarrisonOnThreat(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
        => false;

    /// <summary>
    /// 城外野战威胁且守军总兵力不足：笼城待援（含敌军已踩城格，仍比较邻域总兵力）。
    /// 仅当领主胆量极高（≥<see cref="HighCourageSallyThreshold"/>）时才在劣势下冒险出城。
    /// </summary>
    public static bool ShouldHoldInCityAwaitingRelief(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta = null)
    {
        if (!StrongholdGarrisonRules.HasCityGarrison(stronghold, gameData)
            && StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData) is null)
            return false;

        var threats = FindAllHostileThreats(stronghold, gameData);
        if (threats.Count == 0)
            return false;

        var enemyTotal = threats.Sum(t => t.Soldier);
        if (enemyTotal <= 0)
            return false;

        var defenderTotal = StrongholdGarrisonRules.CountTotalGarrisonAt(stronghold, gameData);
        if (defenderTotal >= enemyTotal * HoldInCityMinStrengthRatio)
            return false;

        if (meta is not null
            && ResolveStrongholdLordCourage(stronghold, meta, gameData) >= HighCourageSallyThreshold)
        {
            return false;
        }

        return true;
    }

    /// <summary>据点邻域内所有敌对军事威胁（含已踩城格敌军）。</summary>
    public static IReadOnlyList<Unit> FindAllHostileThreats(Stronghold stronghold, GameData gameData)
    {
        var threats = new List<Unit>();
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!IsHostileToStronghold(unit, stronghold, gameData))
                continue;

            if (unit.Location.IsSameTile(stronghold.Location)
                || GetManhattanDistanceToStronghold(unit, stronghold) <= ThreatManhattanDistance)
            {
                threats.Add(unit);
            }
        }

        return threats;
    }

    /// <summary>解析据点城主/当主胆量（0–100）。</summary>
    public static int ResolveStrongholdLordCourage(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (stronghold.LordId > 0
            && gameData.Characters.TryGetValue(stronghold.LordId, out var appointed)
            && !appointed.IsDead)
        {
            return appointed.Personality.Courage;
        }

        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            stronghold.ForceId, meta, gameData);
        if (lordId > 0
            && gameData.Characters.TryGetValue(lordId, out var forceLord)
            && !forceLord.IsDead)
        {
            return forceLord.Personality.Courage;
        }

        return 50;
    }

    /// <summary>封锁状态下对城外弱敌抽象出击（守城单位不离开城格；纯城内兵不生成城外地图单位）。</summary>
    public static bool TryAbstractSally(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        out string? message)
    {
        message = null;

        if (!IsStrongholdBlockaded(stronghold, gameData))
            return false;

        if (ShouldHoldInCityAwaitingRelief(stronghold, gameData, meta))
            return false;

        var adjacentEnemies = FindAdjacentHostileUnitsOutsideTile(stronghold, gameData);
        if (adjacentEnemies.Count == 0)
            return false;

        var enemyTotal = adjacentEnemies.Sum(u => u.Soldier);
        var garrisonUnit = StrongholdGarrisonRules.FindActiveDefenderUnit(stronghold, gameData);
        var citySoldiers = StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold);
        var defenderTotal = (garrisonUnit?.Soldier ?? 0) + citySoldiers;

        if (defenderTotal < enemyTotal * SallyMinStrengthRatio)
            return false;

        var morale = garrisonUnit?.Morale ?? stronghold.ForceActor.Morale;
        if (morale < MinMoraleForSally)
            return false;

        var target = adjacentEnemies.OrderBy(u => u.Soldier).First();

        // 业务：已有地图守军且城格未被敌占——由该单位在城格上接敌
        if (garrisonUnit is not null
            && !garrisonUnit.InStronghold
            && !IsEnemyOccupyingStrongholdTile(stronghold, gameData, out _)
            && garrisonUnit.Stance != UnitStance.Attacking
            && garrisonUnit.Status != UnitStatus.Standoff)
        {
            UnitBattleActions.QueueAttack(garrisonUnit, target.Id);
            message = $"{stronghold.Name} 守军出城打击 {target.Name}（据守城格）";
            return true;
        }

        // 业务：敌占城格或仅有城内兵——抽象出击，不在城外生成 Unit
        var commit = ComputeCitySoldiersToCommit(citySoldiers, target.Soldier);
        if (commit <= 0)
            return false;

        if (!StrongholdGarrisonActions.ExecuteAbstractSally(
                context,
                stronghold,
                target,
                gameData,
                context.GameWorld.GameMapMasterData,
                commit,
                meta.Difficulty))
            return false;

        message = $"{stronghold.Name} 城内 {commit} 人出击打击 {target.Name}（无城外单位）";
        return true;
    }

    /// <summary>敌军事单位是否已占据据点格（导致无法再出城占格）。</summary>
    public static bool IsEnemyOccupyingStrongholdTile(
        Stronghold stronghold,
        GameData gameData,
        out Unit? occupier)
    {
        occupier = gameData.Units.Values.FirstOrDefault(u =>
            u.IsMilitary
            && u.Soldier > 0
            && u.Location.IsSameTile(stronghold.Location)
            && IsHostileToStronghold(u, stronghold, gameData));

        return occupier is not null;
    }

    /// <summary>威胁来临前抢占城格：敌军已站在据点格上则不再抢先编组。</summary>
    public static bool CanPreemptivelyMaterializeGarrisonOnTile(Stronghold stronghold, GameData gameData)
        => !IsEnemyOccupyingStrongholdTile(stronghold, gameData, out _);

    /// <summary>
    /// 从城内编组守城单位：空城格可编组；或攻城方已入城格（与 <see cref="MovementRules"/> 同格接敌一致）。
    /// </summary>
    public static bool CanMaterializeGarrisonOnTile(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta = null)
    {
        if (ShouldHoldInCityAwaitingRelief(stronghold, gameData, meta))
            return false;

        if (!IsEnemyOccupyingStrongholdTile(stronghold, gameData, out var occupier))
            return true;

        return occupier!.Directive is UnitDirective.Occupy or UnitDirective.Raid;
    }

    /// <summary>
    /// 据点处于封锁（抽象出击/补给等）：敌占城格，或守军被围标记，或同格充分包围。
    /// 出城野战禁令请用 <see cref="IsFullyEncircled"/>；不充分包围见 <see cref="CanSallyOutDespiteSiege"/>。
    /// </summary>
    public static bool IsStrongholdBlockaded(Stronghold stronghold, GameData gameData)
    {
        if (IsEnemyOccupyingStrongholdTile(stronghold, gameData, out _))
            return true;

        var garrison = StrongholdGarrisonRules.FindActiveDefenderUnit(stronghold, gameData);
        if (garrison?.Status == UnitStatus.BeingSurround)
            return true;

        return IsFullyEncircled(stronghold, gameData);
    }

    /// <summary>同格攻城令且围城压力 ≥ 1：禁止出城野战。</summary>
    public static bool IsFullyEncircled(Stronghold stronghold, GameData gameData)
    {
        var hasSiegeOrderOnTile = gameData.Units.Values.Any(u =>
            u.IsMilitary
            && u.Soldier > 0
            && u.SiegeMode != UnitSiegeMode.None
            && u.Location.IsSameTile(stronghold.Location)
            && IsHostileToStronghold(u, stronghold, gameData));

        if (!hasSiegeOrderOnTile)
            return false;

        var required = BattlefieldContainerRules.GetRequiredSiegeSoldiers(stronghold);
        return BattlefieldContainerRules.GetSiegePressure(stronghold, gameData, required) >= 1.0;
    }

    /// <summary>有同格攻城令但不充分包围时可出城野战。</summary>
    public static bool CanSallyOutDespiteSiege(Stronghold stronghold, GameData gameData)
    {
        var hasSiegeOrderOnTile = gameData.Units.Values.Any(u =>
            u.IsMilitary
            && u.Soldier > 0
            && u.SiegeMode != UnitSiegeMode.None
            && u.Location.IsSameTile(stronghold.Location)
            && IsHostileToStronghold(u, stronghold, gameData));

        if (!hasSiegeOrderOnTile)
            return !IsFullyEncircled(stronghold, gameData);

        return !IsFullyEncircled(stronghold, gameData);
    }

    /// <summary>据点邻域（range=2）是否存在野战威胁（敌对军事单位，且未占城格）。</summary>
    public static bool HasFieldBattleProximityThreat(Stronghold stronghold, GameData gameData)
        => FindFieldBattleProximityThreats(stronghold, gameData).Count > 0;

    /// <summary>兼容旧名：等同 <see cref="HasFieldBattleProximityThreat"/>。</summary>
    public static bool HasThreateningEnemies(Stronghold stronghold, GameData gameData)
        => HasFieldBattleProximityThreat(stronghold, gameData);

    /// <summary>据点邻域（range=2）内的野战威胁单位：敌对、有兵、未占城格。</summary>
    public static IReadOnlyList<Unit> FindFieldBattleProximityThreats(Stronghold stronghold, GameData gameData)
    {
        var threats = new List<Unit>();
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!IsFieldBattleProximityThreat(unit, stronghold, gameData))
                continue;

            threats.Add(unit);
        }

        return threats;
    }

    /// <summary>兼容旧名：等同 <see cref="FindFieldBattleProximityThreats"/>。</summary>
    public static IReadOnlyList<Unit> FindThreateningEnemies(Stronghold stronghold, GameData gameData)
        => FindFieldBattleProximityThreats(stronghold, gameData);

    /// <summary>野战威胁：敌在据点 range 内且不在城格（占城格走封锁/攻城路径）。</summary>
    public static bool IsFieldBattleProximityThreat(Unit enemy, Stronghold stronghold, GameData gameData)
    {
        if (!IsHostileToStronghold(enemy, stronghold, gameData))
            return false;

        if (enemy.Location.IsSameTile(stronghold.Location))
            return false;

        return GetManhattanDistanceToStronghold(enemy, stronghold) <= ThreatManhattanDistance;
    }

    private static int GetManhattanDistanceToStronghold(Unit enemy, Stronghold stronghold)
        => Math.Abs(enemy.Location.X - stronghold.Location.X)
           + Math.Abs(enemy.Location.Y - stronghold.Location.Y);

    /// <summary>与据点相邻、不在城格上的敌对军事单位（抽象出击目标）。</summary>
    public static IReadOnlyList<Unit> FindAdjacentHostileUnitsOutsideTile(
        Stronghold stronghold,
        GameData gameData)
    {
        var result = new List<Unit>();
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!IsHostileToStronghold(unit, stronghold, gameData))
                continue;

            if (unit.Location.IsSameTile(stronghold.Location))
                continue;

            if (!unit.Location.IsAdjacent(stronghold.Location))
                continue;

            result.Add(unit);
        }

        return result;
    }

    /// <summary>
    /// 无野战威胁且未被封锁，且附近无敌方/无友军野战单位时，将 materialize 守军解散回城内兵。
    /// </summary>
    [Obsolete("InStronghold 守军不再自动解散回池。")]
    public static bool TryDissolveGarrisonWhenSafe(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData)
        => false;

    /// <summary>据点 threat 范围内是否有己方非 Support 野战部队。</summary>
    public static bool HasFriendlyFieldPresenceNear(Stronghold stronghold, GameData gameData)
        => gameData.Units.Values.Any(u =>
            u.IsMilitary
            && u.Soldier > 0
            && u.ForceId == stronghold.ForceId
            && u.Directive != UnitDirective.Support
            && GetManhattanDistanceToStronghold(u, stronghold) <= ThreatManhattanDistance);

    /// <summary>据点是否处于受攻/封锁状态（供援防 AI 使用）。</summary>
    public static bool IsStrongholdUnderAttack(Stronghold stronghold, GameData gameData)
        => IsStrongholdBlockaded(stronghold, gameData)
           || HasFieldBattleProximityThreat(stronghold, gameData);

    private static int ComputeCitySoldiersToCommit(int citySoldiers, int targetEnemySoldiers)
    {
        if (citySoldiers <= 0)
            return 0;

        var cap = Math.Max(1, (int)Math.Round(citySoldiers * MaxCitySoldiersSallyRatio));
        var needed = Math.Max(1, (int)Math.Ceiling(targetEnemySoldiers * SallyMinStrengthRatio));
        return Math.Min(citySoldiers, Math.Max(Math.Min(needed, cap), 1));
    }

    private static bool IsHostileToStronghold(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (unit.ForceId == stronghold.ForceId)
            return false;

        if (!gameData.Forces.TryGetValue(unit.ForceId, out var unitForce)
            || !gameData.Forces.TryGetValue(stronghold.ForceId, out var holderForce))
            return false;

        return DiplomacyRules.IsEnemy(unitForce, holderForce).IsSuccess;
    }
}
