using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>野战自动战斗：每日接敌，当日结果可为「对峙」或「决战」。</summary>
public static class FieldBattleAutoResolver
{
    public enum FieldBattleDayKind
    {
        /// <summary>双方接触但未强袭，战线僵持。</summary>
        Standoff,

        /// <summary>一方判定时机成熟，发起决战并由 <see cref="InstantBattleCalculator"/> 结算。</summary>
        Decisive,

        /// <summary>绝对优势方劝降成功，零伤亡收编。</summary>
        Surrender
    }

    public readonly record struct FieldBattleDayResult(
        FieldBattleDayKind Kind,
        /// <summary>累计对峙日数（含当日）。</summary>
        int StandoffDays,
        Unit CommittedAggressor,
        Unit CommittedDefender,
        InstantBattleOutcome? Outcome,
        /// <summary>强袭方评估胜率（百分点）。</summary>
        int AdjustedCommitWinRatePercent,
        string? CommitReason,
        BattleEngagementKind EngagementKind = BattleEngagementKind.FieldBattle,
        BattleFactorBreakdown? FactorBreakdown = null,
        TacticalBattleResult? TacticalResult = null);

    /// <summary>结算相邻两军当日的自动战斗结果。</summary>
    public static FieldBattleDayResult ResolveDailyEngagement(
        GameDate date,
        Unit roleAttacker,
        Unit roleDefender,
        int standoffDaysBeforeToday,
        GameData gameData,
        GameMapMasterData? mapMaster = null,
        bool isPursuitEngagement = false,
        bool bothOrderedAttack = false)
    {
        var standoffDays = standoffDaysBeforeToday + 1;
        var engagementKind = BattleEngagementClassifier.Classify(roleAttacker, roleDefender, gameData);

        // 绝对优势劝降：在强袭前尝试，减少无谓牺牲
        if (TryResolveSurrender(
                date,
                roleAttacker,
                roleDefender,
                standoffDays,
                gameData,
                mapMaster,
                engagementKind,
                out var surrenderResult))
        {
            return surrenderResult;
        }

        var commit = BattleCommitRules.ResolveCommitSide(
            roleAttacker,
            roleDefender,
            standoffDays,
            gameData,
            mapMaster,
            isPursuitEngagement,
            isPursuitEngagement ? roleAttacker : null,
            engagementKind);

        if (!commit.ShouldCommit || commit.Aggressor is null)
        {
            // 业务：双方均未达强袭阈值，当日对峙
            return new FieldBattleDayResult(
                FieldBattleDayKind.Standoff,
                standoffDays,
                roleAttacker,
                roleDefender,
                null,
                0,
                BuildStandoffReason(roleAttacker, roleDefender, standoffDays, gameData),
                engagementKind);
        }

        var aggressor = commit.Aggressor;
        var defender = aggressor.Id == roleAttacker.Id ? roleDefender : roleAttacker;

        // 决战前再试一次劝降（胜率已达强袭阈值）
        if (TryResolveSurrender(
                date,
                aggressor,
                defender,
                standoffDays,
                gameData,
                mapMaster,
                engagementKind,
                out surrenderResult))
        {
            return surrenderResult;
        }

        var target = (Common.Types.Point2)defender.Location;
        var adjustedWinRate = BattleCommitRules.ComputeAdjustedAttackerWinRatePercent(
            aggressor,
            defender,
            gameData,
            mapMaster,
            standoffDays);
        var resolveCtx = InstantBattleCalculator.CreateResolveContext(
            aggressor,
            defender,
            gameData,
            mapMaster,
            standoffDays,
            engagementKind);
        var factorBreakdown = BattleFactorEvaluator.Evaluate(resolveCtx);
        var seed = InstantBattleCalculator.ComputeResolutionSeed(
            gameData.SimulationSeed,
            date,
            aggressor.Id,
            defender.Id,
            target.X,
            target.Y);
        var commitReason = BuildCommitReason(aggressor, defender, adjustedWinRate, gameData);
        var tactical = TacticalBattleSimulator.Resolve(
            aggressor,
            defender,
            gameData,
            seed,
            mapMaster,
            bothOrderedAttack,
            commitReason,
            factorBreakdown);

        return new FieldBattleDayResult(
            FieldBattleDayKind.Decisive,
            standoffDays,
            aggressor,
            defender,
            tactical.Outcome,
            adjustedWinRate,
            commitReason,
            engagementKind,
            factorBreakdown,
            tactical);
    }

    private static bool TryResolveSurrender(
        GameDate date,
        Unit candidateA,
        Unit candidateB,
        int standoffDays,
        GameData gameData,
        GameMapMasterData? mapMaster,
        BattleEngagementKind engagementKind,
        out FieldBattleDayResult result)
    {
        result = default;

        // 双方各评估一次：谁优势谁劝降
        if (TrySurrenderAsAggressor(
                date, candidateA, candidateB, standoffDays, gameData, mapMaster, engagementKind, out result))
            return true;

        return TrySurrenderAsAggressor(
            date, candidateB, candidateA, standoffDays, gameData, mapMaster, engagementKind, out result);
    }

