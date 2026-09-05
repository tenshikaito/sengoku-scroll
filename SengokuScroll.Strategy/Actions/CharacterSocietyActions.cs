using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>确定性人物社会日结算：有限记忆、关系选择、婚约回应、预警后个人投奔。</summary>
public static class CharacterSocietyActions
{
    public static void AdvanceDay(GameData data, StrategyScenarioMeta meta, StrategyDayOutcomeBuffer events)
    {
        var day = data.GameDate.TotalDays;
        foreach (var actor in data.Characters.Values.OrderBy(c => c.Id).ToArray())
        {
            if (actor.IsDead || actor.LastSocialAiDay == day) continue;
            actor.LastSocialAiDay = day;
            if (actor.MarriageProposalExpiryDay <= day)
            {
                CharacterSocialHistory.Record(actor, actor.PendingMarriageFromId, day, "MarriageExpired", "婚约过期");
                actor.PendingMarriageFromId = 0; actor.MarriageProposalExpiryDay = null;
            }
            var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(actor.ForceId, meta, data);
            // Never consent to marriage or spend AP on behalf of a human-controlled lord.
            var humanLord = actor.Id == lordId && (!StrategyAiControlRules.IsForceAiControlled(meta, actor.ForceId)
                || meta.HasHumanControlConfiguration && !meta.AllForcesAiControlled);
            if (humanLord || actor.ForceStatus != Character.CharacterForceStatus.Idle || actor.IsSick
                || actor.RecruitTask is not null || actor.DiplomacyMission is not null || actor.RecruitAssignment is not null)
                continue;

            if (actor.PendingMarriageFromId > 0 && data.Characters.TryGetValue(actor.PendingMarriageFromId, out var proposer)
                && CharacterSocialActions.AreCoLocated(actor, proposer))
            {
                if (CharacterMarriageActions.Eligible(data, actor, proposer) && Opinion(actor, proposer.Id, data) >= 50
                    && Trust(actor, proposer.Id, data) >= 25 && actor.Ap >= 2)
                {
                    CharacterMarriageActions.ProposeOrAccept(data, actor, proposer, out var marriageMessage);
                    Notify(events, proposer.ForceId, "Marriage", marriageMessage);
                    if (actor.ForceId != proposer.ForceId) Notify(events, actor.ForceId, "Marriage", marriageMessage);
                }
                else if (!CharacterMarriageActions.Eligible(data, actor, proposer) || Opinion(actor, proposer.Id, data) < 0)
                    CharacterMarriageActions.Decline(data, actor, proposer, out _);
            }

            if (actor.Id != lordId && lordId > 0 && day % 7 == actor.Id % 7)
            {
                var opinion = Opinion(actor, lordId, data);
                var change = opinion >= 50 ? 1 : opinion <= -50 ? -2 : 0;
                if (change != 0)
                {
                    actor.Loyalty = (byte)Math.Clamp(actor.Loyalty + change, 0, 100);
                    CharacterSocialHistory.Record(actor, lordId, day, "LoyaltyChanged",
                        change > 0 ? "信赖当主，忠诚提升" : "不满当主，忠诚下降");
                }
            }
            if (TryDefect(data, meta, events, actor, lordId)) continue;
            if (day % 7 != actor.Id % 7 || actor.Ap < 1 || actor.Hp < 50) continue;
            var target = data.Characters.Values.Where(c => c.Id != actor.Id && !c.IsDead
                && c.ForceId == actor.ForceId && c.ForceStatus == Character.CharacterForceStatus.Idle
                && CharacterSocialActions.AreCoLocated(actor, c) && Opinion(actor, c.Id, data) > -25)
                .OrderByDescending(c => Opinion(actor, c.Id, data) + Trust(actor, c.Id, data) / 2)
                .ThenBy(c => c.Id).FirstOrDefault();
            if (target is not null) CharacterSocialActions.PerformMeeting(data, actor, target, "Talk", out _);
        }
    }

