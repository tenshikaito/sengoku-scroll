using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Policies.UnitAi;
using SengokuScroll.Strategy.Vision;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>策略 AI：按单位状态、方针、附近威胁与全因素胜率决定行军与接敌；决策返回思维链。</summary>
public static class StrategyUnitAIRules
{
    /// <summary>士气低于此值且邻敌时触发撤退改方针。</summary>
    public const int LowMoraleRetreatThreshold = 35;

    /// <summary>撤退后士气恢复至此值且无邻敌时可脱离撤退方针。</summary>
    public const int RecoverOccupyMoraleThreshold = 55;

    /// <summary>邻敌时己方兵力低于敌军此比例视为兵力劣势，改撤退。</summary>
    public const double AdjacentOutnumberedRetreatRatio = 0.55;

    /// <summary>占领方针下接敌所需最低兵力比（己方/敌军）。</summary>
    public const double OccupyEngageMinStrengthRatio = 0.75;

    /// <summary>袭扰方针下接敌所需最低兵力比（己方/敌军，门槛更低）。</summary>
    public const double RaidEngageMinStrengthRatio = 0.55;

    /// <summary>非军事、无兵、已在战斗或特殊状态下跳过每日 AI 决策。</summary>
    public static bool ShouldSkipDailyAi(Unit unit)
        => UnitAiSkipBehaviorRegistry.ShouldSkipDailyAi(unit);

    /// <summary>返回跳过 AI 的人类可读原因（供调试/日志）。</summary>
    public static string? DescribeSkipReason(Unit unit)
        => UnitAiSkipBehaviorRegistry.DescribeSkipReason(unit);

    /// <summary>
    /// 对峙中 AI 脱困：对手离场则清除；长期同格对峙且胜率偏低则改撤退。
    /// </summary>
    public static StrategyAiDecision? TryResolveStandoffEngagement(
        Unit unit,
        GameData gameData,
        StrategyFieldEngagementRegistry engagementRegistry,
        GameMapMasterData? mapMaster)
    {
        if (unit.Status != UnitStatus.Standoff || unit.ActionTarget.UnitId <= 0)
            return null;

        var opponentId = unit.ActionTarget.UnitId;
        if (!gameData.Units.TryGetValue(opponentId, out var opponent) || opponent.Soldier <= 0)
        {
            var thought = new StrategyAiThought().Add("对峙对手已消失，脱离战场");
            BattlefieldEngagementRules.LeaveBattlefield(unit);
            engagementRegistry.ClearStandoff(unit.Id, opponentId);
            return StrategyAiDecision.Ok("StandoffOpponentGone", "对峙对手消失，脱离接敌", thought);
        }

        if (!MoveEngagementRules.IsInEngagementRange(unit, opponent))
        {
            var thought = new StrategyAiThought().Add("对手已不在同格，脱离战场");
            BattlefieldEngagementRules.LeaveBattlefield(unit);
            engagementRegistry.ClearStandoff(unit.Id, opponentId);
            return StrategyAiDecision.Ok("StandoffOpponentLeft", "对手离格，脱离接敌", thought);
        }

        var standoffDays = engagementRegistry.GetStandoffDays(unit.Id, opponentId);
        if (standoffDays < BattleConstants.AiStandoffBreakRetreatDays)
            return null;

        var winRate = BattleEngagementScorer.ScoreCommitWinRate(unit, opponent, gameData, mapMaster);
        if (winRate >= BattleConstants.AiRetreatCommitWinRateThreshold)
            return null;

        var retreatThought = new StrategyAiThought().Add(
            "对峙第{0}日 强袭胜率={1}%<{2}%，主动脱离改撤退",
            standoffDays,
            winRate,
            BattleConstants.AiRetreatCommitWinRateThreshold);
        unit.Directive = UnitDirective.Retreat;
        BattlefieldEngagementRules.LeaveBattlefield(unit);
        engagementRegistry.ClearStandoff(unit.Id, opponentId);
        return StrategyAiDecision.Ok(
            "StandoffBreakRetreat",
            $"对峙{standoffDays}日胜率偏低，改撤退",
            retreatThought);
    }