    private static bool TrySurrenderAsAggressor(
        GameDate date,
        Unit aggressor,
        Unit defender,
        int standoffDays,
        GameData gameData,
        GameMapMasterData? mapMaster,
        BattleEngagementKind engagementKind,
        out FieldBattleDayResult result)
    {
        result = default;

        if (!BattleSurrenderRules.ShouldOfferSurrender(
                aggressor,
                defender,
                gameData,
                mapMaster,
                standoffDays,
                out var acceptChance,
                out var offerReason))
        {
            return false;
        }

        var target = (Common.Types.Point2)defender.Location;
        var seed = InstantBattleCalculator.ComputeResolutionSeed(
            gameData.SimulationSeed,
            date,
            aggressor.Id,
            defender.Id,
            target.X,
            target.Y);
        var acceptRoll = Math.Abs(HashCode.Combine(seed, 0x5A11_E11D)) % 100;
        if (!BattleSurrenderRules.RollSurrenderAccepted(acceptChance, seed))
            return false;

        var winRate = BattleEngagementScorer.ScoreCommitWinRate(
            aggressor, defender, gameData, mapMaster, standoffDays);
        var outcome = BattleSurrenderRules.CreateSurrenderOutcome(
            aggressor, defender, winRate, seed, acceptRoll);

        result = new FieldBattleDayResult(
            FieldBattleDayKind.Surrender,
            standoffDays,
            aggressor,
            defender,
            outcome,
            winRate,
            $"{offerReason}；{defender.Name} 接受劝降。",
            engagementKind);
        return true;
    }

    /// <summary>生成对峙日战报叙述。</summary>
    public static IReadOnlyList<StrategyBattleLogEntryDto> BuildStandoffLog(
        Unit unitA,
        Unit unitB,
        int standoffDays,
        string? reason)
    {
        return
        [
            new StrategyBattleLogEntryDto
            {
                Order = 1,
                Side = "system",
                Phase = "接触",
                Message = $"{unitA.Name} 与 {unitB.Name} 接敌，当日列阵对峙（第 {standoffDays} 日）。"
            },
            new StrategyBattleLogEntryDto
            {
                Order = 2,
                Side = "system",
                Phase = "对峙",
                Message = reason ?? "双方均未发动强袭，战线僵持。"
            },
            new StrategyBattleLogEntryDto
            {
                Order = 3,
                Side = "system",
                Phase = "结束",
                Message = "当日未分胜负，明日继续对峙或择机强袭。"
            }
        ];
    }

    private static string BuildStandoffReason(
        Unit roleAttacker,
        Unit roleDefender,
        int standoffDays,
        GameData gameData)
    {
        var atkRate = BattleCommitRules.ComputeAdjustedAttackerWinRatePercent(
            roleAttacker,
            roleDefender,
            gameData);
        var defRate = BattleCommitRules.ComputeAdjustedAttackerWinRatePercent(
            roleDefender,
            roleAttacker,
            gameData);

        return $"攻方评估强袭胜率约 {atkRate}%、守方反击评估约 {defRate}%，均未达强袭阈值（{BattleConstants.CommitAssaultWinRateThreshold}%）；{(SiegeBattleRules.IsSiegeEngagement(roleAttacker, roleDefender, gameData) ? "攻城" : "大军")}对峙第 {standoffDays} 日。";
    }

    /// <summary>构建强袭理由：胜率、敌补给与携粮天数。</summary>
    private static string BuildCommitReason(
        Unit aggressor,
        Unit defender,
        int adjustedWinRate,
        GameData gameData)
    {
        var parts = new List<string> { $"评估强袭胜率约 {adjustedWinRate}%" };

        var enemySupply = SupplyStatusEvaluator.EvaluateStatus(defender, gameData);
        if (enemySupply != SupplyStatusEvaluator.Sufficient)
            parts.Add($"敌军补给{SupplyStatusLabel(enemySupply)}");

        var foodDays = SupplyStatusEvaluator.EstimateFoodDaysRemaining(defender);
        // 业务：敌携粮 ≤7 日时写入强袭理由
        if (foodDays <= 7)
            parts.Add($"敌军携粮约 {foodDays} 日");

        parts.Add($"{aggressor.Name} 判定时机成熟，发起强袭");
        return string.Join("；", parts) + "。";
    }

    private static string SupplyStatusLabel(string status) => status switch
    {
        SupplyStatusEvaluator.CutOff => "断绝",
        SupplyStatusEvaluator.Strained => "紧张",
        _ => "充足"
    };
}
