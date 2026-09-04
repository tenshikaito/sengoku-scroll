using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>原子执行和谈条款，并结束战争、建立停战期。</summary>
public static class PeaceSettlementActions
{
    public static bool TryExecute(
        GameData gameData,
        GameMasterData gameMasterData,
        int proposerForceId,
        int targetForceId,
        PeaceSettlementTerms terms,
        out GameError? error)
    {
        error = null;
        if (!PeaceSettlementRules.TryBuildPreview(
                gameData,
                proposerForceId,
                targetForceId,
                terms,
                baseAcceptanceChance: 50,
                out _,
                out error))
        {
            return false;
        }

        var ceded = terms.CededStrongholdIds
            .Select(id => gameData.Strongholds[id])
            .ToList();
        if (ceded.Count > 0)
        {
            var fallback = gameData.Strongholds.Values
                .Where(s => s.ForceId == targetForceId && !terms.CededStrongholdIds.Contains(s.Id))
                .OrderBy(s => s.Id)
                .First();

            foreach (var stronghold in ceded)
                TransferCededStronghold(stronghold, fallback, proposerForceId, targetForceId, gameData, gameMasterData);
        }

        if (terms.ReparationsMoney > 0)
            TransferReparations(gameData, proposerForceId, targetForceId, terms.ReparationsMoney);

        if (!ForceDiplomacyActions.TrySetRelation(
                gameData,
                proposerForceId,
                targetForceId,
                Diplomacy.DiplomacyRelation.Neutral,
                out var relationError))
        {
            error = relationError ?? GameError.DiplomacyError.InvalidForce;
            return false;
        }

        if (terms.DemandOuterVassalage
            && !ForceDiplomacyActions.TryImposeOuterVassalage(
                gameData,
                proposerForceId,
                targetForceId,
                out var vassalError))
        {
            error = vassalError ?? "InvalidPeaceVassalage";
            return false;
        }

        return true;
    }

    private static void TransferCededStronghold(
        Stronghold stronghold,
        Stronghold fallback,
        int proposerForceId,
        int targetForceId,
        GameData gameData,
        GameMasterData gameMasterData)
    {
        foreach (var character in gameData.Characters.Values.Where(c =>
                     c.ForceId == targetForceId
                     && (c.StrongholdId == stronghold.Id || c.LocationStrongholdId == stronghold.Id)))
        {
            if (character.StrongholdId == stronghold.Id)
                character.StrongholdId = fallback.Id;
            if (character.LocationStrongholdId == stronghold.Id)
            {
                character.LocationStrongholdId = fallback.Id;
                character.LocationType = CharacterLocationType.Stronghold;
                character.Location = fallback.Location;
            }
        }

        foreach (var unit in gameData.Units.Values.Where(u =>
                     u.ForceId == targetForceId
                     && (u.HomeStrongholdId == stronghold.Id || u.LocationStrongholdId == stronghold.Id)))
        {
            if (unit.HomeStrongholdId == stronghold.Id)
                unit.HomeStrongholdId = fallback.Id;
            if (unit.LocationStrongholdId == stronghold.Id)
            {
                unit.LocationStrongholdId = fallback.Id;
                unit.Location = fallback.Location;
            }
        }

        StrongholdCaptureActions.TransferStrongholdOwnership(
            stronghold,
            proposerForceId,
            gameData,
            gameMasterData,
            siegeDamage: 0);
        BattlefieldContainerRules.CloseBattlefieldsForStronghold(stronghold, gameData);
    }

    private static void TransferReparations(
        GameData gameData,
        int proposerForceId,
        int targetForceId,
        int amount)
    {
        var remaining = amount;
        foreach (var stronghold in gameData.Strongholds.Values
                     .Where(s => s.ForceId == targetForceId)
                     .OrderByDescending(s => s.ForceActor.Money))
        {
            var payment = Math.Min(remaining, stronghold.ForceActor.Money);
            stronghold.ForceActor.Money -= payment;
            remaining -= payment;
            if (remaining == 0)
                break;
        }

        var receivingStronghold = gameData.Strongholds.Values
            .Where(s => s.ForceId == proposerForceId)
            .OrderByDescending(s => s.ForceActor.Money)
            .ThenBy(s => s.Id)
            .First();
        receivingStronghold.ForceActor.Money += amount - remaining;

        ForceEconomyActions.SyncForceTreasuryFromStrongholds(gameData.Forces[proposerForceId], gameData);
        ForceEconomyActions.SyncForceTreasuryFromStrongholds(gameData.Forces[targetForceId], gameData);
    }
}