    /// <summary>根据战局微调方针；返回是否改写及思维链。</summary>
    public static StrategyAiDirectiveDecision EvaluateDirective(
        Unit unit,
        GameData gameData,
        int playerForceId,
        GameMapMasterData? mapMaster = null,
        StrategyScenarioMeta? meta = null,
        IReadOnlyList<Unit>? observedHostileUnits = null,
        IReadOnlyList<Stronghold>? observedHostileStrongholds = null)
    {
        var thought = new StrategyAiThought();
        var from = unit.Directive.ToString();
        var aiControlled = meta is not null
            ? StrategyAiControlRules.IsForceAiControlled(meta, unit.ForceId)
            : unit.ForceId != playerForceId;
        thought.Add("当前方针={0} 士气={1} 兵力={2} 状态={3} AI控制={4}", from, unit.Morale, unit.Soldier, unit.Status, aiControlled);

        var hostileUnits = observedHostileUnits ?? ResolveHostileUnits(unit, gameData);
        var engagementEnemy = FindEngagementRangeEnemy(unit, hostileUnits);
        thought.Add("敌对部队={0} 接敌范围内敌军={1}",
            hostileUnits.Count,
            engagementEnemy is null ? "无" : $"{engagementEnemy.Name}#{engagementEnemy.Id}({engagementEnemy.Soldier})");

        // 业务：恐惧状态或低士气且有接敌敌军时，进攻方针改为撤退
        if (unit.Status == UnitStatus.Fearful
            || (unit.Morale < LowMoraleRetreatThreshold && engagementEnemy is not null))
        {
            thought.Add("触发低士气/恐惧撤退：Fearful={0} Morale={1}<{2} 有接敌敌={3}",
                unit.Status == UnitStatus.Fearful,
                unit.Morale,
                LowMoraleRetreatThreshold,
                engagementEnemy is not null);

            if (unit.Directive is UnitDirective.Occupy or UnitDirective.Raid or UnitDirective.Move)
            {
                unit.Directive = UnitDirective.Retreat;
                return StrategyAiDirectiveDecision.ChangedTo(
                    "RetreatLowMorale",
                    $"士气/恐惧导致改方针 {from}→Retreat",
                    thought,
                    from,
                    nameof(UnitDirective.Retreat));
            }

            return StrategyAiDirectiveDecision.Unchanged("AlreadyRetreatOrHold", "已处于撤退或非进攻方针", thought);
        }

        // 业务：占领/袭扰方针且邻敌时，按兵力比与胜率决定是否改撤退
        if (engagementEnemy is not null
            && unit.Directive is (UnitDirective.Occupy or UnitDirective.Raid))
        {
            if (IsOutnumbered(unit, engagementEnemy, AdjacentOutnumberedRetreatRatio))
            {
                thought.Add("兵力劣势：己方{0} < 敌{1}×{2:0.##}",
                    unit.Soldier, engagementEnemy.Soldier, AdjacentOutnumberedRetreatRatio);
                unit.Directive = UnitDirective.Retreat;
                return StrategyAiDirectiveDecision.ChangedTo(
                    "RetreatOutnumbered",
                    $"邻敌兵力劣势改方针 {from}→Retreat",
                    thought,
                    from,
                    nameof(UnitDirective.Retreat));
            }

            var winRate = BattleEngagementScorer.ScoreCommitWinRate(
                unit, engagementEnemy, gameData, mapMaster);
            thought.Add("接敌范围内敌军全因素胜率={0}% 撤退阈值={1}%",
                winRate, BattleConstants.AiRetreatCommitWinRateThreshold);

            // 业务：全因素胜率低于 AI 撤退阈值时放弃进攻
            if (winRate > 0 && winRate < BattleConstants.AiRetreatCommitWinRateThreshold)
            {
                unit.Directive = UnitDirective.Retreat;
                return StrategyAiDirectiveDecision.ChangedTo(
                    "RetreatLowWinRate",
                    $"接敌敌军胜率过低({winRate}%) 改方针 {from}→Retreat",
                    thought,
                    from,
                    nameof(UnitDirective.Retreat));
            }
        }

        // 业务：AI 控制势力默认将 Move 升为 Occupy，主动寻敌进攻
        if (unit.Directive == UnitDirective.Move
            && aiControlled
            && unit.Morale >= LowMoraleRetreatThreshold
            && (hostileUnits.Count > 0
                || (observedHostileStrongholds ?? ResolveHostileStrongholds(unit, gameData)).Count > 0))
        {
            thought.Add("AI 控制势力默认 Move→Occupy（有敌对目标）");
            unit.Directive = UnitDirective.Occupy;
            return StrategyAiDirectiveDecision.ChangedTo(
                "PromoteOccupy",
                "AI Move 升为 Occupy",
                thought,
                from,
                nameof(UnitDirective.Occupy));
        }

        if (unit.Directive != UnitDirective.Retreat)
            return StrategyAiDirectiveDecision.Unchanged("KeepDirective", $"保持方针 {unit.Directive}", thought);

        // 业务：撤退中士气恢复、无邻敌且非恐惧时，AI 恢复 Occupy、玩家恢复 Move
        if (unit.Morale >= RecoverOccupyMoraleThreshold
            && engagementEnemy is null
            && unit.Status != UnitStatus.Fearful)
        {
            var to = aiControlled
                ? UnitDirective.Occupy
                : UnitDirective.Move;
            thought.Add("撤退恢复：士气{0}>={1} 无邻敌 → {2}", unit.Morale, RecoverOccupyMoraleThreshold, to);
            unit.Directive = to;
            return StrategyAiDirectiveDecision.ChangedTo(
                "RecoverFromRetreat",
                $"脱离危险恢复方针 Retreat→{to}",
                thought,
                from,
                to.ToString());
        }

        thought.Add("继续撤退：士气={0} 接敌敌={1}", unit.Morale, engagementEnemy is not null);
        return StrategyAiDirectiveDecision.Unchanged("KeepRetreat", "继续撤退方针", thought);
    }

    /// <summary>执行当日 AI 行动；返回是否行动及思维链。</summary>
    public static StrategyAiDecision ExecuteDailyAction(
        Unit unit,
        GameData gameData,
        IPathfindingService pathfinding,
        IReadOnlyList<Unit> hostileUnits,
        IReadOnlyList<Stronghold> hostileStrongholds,
        IGameWorldContext? worldContext = null,
        GameRuleConfig? rules = null,
        StrategyScenarioMeta? meta = null,
        GameMapMasterData? mapMaster = null)
    {
        var thought = new StrategyAiThought();
        thought.Add("方针={0} 攻城={1} 姿态={2} 状态={3} 路径剩余={4} 目标据点={5} 目标部队={6}",
            unit.Directive,
            unit.SiegeMode,
            unit.Stance,
            unit.Status,
            unit.ActionTarget.RoutePoints.Count,
            unit.ActionTarget.StrongholdId,
            unit.ActionTarget.UnitId);

        // 业务：已抵达方针指定的敌城（同格/邻格）时不再沿旧路径穿过该城
        if (TryStopAtDirectiveStronghold(unit, gameData, hostileStrongholds, thought))
        {
            // fall through to directive handling (攻城/待命)
        }
        else if (unit.Directive != UnitDirective.Support
            && unit.Status == UnitStatus.Moving && unit.ActionTarget.RoutePoints.Count > 0)
        {
            thought.Add("已有移动路径，本日继续执行");
            return StrategyAiDecision.Ok("ContinueRoute", "继续既有路径", thought);
        }

        return unit.Directive switch
        {
            UnitDirective.Retreat => MarchRetreat(unit, gameData, hostileUnits, pathfinding, thought),
            UnitDirective.Move =>
                StrategyAiDecision.Fail("Hold", $"方针 {unit.Directive}：待机不规划路径", thought),
            UnitDirective.Support => ExecuteSupportRelief(
                unit, gameData, pathfinding, worldContext, meta, thought, hostileUnits, mapMaster),
            UnitDirective.Occupy or UnitDirective.Raid => AggressiveAction(
                unit, gameData, hostileUnits, hostileStrongholds, pathfinding, worldContext, rules, mapMaster, meta, thought),
            _ => StrategyAiDecision.Fail("UnknownDirective", $"未知方针 {unit.Directive}", thought)
        };
    }

    /// <summary>兼容旧调用：仅返回是否行动。</summary>
    public static bool TryExecuteDailyAction(
        Unit unit,
        GameData gameData,
        IPathfindingService pathfinding,
        IReadOnlyList<Unit> hostileUnits,
        IReadOnlyList<Stronghold> hostileStrongholds,
        IGameWorldContext? worldContext = null,
        GameRuleConfig? rules = null,
        StrategyScenarioMeta? meta = null,
        GameMapMasterData? mapMaster = null)
        => ExecuteDailyAction(
            unit, gameData, pathfinding, hostileUnits, hostileStrongholds,
            worldContext, rules, meta, mapMaster);

