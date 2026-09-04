using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>判定相邻接敌当日是「对峙」还是「强袭决战」。</summary>
public static class BattleCommitRules
{
    /// <summary>当日决战判定结果：是否强袭、担任攻方单位、战斗因素分解。</summary>
    public readonly record struct CommitDecision(bool ShouldCommit, Unit? Aggressor, BattleFactorBreakdown? Breakdown);

    /// <summary>解析当日是否发起决战及担任攻方的一方。</summary>
    public static CommitDecision ResolveCommitSide(
        Unit roleAttacker,
        Unit roleDefender,
        int standoffDays,
        GameData gameData,
        GameMapMasterData? mapMaster = null,
        bool forceDecisivePursuit = false,
        Unit? pursuitAggressor = null,
        BattleEngagementKind engagementKind = BattleEngagementKind.FieldBattle)
    {
        var ctx = new BattleEvaluationContext
        {
            Attacker = roleAttacker,
            Defender = roleDefender,
            GameData = gameData,
            MapMaster = mapMaster,
            Phase = BattleEvaluationPhase.Commit,
            StandoffDays = standoffDays
        };

        var breakdown = BattleFactorEvaluator.Evaluate(ctx);

        // 业务：追击阶段强制决战，由追击方担任攻方；撤退方按日掷点可脱离
        if (forceDecisivePursuit && pursuitAggressor is not null)
        {
            var pursued = pursuitAggressor.Id == roleAttacker.Id ? roleDefender : roleAttacker;
            if (pursued.Directive == UnitDirective.Retreat
                && TryDisengageFromPursuit(pursued, pursuitAggressor, gameData))
            {
                return new CommitDecision(false, null, breakdown);
            }

            var aggressor = pursuitAggressor;
            var defender = aggressor.Id == roleAttacker.Id ? roleDefender : roleAttacker;
            return new CommitDecision(true, aggressor, breakdown);
        }

        var defenderEffective = SiegeBattleRules.IsSiegeEngagement(roleAttacker, roleDefender, gameData)
            ? SiegeBattleRules.EffectiveSiegeSoldierCount(roleAttacker, roleDefender, gameData)
            : roleDefender.Soldier;
        var combined = roleAttacker.Soldier + defenderEffective;

        // 业务：战斗因素强制决战且未阻止时，当日强袭
        if (breakdown.ForceCommit && !breakdown.BlockCommit)
            return new CommitDecision(true, roleAttacker, breakdown);

        // 业务：小股部队——试探期可对峙，超过试探天数或单方胜率达标则决战
        if (combined < BattleConstants.LargeArmySoldierThreshold)
        {
            if (breakdown.BlockCommit)
                return new CommitDecision(false, null, breakdown);

            if (standoffDays <= BattleConstants.SmallArmyProbeDays
                && !breakdown.ForceCommit
                && !ShouldSideCommitAssault(roleAttacker, roleDefender, ctx, breakdown)
                && !ShouldSideCommitAssault(roleDefender, roleAttacker, ctx, breakdown))
                return new CommitDecision(false, null, breakdown);

            return new CommitDecision(true, roleAttacker, breakdown);
        }

        // 业务：大军对峙超过强制决战天数后，由胜率更高方发起强袭
        if (standoffDays >= BattleConstants.StandoffForceBattleDays)
            return new CommitDecision(true, PickStrongerCommitSide(roleAttacker, roleDefender, ctx), breakdown);

        // 业务：攻城专用——强攻方针且胜率达标可提前决战；否则大军优先维持对峙
        if (engagementKind == BattleEngagementKind.Siege)
        {
            if (MoveEngagementRules.IsAggressiveDirective(roleAttacker.Directive)
                && !breakdown.BlockCommit
                && BattleFactorEvaluator.CanUnitEngage(roleAttacker)
                && standoffDays > BattleConstants.SmallArmyProbeDays)
            {
                var siegeRate = BattleFactorEvaluator.ComputeAdjustedCommitWinRate(ctx, selfIsAttacker: true);
                if (siegeRate >= BattleConstants.SiegeCommitWinRateThreshold)
                    return new CommitDecision(true, roleAttacker, breakdown);
            }

            if (SiegeBattleRules.ShouldPreferSiegeStandoff(roleAttacker, roleDefender, gameData, standoffDays)
                && !breakdown.ForceCommit
                && !ShouldSideCommitAssault(roleAttacker, roleDefender, ctx, breakdown)
                && !ShouldSideCommitAssault(roleDefender, roleAttacker, ctx, breakdown))
                return new CommitDecision(false, null, breakdown);
        }

        // 业务：任一方调整胜率达强袭阈值则由其发起决战
        if (ShouldSideCommitAssault(roleAttacker, roleDefender, ctx, breakdown))
            return new CommitDecision(true, roleAttacker, breakdown);

        if (ShouldSideCommitAssault(roleDefender, roleAttacker, ctx, breakdown))
            return new CommitDecision(true, roleDefender, breakdown);

        return new CommitDecision(false, null, breakdown);
    }

