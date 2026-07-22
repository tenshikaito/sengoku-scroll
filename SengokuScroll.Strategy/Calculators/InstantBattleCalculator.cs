using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>瞬间战战力估算与确定性结算（M3-a，接入 <see cref="BattleFactorEvaluator"/>）。</summary>
public static class InstantBattleCalculator
{
    /// <summary>有效战力 = 兵数 × (攻+防)/20，未配置攻防时使用默认值。</summary>
    public static int ComputeEffectivePower(Unit unit)
    {
        var attack = unit.Attack > 0 ? unit.Attack : BattleConstants.DefaultCombatStat;
        var defense = unit.Defense > 0 ? unit.Defense : BattleConstants.DefaultCombatStat;
        return unit.Soldier * (attack + defense) / 20;
    }

    /// <summary>攻方胜率（5%～95%），仅兵数×攻防（无全因素，供简易测试）。</summary>
    public static int ComputeAttackerWinRatePercent(Unit attacker, Unit defender)
    {
        var atk = ComputeEffectivePower(attacker);
        var def = ComputeEffectivePower(defender);
        if (atk + def == 0)
            return 50;

        var rate = (double)atk / (atk + def) * 100;
        return (int)Math.Clamp(
            Math.Round(rate),
            BattleConstants.MinWinRatePercent,
            BattleConstants.MaxWinRatePercent);
    }

    /// <summary>含训练/士气/将领/态势等因素的攻方胜率。</summary>
    /// <summary>含训练/士气/将领/态势等因素的攻方胜率。</summary>
    public static int ComputeAttackerWinRatePercent(BattleEvaluationContext ctx)
        => BattleFactorEvaluator.ComputeAttackerWinRatePercent(ctx);

    /// <summary>构建决战结算用的评估上下文。</summary>
    public static BattleEvaluationContext CreateResolveContext(
        Unit attacker,
        Unit defender,
        GameData gameData,
        GameMapMasterData? mapMaster = null,
        int standoffDays = 0,
        BattleEngagementKind engagementKind = BattleEngagementKind.FieldBattle)
        => new()
        {
            Attacker = attacker,
            Defender = defender,
            GameData = gameData,
            MapMaster = mapMaster,
            Phase = BattleEvaluationPhase.Resolve,
            StandoffDays = standoffDays,
            EngagementKind = engagementKind
        };

    /// <summary>由本局种子、日期与参战方生成确定性掷点种子（联机/回放预埋）。</summary>
    public static int ComputeResolutionSeed(
        int simulationSeed,
        GameDate date,
        int attackerId,
        int defenderId,
        int targetX,
        int targetY)
    {
        var hash = HashCode.Combine(
            simulationSeed,
            date.Year,
            date.Month,
            date.Day,
            attackerId,
            defenderId,
            targetX,
            targetY);
        return hash == int.MinValue ? 0 : Math.Abs(hash);
    }

    /// <summary>兼容旧调用：未传入本局种子时等同 simulationSeed=0。</summary>
    public static int ComputeResolutionSeed(GameDate date, int attackerId, int defenderId, int targetX, int targetY)
        => ComputeResolutionSeed(0, date, attackerId, defenderId, targetX, targetY);

    /// <summary>估算伤亡区间（供战前预览）。</summary>
    public static (int AttackerMin, int AttackerMax, int DefenderMin, int DefenderMax) EstimateCasualtyRanges(
        Unit attacker,
        Unit defender,
        int attackerWinRatePercent,
        StrategyDifficulty difficulty = StrategyDifficulty.Normal)
    {
        var attackerWins = attackerWinRatePercent >= 50;
        if (attackerWins)
        {
            var (defMin, defMax, attMin, attMax) = BattleCasualtyRules.EstimateCasualtyRanges(
                attacker.Soldier,
                defender.Soldier,
                attackerWinRatePercent,
                difficulty);
            return (attMin, attMax, defMin, defMax);
        }

        var (attLoserMin, attLoserMax, defWinnerMin, defWinnerMax) = BattleCasualtyRules.EstimateCasualtyRanges(
            defender.Soldier,
            attacker.Soldier,
            100 - attackerWinRatePercent,
            difficulty);
        return (attLoserMin, attLoserMax, defWinnerMin, defWinnerMax);
    }

    /// <summary>按种子确定性结算野战结果（简化，不含全因素）。</summary>
    /// <summary>按种子确定性结算野战结果（简化，不含全因素）。</summary>
    public static InstantBattleOutcome Resolve(Unit attacker, Unit defender, int seed)
        => Resolve(CreateMinimalContext(attacker, defender), seed);

    /// <summary>按种子与全因素上下文结算野战结果（战术子单位模拟）。</summary>
    public static InstantBattleOutcome Resolve(BattleEvaluationContext ctx, int seed)
    {
        var tactical = TacticalBattleSimulator.Resolve(
            ctx.Attacker,
            ctx.Defender,
            ctx.GameData,
            seed,
            ctx.MapMaster);
        return tactical.Outcome;
    }