    /// <summary>解析与己方外交敌对的地图军事单位列表。</summary>
    public static IReadOnlyList<Unit> ResolveHostileUnits(Unit unit, GameData gameData)
    {
        if (!TryResolveDiplomaticForce(unit.ForceId, gameData, out var myForce))
            return [];

        return [.. gameData.Units.Values
            .Where(u => u.IsMilitary
                        && u.Soldier > 0
                        && u.ForceId != unit.ForceId
                        && TryResolveDiplomaticForce(u.ForceId, gameData, out var otherForce)
                        && DiplomacyRules.IsEnemy(myForce, otherForce).IsSuccess)];
    }

    /// <summary>解析与己方外交敌对的据点列表。</summary>
    public static IReadOnlyList<Stronghold> ResolveHostileStrongholds(Unit unit, GameData gameData)
    {
        if (!TryResolveDiplomaticForce(unit.ForceId, gameData, out var myForce))
            return [];

        return [.. gameData.Strongholds.Values
            .Where(s => s.ForceId != unit.ForceId
                        && TryResolveDiplomaticForce(s.ForceId, gameData, out var ownerForce)
                        && DiplomacyRules.IsEnemy(myForce, ownerForce).IsSuccess)];
    }

    /// <summary>只返回该势力当前视野内的敌军，避免军事 AI 读取全地图单位。</summary>
    public static IReadOnlyList<Unit> ResolveObservedHostileUnits(
        Unit unit,
        GameData gameData,
        StrategyVisibilityLedger visibility)
        => [.. ResolveHostileUnits(unit, gameData)
            .Where(enemy => visibility.IsVisible(
                unit.ForceId,
                enemy.Location.X,
                enemy.Location.Y))];

    /// <summary>只返回已发现的敌城；已探索据点可作为战略目标，但不会泄露城内数值。</summary>
    public static IReadOnlyList<Stronghold> ResolveObservedHostileStrongholds(
        Unit unit,
        GameData gameData,
        StrategyVisibilityLedger visibility)
        => [.. visibility.ObserveStrongholds(unit.ForceId, gameData.GameDate.TotalDays)
            .Where(s => s.ForceId != unit.ForceId
                && TryResolveDiplomaticForce(unit.ForceId, gameData, out var mine)
                && TryResolveDiplomaticForce(s.ForceId, gameData, out var owner)
                && DiplomacyRules.IsEnemy(mine, owner).IsSuccess)];

    /// <summary>内藩/外藩单位按宗主外交关系判定敌友。</summary>
    private static bool TryResolveDiplomaticForce(int forceId, GameData gameData, out Force force)
    {
        if (!gameData.Forces.TryGetValue(forceId, out force!))
            return false;

        if (force.Status == Force.ForceStatus.InnerVassal
            && force.SuzerainForceId is int suzerainId
            && suzerainId > 0
            && gameData.Forces.TryGetValue(suzerainId, out var suzerain))
        {
            force = suzerain;
        }

        return true;
    }

    /// <summary>解析己方势力控制的据点列表（撤退寻路用）。</summary>
    public static IReadOnlyList<Stronghold> ResolveFriendlyStrongholds(Unit unit, GameData gameData)
        => [.. gameData.Strongholds.Values.Where(s => s.ForceId == unit.ForceId)];

    /// <summary>进攻方针：优先接敌范围内敌军，否则向最优目标行军。</summary>
    private static StrategyAiDecision AggressiveAction(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Unit> hostileUnits,
        IReadOnlyList<Stronghold> hostileStrongholds,
        IPathfindingService pathfinding,
        IGameWorldContext? worldContext,
        GameRuleConfig? rules,
        GameMapMasterData? mapMaster,
        StrategyScenarioMeta? meta,
        StrategyAiThought thought)
    {
        thought.Add("进攻评估：敌对部队={0} 敌对据点={1}", hostileUnits.Count, hostileStrongholds.Count);

        if (worldContext is not null && rules is not null && unit.SiegeMode == UnitSiegeMode.None)
        {
            var siege = TryAutoSiegeOrder(unit, worldContext, gameData, hostileStrongholds, rules.SiegeOrderAp, meta, thought);
            if (siege.IsSuccess)
                return siege;
        }

        if (TryHoldAwaitSiegeOnEnemyTile(unit, gameData, hostileStrongholds, thought) is { } hold)
            return hold;

        var engage = QueueEngagementInRange(unit, hostileUnits, gameData, mapMaster, thought);
        if (engage.IsSuccess)
            return engage;

        return AdvanceTowardBestTarget(unit, gameData, hostileUnits, hostileStrongholds, pathfinding, mapMaster, thought);
    }