    private static int Opinion(Character actor, int target, GameData data)
        => actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == target) is { } relationship
            ? CharacterRelationshipRules.Resolve(relationship, today: data.GameDate) : 0;
    private static int Trust(Character actor, int target, GameData data)
        => actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == target) is { } relationship
            ? CharacterRelationshipRules.Resolve(relationship, trust: true, today: data.GameDate) : 0;

    private static bool TryDefect(GameData data, StrategyScenarioMeta meta, StrategyDayOutcomeBuffer events,
        Character actor, int lordId)
    {
        var day = data.GameDate.TotalDays;
        var eligible = actor.Id != lordId && lordId > 0 && actor.Personality.Ambition >= 70
            && EntityEffectHelper.ResolveEffectiveLoyalty(actor, data.GameDate) <= 20 && Opinion(actor, lordId, data) <= -60;
        if (!eligible)
        {
            if (actor.DefectionWarningDay is not null)
            {
                CharacterSocialHistory.Record(actor, lordId, day, "DefectionCancelled", "不满缓解，取消投奔意向");
                Notify(events, actor.ForceId, "DefectionCancelled", $"{actor.Name} 暂时放弃投奔意向。");
            }
            actor.DefectionWarningDay = null; return false;
        }
        if (actor.DefectionWarningDay is null)
        {
            actor.DefectionWarningDay = day;
            CharacterSocialHistory.Record(actor, lordId, day, "DefectionWarning", "对当主强烈不满，可能在30日后投奔他家");
            Notify(events, actor.ForceId, "DefectionWarning", $"{actor.Name} 忠诚低且敌视当主；最早30日后可能投奔，请及时改善关系。");
            return false;
        }
        if (day - actor.DefectionWarningDay < 30 || actor.Position != 0 || actor.LeaderId != 0
            || actor.LocationType != Character.CharacterLocationType.Stronghold
            || data.Units.Values.Any(u => u.LeaderId == actor.Id)
            || data.SubUnits.Values.Any(u => u.LeaderId == actor.Id)
            || data.Forces.Values.Any(f => f.LordCharacterId == actor.Id || f.Successor == actor.Id)
            || data.Strongholds.Values.Any(s => s.LordId == actor.Id || s.LeaderId == actor.Id
                || s.ForceActor.CharacterIds.Contains(actor.Id) || s.CivilianActor.CharacterIds.Contains(actor.Id)
                || s.MerchantActors.Concat(s.ReligionActors).Any(a => a.CharacterIds.Contains(actor.Id)))) return false;
        var castleId = actor.LocationStrongholdId > 0 ? actor.LocationStrongholdId : actor.StrongholdId;
        if (!data.Strongholds.TryGetValue(castleId, out var castle) || castle.ForceId == actor.ForceId
            || !data.Forces.ContainsKey(castle.ForceId)) return false;
        var recipientLord = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(castle.ForceId, meta, data);
        if (recipientLord <= 0 || Opinion(actor, recipientLord, data) < 50) return false;
        var previousForce = actor.ForceId;
        actor.ForceId = castle.ForceId;
        actor.StrongholdId = castle.Id;
        actor.ServiceDate = data.GameDate;
        actor.Loyalty = 50; actor.DefectionWarningDay = null;
        actor.ActionTarget.RoutePoints.Clear(); actor.ActionTarget.CharacterId = 0;
        actor.ActionTarget.ForceId = castle.ForceId; actor.ActionTarget.StrongholdId = castle.Id;
        actor.ActionTarget.UnitId = 0; actor.IsReadyToMove = false;
        actor.ActionStatus = Character.CharacterActionStatus.Waiting;
        actor.ActionPlan = Character.CharacterActionPlan.Rest;
        actor.IntelTasks.Clear();
        CharacterSocialHistory.Record(actor, recipientLord, day, "Defected", "个人投奔，不携带军队或城池");
        Notify(events, previousForce, "CharacterDefected", $"{actor.Name} 已个人投奔他家，没有带走城池或军队。");
        Notify(events, actor.ForceId, "CharacterJoined", $"{actor.Name} 前来投奔。");
        return true;
    }

    private static void Notify(StrategyDayOutcomeBuffer events, int force, string category, string message)
        => events.AddEvent(new() { Category = category, Message = message, RecipientForceId = force });
}
