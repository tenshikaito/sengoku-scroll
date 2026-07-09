using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>瞬间战战力估算与确定性结算（M3-a 简化）。</summary>
/// <remarks>
/// <para>算法说明：</para>
/// <list type="bullet">
///   <item>有效战力 = 兵数 × (攻+防)/20，未配置攻防时使用默认值。</item>
///   <item>攻方胜率 = 攻方战力 / (攻+守) × 100，钳制在 5%～95%。</item>
///   <item>结算：由日期+参战方+目标格生成种子，roll &lt; 胜率则攻方胜；伤亡比例随胜负随机（同种子可复现）。</item>
/// </list>
/// </remarks>
public static class InstantBattleCalculator
{
    /// <summary>有效战力 = 兵数 × (攻+防) 权重。</summary>
    public static int ComputeEffectivePower(Unit unit)
    {
        var attack = unit.Attack > 0 ? unit.Attack : BattleConstants.DefaultCombatStat;
        var defense = unit.Defense > 0 ? unit.Defense : BattleConstants.DefaultCombatStat;
        return unit.Soldier * (attack + defense) / 20;
    }

    /// <summary>攻方胜率（5%～95%），仅基于双方有效战力比，不含 AP buff。</summary>
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

    /// <summary>由日期与参战方生成确定性种子（联机预埋）。</summary>
    public static int ComputeResolutionSeed(GameDate date, int attackerId, int defenderId, int targetX, int targetY)
    {
        var hash = HashCode.Combine(date.Year, date.Month, date.Day, attackerId, defenderId, targetX, targetY);
        return hash == int.MinValue ? 0 : Math.Abs(hash);
    }

    /// <summary>估算伤亡区间（供战前预览）。</summary>
    public static (int AttackerMin, int AttackerMax, int DefenderMin, int DefenderMax) EstimateCasualtyRanges(
        Unit attacker,
        Unit defender,
        int attackerWinRatePercent)
    {
        var attackerWins = attackerWinRatePercent >= 50;
        var attackerMin = attackerWins ? PercentOf(attacker.Soldier, 10) : PercentOf(attacker.Soldier, 30);
        var attackerMax = attackerWins ? PercentOf(attacker.Soldier, 25) : PercentOf(attacker.Soldier, 60);
        var defenderMin = attackerWins ? PercentOf(defender.Soldier, 30) : PercentOf(defender.Soldier, 10);
        var defenderMax = attackerWins ? PercentOf(defender.Soldier, 60) : PercentOf(defender.Soldier, 25);
        return (attackerMin, attackerMax, defenderMin, defenderMax);
    }

    /// <summary>按种子确定性结算野战结果。</summary>
    public static InstantBattleOutcome Resolve(Unit attacker, Unit defender, int seed)
    {
        var attackerSoldiersBefore = attacker.Soldier;
        var defenderSoldiersBefore = defender.Soldier;
        var winRate = ComputeAttackerWinRatePercent(attacker, defender);
        var rng = new Random(seed);
        var roll = rng.Next(100);
        var attackerWon = roll < winRate;

        var attackerLossPct = attackerWon ? rng.Next(10, 26) : rng.Next(30, 61);
        var defenderLossPct = attackerWon ? rng.Next(30, 61) : rng.Next(10, 26);

        var attackerCasualties = Math.Min(attacker.Soldier, PercentOf(attacker.Soldier, attackerLossPct));
        var defenderCasualties = Math.Min(defender.Soldier, PercentOf(defender.Soldier, defenderLossPct));

        return new InstantBattleOutcome(
            AttackerWon: attackerWon,
            AttackerWinRatePercent: winRate,
            AttackerCasualties: attackerCasualties,
            DefenderCasualties: defenderCasualties,
            ResolutionSeed: seed,
            ResolutionRoll: roll,
            AttackerSoldiersBefore: attackerSoldiersBefore,
            DefenderSoldiersBefore: defenderSoldiersBefore);
    }

    /// <summary>生成自动战斗过程叙述（供战报 UI）。</summary>
    public static IReadOnlyList<StrategyBattleLogEntryDto> BuildBattleLog(
        Unit attacker,
        Unit defender,
        InstantBattleOutcome outcome,
        bool bothOrderedAttack = false,
        bool attackerWonApInitiative = false)
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
        if (bothOrderedAttack)
        {
            Add(
                "system",
                "先手",
                attackerWonApInitiative
                    ? $"{attacker.Name} 与 {defender.Name} 互下攻击令；{attacker.Name} AP 较高先行动并担任攻方。"
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
        => Math.Max(0, value * percent / 100);
}

/// <summary>瞬间战结算结果。</summary>
public readonly record struct InstantBattleOutcome(
    bool AttackerWon,
    int AttackerWinRatePercent,
    int AttackerCasualties,
    int DefenderCasualties,
    int ResolutionSeed,
    int ResolutionRoll,
    int AttackerSoldiersBefore,
    int DefenderSoldiersBefore);