    /// <summary>计算攻方在当日决战情境下的调整后胜率（0–100）。</summary>
    public static int ComputeAdjustedAttackerWinRatePercent(
        Unit attacker,
        Unit defender,
        GameData gameData,
        GameMapMasterData? mapMaster = null,
        int standoffDays = 0)
    {
        var ctx = new BattleEvaluationContext
        {
            Attacker = attacker,
            Defender = defender,
            GameData = gameData,
            MapMaster = mapMaster,
            Phase = BattleEvaluationPhase.Commit,
            StandoffDays = standoffDays
        };

        return BattleFactorEvaluator.ComputeAttackerWinRatePercent(ctx);
    }

    private static bool ShouldSideCommitAssault(
        Unit self,
        Unit enemy,
        BattleEvaluationContext baseCtx,
        BattleFactorBreakdown breakdown)
    {
        if (breakdown.BlockCommit || !BattleFactorEvaluator.CanUnitEngage(self))
            return false;

        var adjusted = BattleFactorEvaluator.ComputeAdjustedCommitWinRate(baseCtx, self.Id == baseCtx.Attacker.Id);
        return adjusted >= BattleConstants.CommitAssaultWinRateThreshold;
    }

    private static Unit PickStrongerCommitSide(Unit a, Unit b, BattleEvaluationContext ctx)
    {
        var rateA = BattleFactorEvaluator.ComputeAdjustedCommitWinRate(ctx, selfIsAttacker: true);
        var rateB = BattleFactorEvaluator.ComputeAdjustedCommitWinRate(ctx, selfIsAttacker: false);
        return rateB > rateA ? b : a;
    }

    /// <summary>撤退方在追击压迫下掷点脱离（本局种子+日期确定性）。</summary>
    private static bool TryDisengageFromPursuit(Unit pursued, Unit pursuer, GameData gameData)
    {
        // 业务：基础脱离率约等同标准难度；士气与重整 AP 提高脱离成功
        var chance = StrategyDifficultyRules.PursuitDisengageChancePercent(StrategyDifficulty.Normal);
        chance += pursued.Morale / 5;
        chance += Math.Min(10, pursued.Ap);
        chance = Math.Clamp(chance, 10, 85);

        var roll = DeterministicHash.Combine(
            gameData.SimulationSeed,
            pursued.Id,
            pursuer.Id,
            gameData.GameDate.Year,
            gameData.GameDate.Month,
            gameData.GameDate.Day,
            0xD15E_AE) % 100;

        if (roll >= chance)
            return false;

        // 业务：脱离成功则清除追击方攻击令，避免当日再强制决战
        if (pursuer.ActionTarget.UnitId == pursued.Id)
        {
            pursuer.ActionTarget.UnitId = 0;
            if (pursuer.Stance == UnitStance.Attacking)
                pursuer.Stance = UnitStance.Normal;
        }

        return true;
    }
}
