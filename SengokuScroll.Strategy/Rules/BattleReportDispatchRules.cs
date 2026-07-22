using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Rules;

/// <summary>战报信使派遣条件（减少 AI 反复对战时的信使 spam）。</summary>
public static class BattleReportDispatchRules
{
    /// <summary>是否应为该势力派出战报信使。</summary>
    public static bool ShouldDispatchBattleReport(
        int forceId,
        InstantBattleOutcome outcome,
        Unit attacker,
        Unit defender,
        int playerForceId,
        GameData gameData,
        IReadOnlyList<int>? attackerParticipantIds = null,
        IReadOnlyList<int>? defenderParticipantIds = null)
    {
        // 业务：已有在途战报信使时不再重复派遣
        if (HasInFlightBattleReport(gameData, forceId))
            return false;

        var playerInvolved = IsPlayerForceInvolved(
            playerForceId,
            attacker,
            defender,
            gameData,
            attackerParticipantIds,
            defenderParticipantIds);
        // 业务：玩家参战方始终收到战报
        if (playerInvolved)
            return forceId == attacker.ForceId || forceId == defender.ForceId;

        // 业务：纯 AI 对战仅在大胜（伤亡比≥20%）时通知胜方
        return IsDecisiveAiVictory(forceId, outcome, attacker, defender);
    }

    /// <summary>玩家势力是否有部队参与决战（含驰援）。</summary>
    public static bool IsPlayerForceInvolved(
        int playerForceId,
        Unit attacker,
        Unit defender,
        GameData gameData,
        IReadOnlyList<int>? attackerParticipantIds = null,
        IReadOnlyList<int>? defenderParticipantIds = null)
    {
        if (attacker.ForceId == playerForceId || defender.ForceId == playerForceId)
            return true;

        foreach (var id in attackerParticipantIds ?? [])
        {
            if (gameData.Units.TryGetValue(id, out var unit) && unit.ForceId == playerForceId)
                return true;
        }

        foreach (var id in defenderParticipantIds ?? [])
        {
            if (gameData.Units.TryGetValue(id, out var unit) && unit.ForceId == playerForceId)
                return true;
        }

        return false;
    }

    /// <summary>大军对峙是否应发出僵持通报（仅玩家相关对峙）。</summary>
    public static bool ShouldDispatchStandoffReport(
        int forceId,
        Unit unitA,
        Unit unitB,
        int playerForceId,
        GameData gameData)
    {
        if (HasInFlightBattleReport(gameData, forceId))
            return false;

        var playerInvolved = unitA.ForceId == playerForceId || unitB.ForceId == playerForceId;
        return playerInvolved && (forceId == unitA.ForceId || forceId == unitB.ForceId);
    }

    private static bool IsDecisiveAiVictory(
        int forceId,
        InstantBattleOutcome outcome,
        Unit attacker,
        Unit defender)
    {
        var attackerWon = outcome.AttackerWon;
        if (attackerWon && attacker.ForceId != forceId)
            return false;
        if (!attackerWon && defender.ForceId != forceId)
            return false;

        var loserBefore = attackerWon ? outcome.DefenderSoldiersBefore : outcome.AttackerSoldiersBefore;
        var loserCasualties = attackerWon ? outcome.DefenderCasualties : outcome.AttackerCasualties;
        if (loserBefore <= 0)
            return false;

        // 业务：败方伤亡占战前兵力 20% 以上视为「决定性胜利」，值得发信
        return loserCasualties * 100 / loserBefore >= 20;
    }

    private static bool HasInFlightBattleReport(GameData gameData, int forceId)
        => gameData.MessageCarriers.Values.Any(m =>
            m.ForceId == forceId
            && m.Payload.Type == MessagePayloadType.BattleReport
            && m.Status == MessageCarrierStatus.Moving);
}