    /// <summary>在接敌范围内筛选合格敌军并排队攻击指令。</summary>
    private static StrategyAiDecision QueueEngagementInRange(
        Unit unit,
        IReadOnlyList<Unit> hostileUnits,
        GameData gameData,
        GameMapMasterData? mapMaster,
        StrategyAiThought thought)
    {
        if (!MoveEngagementRules.IsAggressiveDirective(unit.Directive))
        {
            thought.Add("非进攻方针，跳过接敌");
            return StrategyAiDecision.Fail("NotAggressive", "非进攻方针", thought);
        }

        if (!BattleFactorEvaluator.CanUnitEngage(unit))
        {
            thought.Add("己方不可接敌（士气/混乱/撤退）");
            return StrategyAiDecision.Fail("CannotEngage", "己方不可接敌", thought);
        }

        if (!gameData.Forces.TryGetValue(unit.ForceId, out var myForce))
            return StrategyAiDecision.Fail("NoForce", "势力数据缺失", thought);

        // 业务：袭扰方针接敌门槛低于占领方针
        var minRatio = unit.Directive == UnitDirective.Raid
            ? RaidEngageMinStrengthRatio
            : OccupyEngageMinStrengthRatio;

        Unit? best = null;
        var bestScore = int.MinValue;
        var candidates = 0;
        var rejected = 0;

        foreach (var target in hostileUnits)
        {
            if (!MoveEngagementRules.IsInEngagementRange(unit, target))
                continue;

            candidates++;

            // 业务：同格围城战允许攻击不可接敌的守军
            var sameTileSiege = unit.Location.IsSameTile(target.Location)
                && SiegeBattleRules.IsSiegeEngagement(unit, target, gameData);

            if (!sameTileSiege && !BattleFactorEvaluator.CanUnitEngage(target))
            {
                thought.Add("跳过敌军 {0}：目标不可接敌", target.Name);
                rejected++;
                continue;
            }

            if (!gameData.Forces.TryGetValue(target.ForceId, out var targetForce)
                || !DiplomacyRules.IsEnemy(myForce, targetForce).IsSuccess)
            {
                rejected++;
                continue;
            }

            if (IsOutnumbered(unit, target, minRatio))
            {
                thought.Add("跳过敌军 {0}：兵力比不足（需≥{1:0.##}）", target.Name, minRatio);
                rejected++;
                continue;
            }

            var winRate = BattleEngagementScorer.ScoreCommitWinRate(unit, target, gameData, mapMaster);
            if (winRate < BattleConstants.AiRetreatCommitWinRateThreshold)
            {
                thought.Add("跳过敌军 {0}：胜率{1}%<{2}%",
                    target.Name, winRate, BattleConstants.AiRetreatCommitWinRateThreshold);
                rejected++;
                continue;
            }

            var score = BattleEngagementScorer.ScoreAttackTarget(unit, target, gameData, mapMaster);
            var threat = BattleEngagementScorer.ScoreNearbyThreat(unit, target, gameData);
            thought.Add("候选敌军 {0}#{1} 同格={2} 胜率={3}% 威胁={4} 综合={5}",
                target.Name, target.Id, sameTileSiege, winRate, threat, score);

            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        thought.Add("接敌候选={0} 拒绝={1} 最优={2}",
            candidates, rejected, best is null ? "无" : $"{best.Name}#{best.Id}");

        if (best is null)
            return StrategyAiDecision.Fail("NoEngageTarget", "无合格接敌目标", thought);

        UnitBattleActions.QueueAttack(unit, best.Id);
        return StrategyAiDecision.Ok(
            best.Location.IsSameTile(unit.Location) ? "EngageInCity" : "EngageAdjacent",
            $"接敌 {best.Name}（{(best.Location.IsSameTile(unit.Location) ? "城内" : "相邻")}）",
            thought,
            targetUnitId: best.Id,
            targetPoint: (Point2)best.Location);
    }

    /// <summary>撤退方针：优先向友城行军，无敌城时远离最近敌军。</summary>
    private static StrategyAiDecision MarchRetreat(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Unit> hostileUnits,
        IPathfindingService pathfinding,
        StrategyAiThought thought)
    {
        thought.Add("撤退：不接敌，优先友城");
        var unitPoint = (Point2)unit.Location;
        var friendlyStrongholds = ResolveFriendlyStrongholds(unit, gameData);

        Point2? target = null;
        // 业务：有友城时选距离近且敌军压力低的据点作为撤退目标
        if (friendlyStrongholds.Count > 0)
        {
            var best = friendlyStrongholds
                .OrderBy(s => ScoreRetreatStronghold(unitPoint, s, hostileUnits))
                .First();
            target = (Point2)best.Location;
            thought.Add("友城目标={0}@{1},{2} score={3}",
                best.Name, best.Location.X, best.Location.Y,
                ScoreRetreatStronghold(unitPoint, best, hostileUnits));
        }

        // 业务：无敌城时向远离最近敌军的方向走一步
        if (target is null)
        {
            var nearestEnemy = hostileUnits
                .OrderBy(u => Manhattan(unitPoint, (Point2)u.Location))
                .FirstOrDefault();
            if (nearestEnemy is null)
            {
                thought.Add("无敌军可远离，无法规划撤退");
                return StrategyAiDecision.Fail("RetreatNoEnemy", "无敌军参照，无法撤退", thought);
            }

            target = FindBestRetreatStep(unitPoint, (Point2)nearestEnemy.Location);
            thought.Add("远离最近敌军 {0} → 一步 {1}",
                nearestEnemy.Name,
                target is null ? "无" : $"{target.Value.X},{target.Value.Y}");
        }

        if (target is null || target.Value.Equals(unitPoint))
        {
            thought.Add("撤退目标无效或与当前位置相同");
            return StrategyAiDecision.Fail("RetreatNoStep", "无可用撤退格", thought);
        }

        return QueuePath(unit, pathfinding, target.Value, thought, "MarchRetreat", $"撤退行军→({target.Value.X},{target.Value.Y})");
    }

    /// <summary>友城撤退评分：距离越近越好，邻近敌军越少越好。</summary>
    private static int ScoreRetreatStronghold(
        Point2 from,
        Stronghold stronghold,
        IReadOnlyList<Unit> hostileUnits)
    {
        var shPoint = (Point2)stronghold.Location;
        var dist = Manhattan(from, shPoint);
        var enemyPressure = hostileUnits.Count == 0
            ? 0
            : hostileUnits.Min(u => Manhattan(shPoint, (Point2)u.Location));

        return dist * 10 - enemyPressure * 3;
    }

    /// <summary>向综合评分最高的敌军或敌对据点行军。</summary>
    private static StrategyAiDecision AdvanceTowardBestTarget(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Unit> hostileUnits,
        IReadOnlyList<Stronghold> hostileStrongholds,
        IPathfindingService pathfinding,
        GameMapMasterData? mapMaster,
        StrategyAiThought thought)
    {
        // 业务：已踩敌城格时不应改奔更远据点（由 TryHoldAwaitSiegeOnEnemyTile 兜底）
        if (unit.Directive is (UnitDirective.Occupy or UnitDirective.Raid)
            && unit.SiegeMode == UnitSiegeMode.None
            && hostileStrongholds.Any(s => unit.Location.IsSameTile(s.Location)))
        {
            thought.Add("已在敌城格，拒绝改选其它行军目标");
            return StrategyAiDecision.Fail("SiegeHoldOnTile", "已在敌城固守", thought);
        }

        if (ResolveDirectiveHostileStronghold(unit, gameData, hostileStrongholds) is { } directiveTarget)
        {
            thought.Add("方针锁定目标据点 {0}", directiveTarget.Name);
            return QueuePath(
                unit,
                pathfinding,
                (Point2)directiveTarget.Location,
                thought,
                "MarchDirectiveStronghold",
                $"向方针目标 {directiveTarget.Name}行军→({directiveTarget.Location.X},{directiveTarget.Location.Y})",
                targetStrongholdId: directiveTarget.Id);
        }

        var unitPoint = (Point2)unit.Location;
        var minEngageRatio = unit.Directive == UnitDirective.Raid
            ? RaidEngageMinStrengthRatio
            : OccupyEngageMinStrengthRatio;

        Point2? bestTarget = null;
        var bestScore = int.MinValue;
        string? bestLabel = null;

        foreach (var enemy in hostileUnits)
        {
            var dist = Manhattan(unitPoint, (Point2)enemy.Location);
            if (dist <= 0)
                continue;

            // 业务：远距离敌军仅在兵力或胜率达标时才纳入行军目标
            if (IsOutnumbered(unit, enemy, minEngageRatio) && dist > 1)
            {
                thought.Add("跳过远敌 {0}：兵力劣势 dist={1}", enemy.Name, dist);
                continue;
            }

            var winRate = BattleEngagementScorer.ScoreCommitWinRate(unit, enemy, gameData, mapMaster);
            if (winRate < BattleConstants.AiRetreatCommitWinRateThreshold && dist > 2)
            {
                thought.Add("跳过远敌 {0}：胜率{1}% dist={2}", enemy.Name, winRate, dist);
                continue;
            }

            var score = BattleEngagementScorer.ScoreAttackTarget(unit, enemy, gameData, mapMaster);
            thought.Add("行军候选部队 {0} dist={1} 胜率={2}% 综合={3}", enemy.Name, dist, winRate, score);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = (Point2)enemy.Location;
                bestLabel = $"部队 {enemy.Name}";
            }
        }

        var nearbyThreat = hostileUnits.Count(u => Manhattan(unitPoint, (Point2)u.Location) <= 2);
        thought.Add("附近2格敌军威胁数={0}", nearbyThreat);

        foreach (var stronghold in hostileStrongholds)
        {
            var dist = Manhattan(unitPoint, (Point2)stronghold.Location);
            if (dist <= 0)
                continue;

            // 业务：附近有敌军时优先清威胁，暂不远程奔袭据点
            if (nearbyThreat > 0 && dist > 2)
            {
                thought.Add("跳过据点 {0}：优先清附近敌军", stronghold.Name);
                continue;
            }

            var garrison = stronghold.ForceActor.Soldier;
            var score = 700 - dist * 30 + (unit.Soldier - garrison) / 15 - stronghold.Defense / 4;
            // 业务：空城据点额外加分，鼓励速占
            if (garrison == 0)
                score += 100;

            thought.Add("行军候选据点 {0} dist={1} 驻军={2} 城防={3} 综合={4}",
                stronghold.Name, dist, garrison, stronghold.Defense, score);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = (Point2)stronghold.Location;
                bestLabel = $"据点 {stronghold.Name}";
            }
        }