    /// <summary>战术模拟完整结果（战报过程 + 多单位伤亡）。</summary>
    public static TacticalBattleResult ResolveTactical(BattleEvaluationContext ctx, int seed, bool bothOrderedAttack = false, string? commitReason = null)
        => TacticalBattleSimulator.Resolve(
            ctx.Attacker,
            ctx.Defender,
            ctx.GameData,
            seed,
            ctx.MapMaster,
            bothOrderedAttack,
            commitReason);

    private static BattleEvaluationContext CreateMinimalContext(Unit attacker, Unit defender)
        => new()
        {
            Attacker = attacker,
            Defender = defender,
            GameData = new Domain.GameData
            {
                GameDate = new GameDate(1, 1, 1),
                Forces = [],
                Strongholds = [],
                Units = new Dictionary<int, Unit>
                {
                    [attacker.Id] = attacker,
                    [defender.Id] = defender
                },
                Characters = [],
                SupplyConvoys = [],
                MessageCarriers = [],
                SubUnits = []
            },
            Phase = BattleEvaluationPhase.Resolve
        };

    /// <summary>生成自动战斗过程叙述（供战报 UI）。</summary>
    public static IReadOnlyList<StrategyBattleLogEntryDto> BuildBattleLog(
        Unit attacker,
        Unit defender,
        InstantBattleOutcome outcome,
        bool bothOrderedAttack = false,
        bool attackerWonMovementInitiative = false,
        string? commitReason = null)
    {
        var logs = new List<StrategyBattleLogEntryDto>();
        var order = 0;

        void Add(string side, string phase, string message) =>
            logs.Add(new StrategyBattleLogEntryDto
            {
                Order = ++order,
                Side = side,
                Phase = phase,
                Message = message
            });

        Add("system", "接触", $"{attacker.Name} 与 {defender.Name} 在野外遭遇。");
        if (!string.IsNullOrWhiteSpace(commitReason))
            Add("system", "强袭", commitReason);
        if (bothOrderedAttack)
        {
            Add(
                "system",
                "先手",
                attackerWonMovementInitiative
                    ? $"{attacker.Name} 与 {defender.Name} 互下攻击令；{attacker.Name} 移动力较高先行动并担任攻方。"
                    : $"{attacker.Name} 与 {defender.Name} 互下攻击令；{attacker.Name} 先行动并担任攻方。");
        }

        Add("attacker", "接敌", $"{attacker.Name} 发起进攻（{outcome.AttackerSoldiersBefore} 名）。");
        Add("defender", "接敌", $"{defender.Name} 列阵应战（{outcome.DefenderSoldiersBefore} 名）。");
        Add(
            "system",
            "交锋",
            $"战前评估：攻方胜率 {outcome.AttackerWinRatePercent}%，判定值 {outcome.ResolutionRoll}（需 < {outcome.AttackerWinRatePercent} 则攻方胜）。");

        if (outcome.AttackerWon)
        {
            Add("attacker", "突破", "正面冲击奏效，敌军阵线动摇。");
            Add(
                "defender",
                "溃退",
                $"伤亡 {outcome.DefenderCasualties} 名，剩余 {Math.Max(0, outcome.DefenderSoldiersBefore - outcome.DefenderCasualties)} 名。");
            Add("attacker", "追击", $"乘势掩杀，己方伤亡 {outcome.AttackerCasualties} 名。");
            Add("system", "结束", "攻方获胜，当日野战结束。");
        }
        else
        {
            Add("defender", "反击", "守军顽强抵抗，攻势被遏止。");
            Add(
                "attacker",
                "受挫",
                $"损失 {outcome.AttackerCasualties} 名，剩余 {Math.Max(0, outcome.AttackerSoldiersBefore - outcome.AttackerCasualties)} 名后后撤。");
            Add("defender", "维持", $"守军伤亡 {outcome.DefenderCasualties} 名。");
            Add("system", "结束", "守方获胜，当日野战结束。");
        }

        return logs;
    }

    private static int PercentOf(int value, int percent)
    {
        if (value <= 0 || percent <= 0)
            return 0;

        var casualties = value * percent / 100;
        // 业务：小股部队百分比截断为 0 时保底 1 伤亡，避免残兵永远打不掉
        if (casualties == 0)
            return Math.Min(1, value);

        return casualties;
    }
}

/// <summary>瞬间战结算结果。</summary>
public readonly record struct InstantBattleOutcome(
    /// <summary>攻方是否获胜。</summary>
    bool AttackerWon,
    /// <summary>攻方胜率（百分点）。</summary>
    int AttackerWinRatePercent,
    /// <summary>攻方伤亡人数。</summary>
    int AttackerCasualties,
    /// <summary>守方伤亡人数。</summary>
    int DefenderCasualties,
    /// <summary>确定性结算种子。</summary>
    int ResolutionSeed,
    /// <summary>判定掷骰值（攻方胜需 roll &lt; 胜率）。</summary>
    int ResolutionRoll,
    /// <summary>开战前攻方兵数。</summary>
    int AttackerSoldiersBefore,
    /// <summary>开战前守方兵数。</summary>
    int DefenderSoldiersBefore,
    /// <summary>是否为劝降收编（零战损）。</summary>
    bool IsSurrendered = false);
