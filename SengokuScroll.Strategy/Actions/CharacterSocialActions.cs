using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>人物会面互动：产生双向关系变化，并由当主支付行动力/赠礼。</summary>
public static class CharacterSocialActions
{
    public const int TalkApCost = 1;
    public const int GiftApCost = 2;
    public const int GiftMoneyCost = 100;

    public static GameResult TryInteract(
        GameData gameData,
        StrategyScenarioMeta meta,
        int actorId,
        int targetId,
        string interaction,
        out string message)
    {
        message = string.Empty;
        interaction ??= string.Empty;
        if (!gameData.Characters.TryGetValue(actorId, out var actor)
            || !gameData.Characters.TryGetValue(targetId, out var target)
            || actor.IsDead
            || target.IsDead
            || actor.Id == target.Id)
        {
            return GameError.CharacterError.CharacterNotFound;
        }

        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        if (meta.AllForcesAiControlled || actor.Id != lordId || actor.ForceId != meta.PlayerForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (!AreCoLocated(actor, target))
            return GameError.DomesticError.CharacterNotAtStronghold;

        if (actor.ForceStatus == CharacterForceStatus.Prisoner || target.ForceStatus == CharacterForceStatus.Prisoner)
            return new GameError("SocialPrisonerUnavailable");

        if (interaction.Equals("Marry", StringComparison.OrdinalIgnoreCase))
            return CharacterMarriageActions.ProposeOrAccept(gameData, actor, target, out message);
        if (interaction.Equals("DeclineMarriage", StringComparison.OrdinalIgnoreCase))
            return CharacterMarriageActions.Decline(gameData, actor, target, out message);

        return PerformMeeting(gameData, actor, target, interaction, out message);
    }

    // Internal shared rules used by validated player commands and daily NPC decisions.
    internal static GameResult PerformMeeting(GameData gameData, Character actor, Character target,
        string interaction, out string message)
    {
        message = string.Empty;
        if (!AreCoLocated(actor, target) || actor.IsDead || target.IsDead || actor.Id == target.Id
            || actor.ForceStatus == CharacterForceStatus.Prisoner || target.ForceStatus == CharacterForceStatus.Prisoner)
            return new GameError("SocialUnavailable");

        var normalized = interaction.Trim();
        var isGift = normalized.Equals("Gift", StringComparison.OrdinalIgnoreCase);
        if (!isGift && !normalized.Equals("Talk", StringComparison.OrdinalIgnoreCase))
            return GameError.DataNotFound;

        var today = gameData.GameDate.TotalDays;
        var existing = actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == target.Id);
        var reverse = target.Relationships.FirstOrDefault(r => r.TargetCharacterId == actor.Id);
        var cooldown = isGift ? 7 : 1;
        if (new[] { existing, reverse }.Any(r => (isGift ? r?.LastGiftDay : r?.LastTalkDay) is int last
            && today - last < cooldown)) return new GameError("SocialCooldown", cooldown);

        var apCost = isGift ? GiftApCost : TalkApCost;
        if (actor.Ap < apCost)
            return GameError.ApNotEnough;
        if (isGift && actor.Money < GiftMoneyCost)
            return GameError.MarketError.TradeNotFilled;
        if (isGift && target.Money > int.MaxValue - GiftMoneyCost)
            return new GameError("SocialRecipientTreasuryFull");

        actor.Ap -= apCost;
        if (isGift)
        {
            actor.Money -= GiftMoneyCost;
            target.Money += GiftMoneyCost;
        }

        var charmBonus = Math.Clamp((actor.Charm - 50) / 20, -1, 2);
        var hostilePenalty = actor.EnemyIds.Contains(target.Id) || target.EnemyIds.Contains(actor.Id) ? 3 : 0;
        var giftPreference = isGift ? (target.Personality.Desire - 50) / 25 : 0;
        var forwardRelationship = Math.Max(1, (isGift ? 8 : 4) + charmBonus - hostilePenalty);
        var reverseRelationship = Math.Max(1, (isGift ? 5 : 3) + charmBonus + giftPreference - hostilePenalty);
        var forwardTrust = Math.Max(1, (isGift ? 5 : 2) - hostilePenalty);
        var reverseTrust = Math.Max(1, (isGift ? 3 : 1) - hostilePenalty);

        if (existing?.Relationship >= 75) { forwardRelationship = 1; forwardTrust = 1; }
        if (reverse?.Relationship >= 75) { reverseRelationship = 1; reverseTrust = 1; }
        ApplyRelationship(actor, target.Id, forwardRelationship, forwardTrust);
        ApplyRelationship(target, actor.Id, reverseRelationship, reverseTrust);
        foreach (var entry in new[] { actor.Relationships.First(r => r.TargetCharacterId == target.Id),
            target.Relationships.First(r => r.TargetCharacterId == actor.Id) })
        { if (isGift) entry.LastGiftDay = today; else entry.LastTalkDay = today; }
        CharacterSocialHistory.Record(actor, target.Id, today, isGift ? "GiftSent" : "Talk", isGift ? "赠送金钱礼物" : "交谈");
        CharacterSocialHistory.Record(target, actor.Id, today, isGift ? "GiftReceived" : "Talk", isGift ? "收到金钱礼物" : "交谈");
        actor.Emotion = Math.Clamp(actor.Emotion + 1, -100, 100);
        target.Emotion = Math.Clamp(target.Emotion + (isGift ? 3 : 1), -100, 100);

        message = isGift
            ? $"{actor.Name} 向 {target.Name} 赠礼，双方关系有所增进"
            : $"{actor.Name} 与 {target.Name} 交谈，彼此更加熟悉";
        return GameResult.Ok();
    }

    internal static bool AreCoLocated(Character actor, Character target)
    {
        if (actor.LocationType != target.LocationType)
            return false;

        return actor.LocationType switch
        {
            CharacterLocationType.Stronghold => ResolveStrongholdId(actor) > 0
                && ResolveStrongholdId(actor) == ResolveStrongholdId(target),
            CharacterLocationType.Map => actor.Location.X == target.Location.X
                && actor.Location.Y == target.Location.Y,
            CharacterLocationType.Unit => actor.ActionTarget.UnitId > 0
                && actor.ActionTarget.UnitId == target.ActionTarget.UnitId,
            _ => false,
        };
    }

    private static int ResolveStrongholdId(Character character)
        => character.LocationStrongholdId > 0
            ? character.LocationStrongholdId
            : character.StrongholdId;

    internal static void ApplyRelationship(Character owner, int targetId, int relationshipDelta, int trustDelta)
    {
        var relationship = owner.Relationships.FirstOrDefault(entry => entry.TargetCharacterId == targetId);
        if (relationship is null)
        {
            relationship = new CharacterRelationship
            {
                OwnerCharacterId = owner.Id,
                TargetCharacterId = targetId,
            };
            owner.Relationships.Add(relationship);
        }

        relationship.Relationship = (sbyte)Math.Clamp(
            relationship.Relationship + relationshipDelta,
            -100,
            100);
        relationship.Trust = (sbyte)Math.Clamp(
            relationship.Trust + trustDelta,
            -100,
            100);
    }
}
