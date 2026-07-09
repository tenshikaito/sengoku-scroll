using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Strategy.Systems;

/// <summary>瞬间战预览与执行（M3-a 野战：相邻敌军单位）。</summary>
public sealed class StrategyInstantBattleSystem(
    IGameContext context,
    UnitAttackEvaluator attackEvaluator)
{
    /// <summary>战前预览：校验攻击合法性并估算胜率/伤亡区间。</summary>
    public GameResult<StrategyBattlePreviewDto> Preview(int attackerUnitId, Point2 target)
    {
        var validation = ValidateFieldBattle(attackerUnitId, target, out var attacker, out var defender);
        if (!validation)
            return validation.Error!;

        return BuildPreview(attacker!, defender!, target);
    }

    /// <summary>执行瞬间战并写回单位状态。</summary>
    public GameResult<(StrategyBattlePreviewDto Preview, InstantBattleOutcome Outcome)> Execute(
        int attackerUnitId,
        Point2 target)
    {
        var validation = ValidateFieldBattle(attackerUnitId, target, out var attacker, out var defender);
        if (!validation)
            return validation.Error!;

        var preview = BuildPreview(attacker!, defender!, target);
        var outcome = InstantBattleCalculator.Resolve(attacker!, defender!, preview.ResolutionSeed);

        UnitBattleActions.ApplyCasualties(attacker!, outcome.AttackerCasualties);
        UnitBattleActions.ApplyCasualties(defender!, outcome.DefenderCasualties);
        UnitBattleActions.MarkAttacked(attacker!, context.GameRuleConfig);

        return (preview, outcome);
    }

    private GameResult ValidateFieldBattle(
        int attackerUnitId,
        Point2 target,
        out Unit? attacker,
        out Unit? defender)
    {
        attacker = null;
        defender = null;

        if (!context.GameWorldContext.GameWorld.GameData.Units.TryGetValue(attackerUnitId, out attacker))
            return UnitError.UnitNotFound;

        defender = context.GameWorldContext.GetUnitOrDefault(target);
        if (defender is null)
            return UnitError.AttackTargetNotFound;

        if (defender.Id == attacker.Id)
            return UnitError.AttackTargetNotFound;

        return attackEvaluator.Evaluate(attacker, target);
    }

    private StrategyBattlePreviewDto BuildPreview(Unit attacker, Unit defender, Point2 target)
    {
        var date = context.GameWorldContext.GameWorld.GameData.GameDate;
        var winRate = InstantBattleCalculator.ComputeAttackerWinRatePercent(attacker, defender);
        var seed = InstantBattleCalculator.ComputeResolutionSeed(
            date,
            attacker.Id,
            defender.Id,
            target.X,
            target.Y);
        var (attMin, attMax, defMin, defMax) =
            InstantBattleCalculator.EstimateCasualtyRanges(attacker, defender, winRate);

        return new StrategyBattlePreviewDto
        {
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            TargetX = target.X,
            TargetY = target.Y,
            AttackerWinRatePercent = winRate,
            AttackerSoldiers = attacker.Soldier,
            DefenderSoldiers = defender.Soldier,
            DefenderName = defender.Name,
            EstimatedAttackerLossMin = attMin,
            EstimatedAttackerLossMax = attMax,
            EstimatedDefenderLossMin = defMin,
            EstimatedDefenderLossMax = defMax,
            ResolutionSeed = seed
        };
    }
}
