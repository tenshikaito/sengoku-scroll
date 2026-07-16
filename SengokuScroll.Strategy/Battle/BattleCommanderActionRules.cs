using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Battle;

/// <summary>将领在决战中的行动意图。</summary>
public enum BattleCommanderActionKind
{
    /// <summary>强攻——正面突击，输出高、承伤也高。</summary>
    Assault,
    /// <summary>坚守——稳固阵脚，降低输出与承伤。</summary>
    Hold,
    /// <summary>侧击——调度偏师打击翼后。</summary>
    Flank,
    /// <summary>鼓舞——亲临阵前提振士气。</summary>
    Rally,
    /// <summary>脱离——收缩战线，尝试撤出。</summary>
    Withdraw
}

/// <summary>双方将领行动判定（可每回合重判）。</summary>
public static class BattleCommanderActionRules
{
    public readonly record struct CommanderDecision(
        BattleCommanderActionKind Action,
        string Label,
        string Description,
        /// <summary>本回合己方输出倍率。</summary>
        double OwnDamageScale,
        /// <summary>本回合己方承伤倍率。</summary>
        double OwnTakenScale,
        /// <summary>本回合士气变动。</summary>
        int MoraleDelta);

    /// <summary>综合战况、性格与历史将令，判定本回合将领行动意图。</summary>
    public static CommanderDecision Decide(
        Character? commander,
        Unit unit,
        Character? enemyCommander,
        bool isAttacker,
        bool isSurrounded,
        int estimatedWinRatePercent,
        Random rng,
        double ownRemainingRatio = 1.0,
        double enemyRemainingRatio = 1.0,
        BattleCommanderActionKind? previous = null)
    {
        var name = CommanderName(commander, unit);

        // 业务：撤退方针、低士气或余兵 <35% 时强制脱离
        if (unit.Directive == Unit.UnitDirective.Retreat
            || unit.Morale < BattleConstants.LowMoraleEngageThreshold
            || ownRemainingRatio < 0.35)
        {
            return Make(
                BattleCommanderActionKind.Withdraw,
                "脱离",
                $"{name} 见己方折损过重，下令脱离战场。",
                0.70, 0.82, -3);
        }

        // 业务：被围攻守方——高智谋 60% 突围，高行动力死守，否则撤离
        if (isSurrounded && !isAttacker)
        {
            if (commander is not null && commander.Strategy >= 68 && rng.Next(100) < 60)
                return Make(BattleCommanderActionKind.Flank, "突围",
                    $"{name} 察破包围，指挥精锐向薄弱处突围。", 1.15, 1.05, 2);

            if (commander is not null && commander.Personality.Action >= 65)
                return Make(BattleCommanderActionKind.Hold, "死守",
                    $"{name} 令全军收缩阵型，死守待变。", 0.85, 0.88, 1);

            return Make(BattleCommanderActionKind.Withdraw, "突围撤离",
                $"{name} 判断已被围困，试图杀出重围。", 0.80, 0.95, -4);
        }

        // 业务：攻方兵力优势 +15% 且胜率 ≥55% 时，70% 概率延续强攻
        if (isAttacker && ownRemainingRatio > enemyRemainingRatio + 0.15 && estimatedWinRatePercent >= 55)
        {
            if (previous == BattleCommanderActionKind.Assault && rng.Next(100) < 70)
                return Make(BattleCommanderActionKind.Assault, "乘胜强攻",
                    $"{name} 见敌阵动摇，下令乘胜强攻。", 1.22, 1.06, 2);
        }

        // 业务：己方劣势 12% 以上时，高统率鼓舞或高行动力稳阵
        if (ownRemainingRatio + 0.12 < enemyRemainingRatio)
        {
            if (commander is not null && commander.Leadership >= 60 && unit.Morale < 55)
                return Make(BattleCommanderActionKind.Rally, "鼓舞",
                    $"{name} 亲临阵前鼓舞士气。", 1.05, 0.95, 6);

            if (commander is not null && commander.Personality.Action >= 60)
                return Make(BattleCommanderActionKind.Hold, "稳住阵脚",
                    $"{name} 见战况不利，令各队稳住阵脚。", 0.90, 0.88, 0);
        }

        if (unit.Morale < 50 && commander is not null && commander.Leadership >= 60
            && previous != BattleCommanderActionKind.Rally)
        {
            return Make(BattleCommanderActionKind.Rally, "鼓舞",
                $"{name} 亲临阵前鼓舞士气。", 1.05, 0.95, 6);
        }

        if (commander is not null)
        {
            var courage = commander.Personality.Courage;
            var ambition = commander.Personality.Ambition;
            var caution = commander.Personality.Action;
            var strategy = commander.Strategy;
            var enemyStrategy = enemyCommander?.Strategy ?? 50;

            // 业务：慎重将领攻势受阻（胜率 <48%）时改令坚守
            if (previous == BattleCommanderActionKind.Assault
                && caution >= 70
                && estimatedWinRatePercent < 48)
            {
                return Make(BattleCommanderActionKind.Hold, "改令坚守",
                    $"{name} 见攻势受阻，改令各队坚守。", 0.92, 0.90, 0);
            }

            if (caution >= 78 && (!isAttacker || estimatedWinRatePercent < 55))
                return Make(BattleCommanderActionKind.Hold, "慎重应战",
                    $"{name} 持重不躁，令各队稳固阵脚。", 0.92, 0.90, 0);

            if (strategy >= 70 && strategy > enemyStrategy + 5)
                return Make(BattleCommanderActionKind.Flank, "用计侧击",
                    $"{name} 以智谋调度，令偏师侧击敌军。", 1.18, 0.97, 1);

            if (courage >= 75 || (ambition >= 70 && isAttacker))
                return Make(BattleCommanderActionKind.Assault, "强攻",
                    $"{name} 意气风发，下令全军强攻。", 1.20, 1.08, 2);
        }

        if (isAttacker)
            return Make(BattleCommanderActionKind.Assault, "进攻",
                $"{name} 指挥本队发起进攻。", 1.08, 1.0, 0);

        return Make(BattleCommanderActionKind.Hold, "应战",
            $"{name} 列阵应战，等待战机。", 1.0, 0.95, 0);
    }

    /// <summary>将领行动的中文动词，用于战报续令叙述。</summary>
    public static string ActionVerb(BattleCommanderActionKind action) => action switch
    {
        BattleCommanderActionKind.Assault => "强攻",
        BattleCommanderActionKind.Hold => "坚守",
        BattleCommanderActionKind.Flank => "侧击",
        BattleCommanderActionKind.Rally => "鼓舞",
        BattleCommanderActionKind.Withdraw => "脱离",
        _ => "行动"
    };

    private static CommanderDecision Make(
        BattleCommanderActionKind action,
        string label,
        string description,
        double dmg,
        double taken,
        int morale)
        => new(action, label, description, dmg, taken, morale);

    private static string CommanderName(Character? commander, Unit unit)
        => commander?.Name ?? $"{unit.Name}主将";
}
