using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Diplomacy;
using static SengokuScroll.Domain.Entities.Force;

namespace SengokuScroll.Strategy.Actions;

/// <summary>外交（对他国）与外政（对本家内藩）状态变更。</summary>
public static class ForceDiplomacyActions
{
    public const ushort DefaultTruceDays = 180;

    public static bool TrySetRelation(
        GameData gameData,
        int forceId,
        int targetForceId,
        DiplomacyRelation relation,
        out string? error)
    {
        error = null;
        if (forceId == targetForceId)
        {
            error = "SelfForce";
            return false;
        }

        if (!gameData.Forces.ContainsKey(forceId) || !gameData.Forces.ContainsKey(targetForceId))
        {
            error = "ForceNotFound";
            return false;
        }

        var activeWars = gameData.Wars.Values
            .Where(w => !w.IsEnded && WarRules.AreOnOppositeSides(w, forceId, targetForceId))
            .ToList();
        var wasAtWar = activeWars.Count > 0
            || gameData.Forces[forceId].Diplomacies.Any(d =>
                d.TargetForceId == targetForceId && d.Relation == DiplomacyRelation.Enemy);

        var forward = UpsertDiplomacy(gameData, forceId, targetForceId, relation);
        var reverse = UpsertDiplomacy(gameData, targetForceId, forceId, relation);

        if (relation == DiplomacyRelation.Enemy)
        {
            forward.IsTruce = false;
            forward.TrucePeriod = 0;
            reverse.IsTruce = false;
            reverse.TrucePeriod = 0;
            WarRules.EnsureWarBetween(gameData, forceId, targetForceId, gameData.GameDate);
        }
        else if (wasAtWar)
        {
            foreach (var war in activeWars)
            {
                var forceIsAggressor = WarRules.IsOnAggressorSide(war, forceId);
                var opponents = (forceIsAggressor ? war.DefenderForceIds : war.AggressorForceIds).ToList();
                var endsWholeWar = forceId == war.AggressorForceId || forceId == war.DefenderForceId;
                WarRules.SeparatePeace(war, forceId, gameData.GameDate);

                if (endsWholeWar)
                {
                    foreach (var aggressorId in war.AggressorForceIds)
                    foreach (var defenderId in war.DefenderForceIds)
                        SetNeutralTrucePair(gameData, aggressorId, defenderId);

                    foreach (var battlefield in gameData.Battlefields.Values
                                 .Where(b => !b.IsClosed && b.WarId == war.Id)
                                 .ToList())
                    {
                        BattlefieldContainerRules.CloseBattlefield(battlefield, gameData);
                    }
                }
                else
                {
                    foreach (var opponentId in opponents)
                        SetNeutralTrucePair(gameData, forceId, opponentId);

                    DetachForceFromWarBattlefields(gameData, war.Id, forceId);
                }
            }

            if (activeWars.Count == 0)
                SetNeutralTrucePair(gameData, forceId, targetForceId);
            else
            {
                forward.IsTruce = true;
                forward.TrucePeriod = DefaultTruceDays;
                reverse.IsTruce = true;
                reverse.TrucePeriod = DefaultTruceDays;
            }
        }

        return true;
    }

