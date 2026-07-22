using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Policies.Battle;

namespace SengokuScroll.Strategy.Battle;

/// <summary>子队目标选择评分（替代随机挑敌）。</summary>
public static class BattleTargetScoring
{
    /// <summary>在候选敌军中按战术评分选取最优打击目标（含轻微随机抖动）。</summary>
    public static T PickBestTarget<T>(
        IReadOnlyList<T> enemies,
        Func<T, int> soldiers,
        Func<T, int> defense,
        Func<T, byte> typeId,
        Func<T, BattleFormationSlot> slot,
        Func<T, bool> isCommanderParent,
        BattleFormationSlot actorSlot,
        byte actorTypeId,
        BattleCommanderActionKind commanderAction,
        Random rng)
        where T : class
    {
        T? best = null;
        var bestScore = int.MinValue;

        foreach (var enemy in enemies)
        {
            if (soldiers(enemy) <= 0)
                continue;

            var score = Score(
                soldiers(enemy),
                defense(enemy),
                typeId(enemy),
                slot(enemy),
                isCommanderParent(enemy),
                actorSlot,
                actorTypeId,
                commanderAction);

            // 业务：±3 随机抖动，避免同分目标永远固定
            score += rng.Next(0, 4);

            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best ?? enemies.First(e => soldiers(e) > 0);
    }

    /// <summary>单目标战术评分：残血、阵位距离、兵种克制与将领意图综合加权。</summary>
    public static int Score(
        int enemySoldiers,
        int enemyDefense,
        byte enemyTypeId,
        BattleFormationSlot enemySlot,
        bool enemyIsCommanderParent,
        BattleFormationSlot actorSlot,
        byte actorTypeId,
        BattleCommanderActionKind commanderAction)
    {
        var score = 40;

        // 业务：残血优先集火——<30 人 +25 分，<60 人 +12 分
        if (enemySoldiers < 30) score += 25;
        else if (enemySoldiers < 60) score += 12;

        // 业务：低防易打，防御每低于 18 一点加 1 分
        score += Math.Max(0, 18 - enemyDefense);

        // 业务：近战按阵位距离衰减；远程可打后军，2 格内加分
        var dist = BattleFormationSlotRules.SlotDistance(actorSlot, enemySlot);
        if (BattleFormationSlotRules.IsRanged(actorTypeId))
            score += dist <= 2 ? 10 : -5;
        else
            score += dist switch
            {
                0 => 18,
                1 => 10,
                2 => 0,
                _ => -20
            };

        // 业务：骑兵克制远程 +22 分，侧翼位 +10 分
        if (BattleFormationSlotRules.IsCavalry(actorTypeId))
        {
            if (BattleFormationSlotRules.IsRanged(enemyTypeId))
                score += 22;
            if (BattleFormationSlotRules.IsFlank(enemySlot))
                score += 10;
        }

        // 远程优先打前军密集
        if (BattleFormationSlotRules.IsRanged(actorTypeId) && enemySlot == BattleFormationSlot.Front)
            score += 14;

        score = CommanderActionScoringRegistry.Apply(
            commanderAction,
            score,
            enemySlot,
            enemyIsCommanderParent,
            actorSlot,
            dist);

        if (enemyIsCommanderParent)
            score += 12; // 业务：主队将领所在子队额外加分

        return score;
    }
}
