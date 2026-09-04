using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Rules;

/// <summary>策略模式战争分数：野战、劝降与据点占领。</summary>
public static class StrategyWarScoreRules
{
    public static int RecordBattleOutcome(
        GameData gameData,
        Unit attacker,
        Unit defender,
        InstantBattleOutcome outcome,
        bool surrendered = false)
    {
        var war = WarRules.FindActiveWarBetween(attacker.ForceId, defender.ForceId, gameData);
        if (war is null)
            return 0;

        var winner = outcome.AttackerWon ? attacker : defender;
        var loser = outcome.AttackerWon ? defender : attacker;
        var loserBefore = Math.Max(1, outcome.AttackerWon
            ? outcome.DefenderSoldiersBefore
            : outcome.AttackerSoldiersBefore);
        var loserCasualties = outcome.AttackerWon
            ? outcome.DefenderCasualties
            : outcome.AttackerCasualties;
        var casualtyRatioPercent = Math.Clamp(loserCasualties * 100 / loserBefore, 0, 100);

        // 业务：普通胜利 5 分；按败方损失最多再加 8 分；整建制劝降/溃灭再加 2 分。
        var score = 5 + casualtyRatioPercent * 8 / 100;
        if (surrendered || loser.Soldier <= 0)
            score += 2;
        score = Math.Clamp(score, 5, 15);

        return WarRules.AddWarScore(
            war,
            winner.ForceId,
            loser.ForceId,
            score,
            gameData.GameDate,
            "BattleVictory",
            winner.Id,
            $"{winner.Name} 击败 {loser.Name}");
    }

    public static int RecordStrongholdOccupation(
        GameData gameData,
        Stronghold stronghold,
        int occupierForceId,
        int previousForceId)
    {
        var war = WarRules.FindActiveWarBetween(occupierForceId, previousForceId, gameData);
        if (war is null)
            return 0;

        // 业务：据点规模 1～30 直接映射为设计规定的 10～30 分。
        var score = Math.Clamp(9 + Math.Max(1, (int)stronghold.Scale), 10, 30);
        return WarRules.AddWarScore(
            war,
            occupierForceId,
            previousForceId,
            score,
            gameData.GameDate,
            "StrongholdOccupied",
            stronghold.Id,
            $"占领 {stronghold.Name}");
    }
}