    public static bool TryImposeOuterVassalage(
        GameData gameData,
        int suzerainForceId,
        int targetForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, suzerainForceId, out var suzerain)
            || !ForceDiplomacyRules.TryGetForce(gameData, targetForceId, out var target))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanImposeVassalage(suzerain, target))
        {
            error = "InvalidVassalage";
            return false;
        }

        target.Status = ForceStatus.OuterVassal;
        target.SuzerainForceId = suzerainForceId;
        BindVassalDiplomacy(gameData, suzerainForceId, targetForceId);
        return true;
    }

    public static bool TrySubmitOuterVassalage(
        GameData gameData,
        int submitterForceId,
        int suzerainForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, submitterForceId, out var submitter)
            || !ForceDiplomacyRules.TryGetForce(gameData, suzerainForceId, out var suzerain))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanSubmitVassalage(submitter, suzerain))
        {
            error = "InvalidVassalage";
            return false;
        }

        submitter.Status = ForceStatus.OuterVassal;
        submitter.SuzerainForceId = suzerainForceId;
        BindVassalDiplomacy(gameData, suzerainForceId, submitterForceId);
        return true;
    }

    public static bool TryReleaseVassal(
        GameData gameData,
        int suzerainForceId,
        int vassalForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, suzerainForceId, out var suzerain)
            || !ForceDiplomacyRules.TryGetForce(gameData, vassalForceId, out var vassal))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanReleaseVassal(suzerain, vassal))
        {
            error = "InvalidVassalage";
            return false;
        }

        GrantIndependence(gameData, vassal);
        return true;
    }

    public static bool TryDeclareIndependence(
        GameData gameData,
        int vassalForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, vassalForceId, out var vassal))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanDeclareIndependence(vassal))
        {
            error = "InvalidVassalage";
            return false;
        }

        GrantIndependence(gameData, vassal);
        return true;
    }

    /// <summary>外政：任命内藩（本家将领分离为 InnerVassal 势力）。</summary>
    public static bool TryAppointInnerVassal(
        GameData gameData,
        int suzerainForceId,
        int targetForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, suzerainForceId, out var suzerain)
            || !ForceDiplomacyRules.TryGetForce(gameData, targetForceId, out var target))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanAppointInnerVassal(suzerain, target))
        {
            error = "InvalidInnerVassal";
            return false;
        }

        target.Status = ForceStatus.InnerVassal;
        target.SuzerainForceId = suzerainForceId;
        UpsertDiplomacy(gameData, suzerainForceId, targetForceId, DiplomacyRelation.Neutral);
        UpsertDiplomacy(gameData, targetForceId, suzerainForceId, DiplomacyRelation.Neutral);
        return true;
    }

    /// <summary>外政：撤销内藩，恢复独立。</summary>
    public static bool TryRevokeInnerVassal(
        GameData gameData,
        int suzerainForceId,
        int vassalForceId,
        out string? error)
    {
        error = null;
        if (!ForceDiplomacyRules.TryGetForce(gameData, suzerainForceId, out var suzerain)
            || !ForceDiplomacyRules.TryGetForce(gameData, vassalForceId, out var vassal))
        {
            error = "ForceNotFound";
            return false;
        }

        if (!ForceDiplomacyRules.CanRevokeInnerVassal(suzerain, vassal))
        {
            error = "InvalidInnerVassal";
            return false;
        }

        GrantIndependence(gameData, vassal);
        return true;
    }

    private static void GrantIndependence(GameData gameData, Force vassal)
    {
        var formerSuzerain = vassal.SuzerainForceId;
        vassal.Status = ForceStatus.Independence;
        vassal.SuzerainForceId = null;

        if (formerSuzerain is int suzerainId)
        {
            ClearSuzerainBinding(gameData, suzerainId, vassal.Id);
            ClearSuzerainBinding(gameData, vassal.Id, suzerainId);
        }
    }

    private static void BindVassalDiplomacy(GameData gameData, int suzerainForceId, int vassalForceId)
    {
        UpsertDiplomacy(gameData, suzerainForceId, vassalForceId, DiplomacyRelation.Neutral, suzerainForceId);
        UpsertDiplomacy(gameData, vassalForceId, suzerainForceId, DiplomacyRelation.Neutral, suzerainForceId);
    }

    private static Diplomacy UpsertDiplomacy(
        GameData gameData,
        int forceId,
        int targetForceId,
        DiplomacyRelation relation,
        int? suzerainId = null)
    {
        if (!gameData.Forces.TryGetValue(forceId, out var force))
            throw new InvalidOperationException($"Force {forceId} does not exist.");

        var existing = force.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId);
        if (existing is null)
        {
            force.Diplomacies.Add(new Diplomacy
            {
                ForceId = forceId,
                TargetForceId = targetForceId,
                Relation = relation,
                SuzerainId = suzerainId
            });
            return force.Diplomacies[^1];
        }

        existing.Relation = relation;
        existing.SuzerainId = suzerainId;
        return existing;
    }

    private static void ClearSuzerainBinding(GameData gameData, int forceId, int targetForceId)
    {
        if (!gameData.Forces.TryGetValue(forceId, out var force))
            return;

        var dip = force.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId);
        if (dip is not null)
            dip.SuzerainId = null;
    }

    private static void SetNeutralTrucePair(GameData gameData, int forceId, int targetForceId)
    {
        if (forceId == targetForceId)
            return;

        var forward = UpsertDiplomacy(gameData, forceId, targetForceId, DiplomacyRelation.Neutral);
        var reverse = UpsertDiplomacy(gameData, targetForceId, forceId, DiplomacyRelation.Neutral);
        forward.IsTruce = true;
        forward.TrucePeriod = DefaultTruceDays;
        reverse.IsTruce = true;
        reverse.TrucePeriod = DefaultTruceDays;
    }

    private static void DetachForceFromWarBattlefields(GameData gameData, int warId, int forceId)
    {
        foreach (var battlefield in gameData.Battlefields.Values
                     .Where(b => !b.IsClosed && b.WarId == warId)
                     .ToList())
        {
            var leavingUnitIds = battlefield.SideAUnitIds
                .Concat(battlefield.SideBUnitIds)
                .Distinct()
                .Where(id => gameData.Units.TryGetValue(id, out var unit) && unit.ForceId == forceId)
                .ToList();
            battlefield.SideAUnitIds.RemoveAll(leavingUnitIds.Contains);
            battlefield.SideBUnitIds.RemoveAll(leavingUnitIds.Contains);
            foreach (var unitId in leavingUnitIds)
            {
                if (gameData.Units.TryGetValue(unitId, out var unit))
                    BattlefieldContainerRules.LeaveBattlefield(unit);
            }

            var sideAActive = battlefield.SideAUnitIds.Any(id =>
                gameData.Units.TryGetValue(id, out var unit) && unit.Soldier > 0);
            var sideBActive = battlefield.SideBUnitIds.Any(id =>
                gameData.Units.TryGetValue(id, out var unit) && unit.Soldier > 0);
            if (!sideAActive || !sideBActive)
                BattlefieldContainerRules.CloseBattlefield(battlefield, gameData);
        }
    }
}
