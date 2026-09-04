using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>绝对优势时劝降：减少牺牲、收编敌军。</summary>
public static class BattleSurrenderRules
{
    /// <summary>是否应由优势方提出劝降（对峙或决战前）。</summary>
    public static bool ShouldOfferSurrender(
        Unit aggressor,
        Unit defender,
        GameData gameData,
        GameMapMasterData? mapMaster,
        int standoffDays,
        out int acceptChancePercent,
        out string reason)
    {
        acceptChancePercent = 0;
        reason = "";

        if (aggressor.Soldier <= 0 || defender.Soldier <= 0)
            return false;

        if (aggressor.Directive == UnitDirective.Retreat)
            return false;

        // 业务：追击中的败军更易劝降；死守/高士气则难
        var winRate = BattleEngagementScorer.ScoreCommitWinRate(
            aggressor, defender, gameData, mapMaster, standoffDays);

        if (winRate < BattleConstants.SurrenderOfferWinRateThreshold)
            return false;

        // 业务：兵力绝对优势（至少配置倍率）或胜率极高时才提出劝降
        var strengthOk = aggressor.Soldier >= defender.Soldier * BattleConstants.SurrenderMinStrengthRatio
                         || winRate >= BattleConstants.SurrenderAbsoluteWinRateThreshold;

        if (!strengthOk)
            return false;

        acceptChancePercent = ComputeAcceptChance(aggressor, defender, gameData, winRate, standoffDays);
        if (acceptChancePercent < BattleConstants.SurrenderMinAcceptChanceToOffer)
            return false;

        reason =
            $"{aggressor.Name} 评估胜率约 {winRate}%、兵力优势明显，向 {defender.Name} 劝降（预计接受率 {acceptChancePercent}%）";
        return true;
    }

    /// <summary>掷点判定守方是否接受劝降（确定性种子，接受率上限 95%）。</summary>
    public static bool RollSurrenderAccepted(int acceptChancePercent, int seed)
    {
        var roll = DeterministicHash.Combine(seed, unchecked((int)0x5A11_E11D)) % 100;
        return roll < Math.Clamp(acceptChancePercent, 0, 95);
    }

    /// <summary>劝降成功：零伤亡，攻方胜。</summary>
    public static InstantBattleOutcome CreateSurrenderOutcome(
        Unit aggressor,
        Unit defender,
        int winRatePercent,
        int seed,
        int acceptRoll)
        => new(
            AttackerWon: true,
            AttackerWinRatePercent: winRatePercent,
            AttackerCasualties: 0,
            DefenderCasualties: 0,
            ResolutionSeed: seed,
            ResolutionRoll: acceptRoll,
            AttackerSoldiersBefore: aggressor.Soldier,
            DefenderSoldiersBefore: defender.Soldier,
            IsSurrendered: true);

    /// <summary>综合胜率、士气、补给、性格与对峙天数，计算守方接受劝降的概率（5–92%）。</summary>
    public static int ComputeAcceptChance(
        Unit aggressor,
        Unit defender,
        GameData gameData,
        int winRate,
        int standoffDays)
    {
        var chance = BattleConstants.SurrenderAcceptBasePercent;

        // 业务：胜率越高越易降
        chance += (winRate - BattleConstants.SurrenderOfferWinRateThreshold) / 2;

        // 业务：守方士气低、恐惧、撤退方针
        chance += (50 - defender.Morale) / 3;
        if (defender.Status == UnitStatus.Fearful)
            chance += 15;
        if (defender.Directive == UnitDirective.Retreat)
            chance += 12;

        // 业务：补给断绝
        var supply = SupplyStatusEvaluator.EvaluateStatus(defender, gameData);
        if (supply == SupplyStatusEvaluator.CutOff)
            chance += 18;
        else if (supply == SupplyStatusEvaluator.Strained)
            chance += 8;

        // 业务：对峙日久 → 厌战
        chance += Math.Min(10, standoffDays / 3);

        gameData.Characters.TryGetValue(aggressor.LeaderId, out var atkCmd);
        gameData.Characters.TryGetValue(defender.LeaderId, out var defCmd);

        if (atkCmd is not null)
        {
            // 业务：政治/魅力促成劝降
            chance += (atkCmd.Politics + atkCmd.Charm) / 20;
        }

        if (defCmd is not null)
        {
            // 业务：守将勇猛/野心高则拒降
            chance -= defCmd.Personality.Courage / 12;
            chance -= defCmd.Personality.Ambition / 15;
            chance += (100 - defCmd.Personality.Action) / 25;
        }

        // 业务：兵力比悬殊时接受率额外上升
        if (defender.Soldier > 0)
        {
            var ratio = (double)aggressor.Soldier / defender.Soldier;
            if (ratio >= 3)
                chance += 15;
            else if (ratio >= 2)
                chance += 8;
        }

        return Math.Clamp(chance, 5, 92);
    }
}