        if (bestTarget is null)
        {
            thought.Add("无可行进攻目标");
            return StrategyAiDecision.Fail("NoAdvanceTarget", "无可行进攻目标", thought);
        }

        thought.Add("选定目标={0} @({1},{2}) score={3}", bestLabel, bestTarget.Value.X, bestTarget.Value.Y, bestScore);
        return QueuePath(
            unit,
            pathfinding,
            bestTarget.Value,
            thought,
            "MarchAttack",
            $"向{bestLabel}行军→({bestTarget.Value.X},{bestTarget.Value.Y})");
    }

    /// <summary>Support 方针：向受威胁友城行军援防。</summary>
    private static StrategyAiDecision ExecuteSupportRelief(
        Unit unit,
        GameData gameData,
        IPathfindingService pathfinding,
        IGameWorldContext? worldContext,
        StrategyScenarioMeta? meta,
        StrategyAiThought thought,
        IReadOnlyList<Unit> observedEnemies,
        GameMapMasterData? mapMaster)
    {
        // Re-evaluate support routes daily: besiegers can move or disappear.
        unit.ActionTarget.RoutePoints.Clear();
        unit.ActionTarget.UnitId = 0;
        if (unit.Status == UnitStatus.Moving)
            unit.Status = UnitStatus.Waiting;
        // 业务：编制同调——挂目标单位且可叠时跟随其位置/路径
        if (unit.DirectiveTargetId > 0
            && gameData.Units.TryGetValue(unit.DirectiveTargetId, out var followTarget)
            && followTarget.Soldier > 0
            && WarRules.CanMilitaryStack(unit.ForceId, followTarget.ForceId, gameData))
        {
            unit.ActionTarget.UnitId = followTarget.Id;

            if (followTarget.BattlefieldId > 0
                && gameData.Battlefields.TryGetValue(followTarget.BattlefieldId, out var bf)
                && !bf.IsClosed)
            {
                BattlefieldContainerRules.AddUnitToBattlefield(bf, unit, gameData);
                thought.Add("跟随目标入战场 BF={0}", bf.Id);
                return StrategyAiDecision.Ok("SupportJoinBattle", $"支援入战场跟随 {followTarget.Name}", thought);
            }

            if (unit.Location.IsSameTile(followTarget.Location))
            {
                thought.Add("已与支援目标 {0} 同格", followTarget.Name);
                return StrategyAiDecision.Ok(
                    "SupportStacked",
                    $"与 {followTarget.Name} 同格支援",
                    thought,
                    targetUnitId: followTarget.Id);
            }

            return QueuePath(
                unit,
                pathfinding,
                (Point2)followTarget.Location,
                thought,
                "SupportFollowUnit",
                $"跟随 {followTarget.Name}→({followTarget.Location.X},{followTarget.Location.Y})");
        }

        var threatened = ResolveThreatenedFriendlyStrongholds(unit, gameData, observedEnemies);
        // Finish the assigned relief journey even after the besiegers are gone.
        if (gameData.Strongholds.TryGetValue(unit.ActionTarget.StrongholdId, out var assigned)
            && assigned.ForceId == unit.ForceId)
            threatened = new[] { assigned }.Concat(threatened.Where(s => s.Id != assigned.Id)).ToArray();
        thought.Add("受威胁友城数={0}", threatened.Count);

        if (threatened.Count == 0)
            return StrategyAiDecision.Fail("SupportIdle", "无受威胁友城，Support 待机", thought);

        foreach (var target in threatened)
        {
            var besiegers = observedEnemies.Where(e => e.IsMilitary && e.Soldier > 0
                && !e.InStronghold
                && Manhattan((Point2)e.Location, (Point2)target.Location) <= 1)
                .OrderBy(e => e.Soldier).ThenBy(e => e.Id).ToArray();
            if (besiegers.Length > 0)
            {
                unit.ActionTarget.StrongholdId = target.Id;
                foreach (var enemy in besiegers)
                {
                    var winRate = BattleEngagementScorer.ScoreCommitWinRate(unit, enemy, gameData, mapMaster);
                    thought.Add("解围评估 {0}：胜率={1}%", enemy.Name, winRate);
                    if (!BattleFactorEvaluator.CanUnitEngage(unit)
                        || unit.Soldier < besiegers.Where(e => e.Location.IsSameTile(enemy.Location)).Sum(e => (long)e.Soldier) * OccupyEngageMinStrengthRatio
                        || winRate < BattleConstants.AiRetreatCommitWinRateThreshold)
                        continue;

                    unit.ActionTarget.UnitId = enemy.Id;
                    if (unit.Location.IsSameTile(enemy.Location))
                    {
                        UnitBattleActions.QueueAttack(unit, enemy.Id);
                        return StrategyAiDecision.Ok("ReliefEngage", $"解围接敌 {enemy.Name}", thought, targetUnitId: enemy.Id);
                    }
                    var reliefPath = pathfinding.CalculatePath(unit, (Point2)enemy.Location);
                    if (reliefPath is null || reliefPath.Count < 2
                        || worldContext is not null && !ReliefPathRules.IsTransitPathClear(unit, reliefPath, worldContext))
                        continue;
                    return QueuePath(unit, pathfinding, (Point2)enemy.Location, thought,
                        "MarchReliefBattle", $"向围城军 {enemy.Name} 行军解围", targetStrongholdId: target.Id);
                }
                unit.ActionTarget.UnitId = 0;
                return StrategyAiDecision.Fail("ReliefWaiting", "解围兵力/胜率不足或路径被阻，等待增援", thought);
            }

            if (unit.Location.IsSameTile(target.Location))
            {
                unit.ActionTarget.StrongholdId = target.Id;
                if (!unit.InStronghold
                    && worldContext is not null
                    && UnitStrongholdPresenceRules.CanEnterStronghold(unit, target, gameData))
                {
                    UnitStrongholdPresenceActions.EnterStronghold(
                        worldContext, unit, target, gameData, meta);
                    thought.Add("援防入城 {0}", target.Name);
                    return StrategyAiDecision.Ok(
                        "SupportEnteredStronghold",
                        $"援防入城 {target.Name}",
                        thought,
                        targetStrongholdId: target.Id);
                }

                thought.Add("已抵达援防目标 {0}", target.Name);
                return StrategyAiDecision.Ok(
                    "ReliefArrived",
                    $"已抵达援防据点 {target.Name}",
                    thought,
                    targetStrongholdId: target.Id);
            }

            var path = pathfinding.CalculatePath(unit, (Point2)target.Location);
            if (path is null || path.Count < 2)
                continue;

            if (worldContext is not null && !ReliefPathRules.IsTransitPathClear(unit, path, worldContext))
                continue;

            unit.ActionTarget.StrongholdId = target.Id;
            return QueuePath(
                unit,
                pathfinding,
                (Point2)target.Location,
                thought,
                "MarchRelief",
                $"援防 {target.Name}→({target.Location.X},{target.Location.Y})",
                targetStrongholdId: target.Id);
        }

        return StrategyAiDecision.Fail("ReliefPathBlocked", "援防路径均被敌军阻挡", thought);
    }

    /// <summary>AI 自动下达攻城指令：同格强攻，邻格包围。</summary>
    public static StrategyAiDecision TryAutoSiegeOrder(
        Unit unit,
        IGameWorldContext worldContext,
        GameData gameData,
        IReadOnlyList<Stronghold> hostileStrongholds,
        int siegeApCost,
        StrategyScenarioMeta? meta,
        StrategyAiThought thought)
    {
        if (unit.SiegeMode != UnitSiegeMode.None)
        {
            thought.Add("已有攻城指令 SiegeMode={0} 目标据点={1}", unit.SiegeMode, unit.ActionTarget.StrongholdId);
            return StrategyAiDecision.Fail("AlreadySieging", "已在攻城状态", thought,
                targetStrongholdId: unit.ActionTarget.StrongholdId,
                siegeMode: unit.SiegeMode.ToString());
        }

        if (unit.Directive is not (UnitDirective.Occupy or UnitDirective.Raid))
            return StrategyAiDecision.Fail("NotAggressive", "非进攻方针，不自动攻城", thought);

        var onTile = ResolveDirectiveOnTileStronghold(unit, gameData, hostileStrongholds)
                     ?? hostileStrongholds.FirstOrDefault(s => unit.Location.IsSameTile(s.Location));
        if (onTile is not null)
        {
            // Contact permits authoritative validation, and Apply must mutate the real castle.
            onTile = gameData.Strongholds[onTile.Id];
            var assaultAp = ResolveAutoSiegeApCost(unit, onTile, siegeApCost);
            thought.Add("已踩敌城 {0}，尝试强攻（令AP={1}）", onTile.Name, assaultAp);
            if (SiegeOrderRules.Validate(unit, onTile, UnitSiegeMode.Assault, gameData, assaultAp).IsSuccess)
            {
                SiegeOrderRules.Apply(worldContext, unit, onTile, UnitSiegeMode.Assault, gameData, assaultAp, meta);
                return StrategyAiDecision.Ok(
                    "SiegeAssault",
                    $"自动强攻 {onTile.Name}",
                    thought,
                    targetStrongholdId: onTile.Id,
                    siegeMode: nameof(UnitSiegeMode.Assault),
                    stance: unit.Stance.ToString());
            }

            thought.Add("强攻校验失败 AP={0}", unit.Ap);
        }

        var encircleTarget = ResolveDirectiveAdjacentStronghold(unit, gameData, hostileStrongholds)
                             ?? hostileStrongholds
                                 .Where(s => unit.Location.IsAdjacent(s.Location) && !unit.Location.IsSameTile(s.Location))
                                 .OrderByDescending(s => s.ForceActor.Soldier)
                                 .FirstOrDefault();

        if (encircleTarget is not null)
        {
            encircleTarget = gameData.Strongholds[encircleTarget.Id];
            // 业务：Occupy 且方针锁定据点时，优先踏上城格再强攻，邻格不提前包围
            if (unit.Directive == UnitDirective.Occupy
                && ResolveDirectiveHostileStronghold(unit, gameData, hostileStrongholds) is { } directive
                && encircleTarget.Id == directive.Id
                && !unit.Location.IsSameTile(directive.Location))
            {
                thought.Add("方针目标 {0}：优先入城，本日不邻格包围", directive.Name);
                return StrategyAiDecision.Fail("AwaitOccupyEntry", "等待踏上方针目标城格", thought);
            }

            var encircleAp = ResolveAutoSiegeApCost(unit, encircleTarget, siegeApCost);
            thought.Add("邻格敌城 {0}，尝试包围（令AP={1}）", encircleTarget.Name, encircleAp);
            if (SiegeOrderRules.Validate(unit, encircleTarget, UnitSiegeMode.Encircle, gameData, encircleAp).IsSuccess)
            {
                SiegeOrderRules.Apply(
                    worldContext, unit, encircleTarget, UnitSiegeMode.Encircle, gameData, encircleAp, meta);
                return StrategyAiDecision.Ok(
                    "SiegeEncircle",
                    $"自动包围 {encircleTarget.Name}",
                    thought,
                    targetStrongholdId: encircleTarget.Id,
                    siegeMode: nameof(UnitSiegeMode.Encircle),
                    stance: unit.Stance.ToString());
            }

            thought.Add("包围校验失败 AP={0}", unit.Ap);
        }

        return StrategyAiDecision.Fail("NoSiegeOrder", "未满足自动攻城条件", thought);
    }

    /// <summary>从当主居城派遣 idle 部队援防受威胁友城（势力级，日初一次）。</summary>
    public static bool TryDispatchLordRelief(
        int forceId,
        GameData gameData,
        StrategyScenarioMeta meta,
        IPathfindingService pathfinding,
        IGameWorldContext worldContext,
        StrategyVisibilityLedger? visibility = null)
    {
        if (!StrategyAiControlRules.IsForceAiControlled(meta, forceId))
            return false;

        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(forceId, gameData, meta);
        if (residenceId <= 0
            || !gameData.Strongholds.TryGetValue(residenceId, out var residence))
            return false;

        var threatened = gameData.Strongholds.Values
            .Where(s => s.ForceId == forceId && s.Id != residenceId)
            .Select(s => new { Castle = s, Threats = GarrisonBehaviorRules.FindFieldBattleProximityThreats(s, gameData)
                .Count(u => visibility is null || visibility.IsVisible(forceId, u.Location.X, u.Location.Y)) })
            .Where(s => s.Threats > 0)
            .OrderByDescending(s => s.Threats).ThenBy(s => s.Castle.Id).Select(s => s.Castle)
            .ToList();

        if (threatened.Count == 0)
            return false;

        var reliefUnit = gameData.Units.Values
            .Where(u => u.ForceId == forceId
                        && u.IsMilitary
                        && u.Soldier >= 300
                        && u.Location.IsSameTile(residence.Location)
                        && u.SiegeMode == UnitSiegeMode.None
                        && !ShouldSkipDailyAi(u)
                        && u.Directive is UnitDirective.Move or UnitDirective.Occupy or UnitDirective.Support)
            .OrderByDescending(u => u.Soldier)
            .FirstOrDefault();

        if (reliefUnit is null)
            return false;

        foreach (var target in threatened)
        {
            if (reliefUnit.Location.IsSameTile(target.Location))
            {
                reliefUnit.Directive = UnitDirective.Support;
                reliefUnit.ActionTarget.StrongholdId = target.Id;
                reliefUnit.ActionTarget.UnitId = 0;
                return true;
            }

            var path = pathfinding.CalculatePath(reliefUnit, (Point2)target.Location);
            if (path is null || path.Count < 2)
                continue;

            if (!ReliefPathRules.IsTransitPathClear(reliefUnit, path, worldContext))
                continue;

            reliefUnit.Directive = UnitDirective.Support;
            reliefUnit.ActionTarget.StrongholdId = target.Id;
            reliefUnit.ActionTarget.UnitId = 0;
            reliefUnit.Status = UnitStatus.Moving;
            reliefUnit.ActionTarget.RoutePoints.Clear();
            foreach (var node in path.Skip(1))
                reliefUnit.ActionTarget.RoutePoints.Enqueue(node.Location);

            return true;
        }

        return false;
    }

    /// <summary>己方受威胁据点，按威胁程度排序。</summary>
    public static IReadOnlyList<Stronghold> ResolveThreatenedFriendlyStrongholds(Unit unit, GameData gameData,
        IReadOnlyList<Unit>? observedEnemies = null)
    {
        if (observedEnemies is not null)
            return gameData.Strongholds.Values.Where(s => s.ForceId == unit.ForceId)
                .Select(s => new { Castle = s, Threats = observedEnemies.Count(u =>
                    Manhattan((Point2)u.Location, (Point2)s.Location) <= GarrisonBehaviorRules.ThreatManhattanDistance) })
                .Where(s => s.Threats > 0).OrderByDescending(s => s.Threats)
                .ThenBy(s => Manhattan((Point2)unit.Location, (Point2)s.Castle.Location)).ThenBy(s => s.Castle.Id)
                .Select(s => s.Castle).ToArray();
        return ResolveLegacyThreatenedStrongholds(unit, gameData);
    }

    private static IReadOnlyList<Stronghold> ResolveLegacyThreatenedStrongholds(Unit unit, GameData gameData)
        => [.. gameData.Strongholds.Values
            .Where(s => s.ForceId == unit.ForceId)
            .Where(s => GarrisonBehaviorRules.IsStrongholdUnderAttack(s, gameData))
            .OrderByDescending(s => GarrisonBehaviorRules.IsStrongholdBlockaded(s, gameData))
            .ThenByDescending(s => GarrisonBehaviorRules.FindFieldBattleProximityThreats(s, gameData).Count)
            .ThenBy(s => Manhattan((Point2)unit.Location, (Point2)s.Location))];

    /// <summary>方针 <see cref="Unit.DirectiveTargetId"/> 指向的敌对据点（若存在且仍敌对）。</summary>
    public static Stronghold? ResolveDirectiveHostileStronghold(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Stronghold>? hostileStrongholds = null)
    {
        if (hostileStrongholds is not null)
            return hostileStrongholds.FirstOrDefault(s => s.Id == unit.DirectiveTargetId);
        if (unit.DirectiveTargetId <= 0
            || !gameData.Strongholds.TryGetValue(unit.DirectiveTargetId, out var target))
            return null;

        if (target.ForceId == unit.ForceId)
            return null;

        if (!gameData.Forces.TryGetValue(unit.ForceId, out var myForce)
            || !gameData.Forces.TryGetValue(target.ForceId, out var ownerForce)
            || !DiplomacyRules.IsEnemy(myForce, ownerForce).IsSuccess)
            return null;

        if (hostileStrongholds is not null && !hostileStrongholds.Any(s => s.Id == target.Id))
            return null;

        return target;
    }

    private static bool IsAtDirectiveStrongholdSiegeRange(Unit unit, Stronghold stronghold)
        => unit.Location.IsSameTile(stronghold.Location)
           || unit.Location.IsAdjacent(stronghold.Location);

    /// <summary>
    /// AI 自动攻城 AP：已踏敌城同格或方针锁定目标时免 AP（踏城即投入攻城，不应再另耗令 AP）。
    /// </summary>
    private static int ResolveAutoSiegeApCost(Unit unit, Stronghold target, int siegeApCost)
    {
        if (unit.Directive is UnitDirective.Occupy or UnitDirective.Raid
            && unit.Location.IsSameTile(target.Location))
            return 0;

        if (unit.DirectiveTargetId == target.Id
            && unit.Directive is UnitDirective.Occupy or UnitDirective.Raid)
            return 0;

        return siegeApCost;
    }

    private static Stronghold? ResolveDirectiveOnTileStronghold(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Stronghold> hostileStrongholds)
    {
        var directive = ResolveDirectiveHostileStronghold(unit, gameData, hostileStrongholds);
        return directive is not null && unit.Location.IsSameTile(directive.Location)
            ? directive
            : null;
    }

    private static Stronghold? ResolveDirectiveAdjacentStronghold(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Stronghold> hostileStrongholds)
    {
        var directive = ResolveDirectiveHostileStronghold(unit, gameData, hostileStrongholds);
        return directive is not null
               && unit.Location.IsAdjacent(directive.Location)
               && !unit.Location.IsSameTile(directive.Location)
            ? directive
            : null;
    }

    /// <summary>已踏上方针敌城格时清空越城路径，避免穿过目标格继续行军。</summary>
    private static bool TryStopAtDirectiveStronghold(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Stronghold> hostileStrongholds,
        StrategyAiThought thought)
    {
        var directive = ResolveDirectiveHostileStronghold(unit, gameData, hostileStrongholds);
        if (directive is null || !unit.Location.IsSameTile(directive.Location))
            return false;

        if (unit.ActionTarget.RoutePoints.Count == 0)
            return false;

        thought.Add("已踏上方针目标 {0}，停止沿路径穿过该城", directive.Name);
        unit.ActionTarget.RoutePoints.Clear();
        if (unit.Status == UnitStatus.Moving)
            unit.Status = UnitStatus.Waiting;
        return true;
    }

    /// <summary>
    /// 已踏敌城同格且尚未下达攻城令时固守待命，不再改奔其它据点（AP 不足亦如此）。
    /// </summary>
    private static StrategyAiDecision? TryHoldAwaitSiegeOnEnemyTile(
        Unit unit,
        GameData gameData,
        IReadOnlyList<Stronghold> hostileStrongholds,
        StrategyAiThought thought)
    {
        if (unit.Directive is not (UnitDirective.Occupy or UnitDirective.Raid))
            return null;

        if (unit.SiegeMode != UnitSiegeMode.None)
            return null;

        var onTile = ResolveDirectiveOnTileStronghold(unit, gameData, hostileStrongholds)
                     ?? hostileStrongholds.FirstOrDefault(s => unit.Location.IsSameTile(s.Location));
        if (onTile is null)
            return null;

        unit.ActionTarget.RoutePoints.Clear();
        if (unit.Status == UnitStatus.Moving)
            unit.Status = UnitStatus.Waiting;

        thought.Add("已踏敌城 {0}，本营固守待攻城（AP={1}）", onTile.Name, unit.Ap);
        return StrategyAiDecision.Ok(
            "SiegeAwaitOnTile",
            $"固守 {onTile.Name}，待攻城",
            thought,
            targetStrongholdId: onTile.Id);
    }

    /// <summary>统计指定格上敌方势力的驻军总数（城内兵+同格部队）。</summary>
    private static int CountGarrisonAt(GameData gameData, int x, int y, int forceId)
    {
        var stronghold = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == forceId && s.Location.X == x && s.Location.Y == y);

        if (stronghold is not null)
            return StrongholdGarrisonRules.CountTotalGarrisonAt(stronghold, gameData);

        return gameData.Units.Values
            .Where(u => u.IsMilitary
                        && u.Soldier > 0
                        && u.ForceId == forceId
                        && u.Location.X == x
                        && u.Location.Y == y)
            .Sum(u => u.Soldier);
    }

    /// <summary>寻路并将路径写入单位行动队列，进入移动状态。</summary>
    private static StrategyAiDecision QueuePath(
        Unit unit,
        IPathfindingService pathfinding,
        Point2 target,
        StrategyAiThought thought,
        string code,
        string message,
        int? targetStrongholdId = null)
    {
        if (SiegeOrderRules.IsSiegeMovementLocked(unit))
        {
            thought.Add("攻城令锁定，拒绝规划路径");
            return StrategyAiDecision.Fail("SiegeLocked", "攻城令期间不可移动", thought);
        }

        var path = pathfinding.CalculatePath(unit, target);
        if (path is null || path.Count < 2)
        {
            thought.Add("寻路失败→({0},{1})", target.X, target.Y);
            return StrategyAiDecision.Fail("PathFailed", $"寻路失败→({target.X},{target.Y})", thought);
        }

        unit.Status = UnitStatus.Moving;
        unit.ActionTarget.RoutePoints.Clear();

        foreach (var node in path.Skip(1))
            unit.ActionTarget.RoutePoints.Enqueue(node.Location);

        thought.Add("路径长度={0}（含起点）", path.Count);
        return StrategyAiDecision.Ok(code, message, thought, targetPoint: target, targetStrongholdId: targetStrongholdId);
    }

    /// <summary>查找接敌范围内的首个敌军（方针评估用）。</summary>
    private static Unit? FindEngagementRangeEnemy(Unit unit, IReadOnlyList<Unit> hostileUnits)
    {
        foreach (var hostile in hostileUnits)
        {
            if (MoveEngagementRules.IsInEngagementRange(unit, hostile))
                return hostile;
        }

        return null;
    }

    /// <summary>判断己方兵力是否低于敌军指定比例（兵力劣势）。</summary>
    private static bool IsOutnumbered(Unit self, Unit enemy, double minSelfToEnemyRatio)
    {
        if (enemy.Soldier <= 0)
            return false;

        return self.Soldier < enemy.Soldier * minSelfToEnemyRatio;
    }

    /// <summary>从四邻格中选取离敌军曼哈顿距离最大的一步（撤退一步）。</summary>
    private static Point2? FindBestRetreatStep(Point2 from, Point2 enemy)
    {
        Point2? best = null;
        var bestScore = int.MinValue;

        foreach (var step in Neighbors(from))
        {
            var score = Manhattan(step, enemy) * 10;
            if (score > bestScore)
            {
                bestScore = score;
                best = step;
            }
        }

        return best;
    }

    private static int Manhattan(Point2 a, Point2 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static IEnumerable<Point2> Neighbors(Point2 p)
    {
        yield return new Point2(p.X + 1, p.Y);
        yield return new Point2(p.X - 1, p.Y);
        yield return new Point2(p.X, p.Y + 1);
        yield return new Point2(p.X, p.Y - 1);
    }
}
