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
        var socialGroups = data.Characters.Values.Where(CanSocialize)
            .GroupBy(SocialLocation).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Id).ToArray());
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
            if (humanLord || !CanSocialize(actor))
                continue;

            if (actor.PendingMarriageFromId > 0 && data.Characters.TryGetValue(actor.PendingMarriageFromId, out var proposer)
                && CharacterSocialActions.AreCoLocated(actor, proposer))
            {
                var input = CharacterDecisionRules.Capture(actor, proposer.Id, data);
                var decision = CharacterDecisionRules.Evaluate(CharacterDecisionKind.Marriage, input);
                var eligible = CharacterMarriageActions.Eligible(data, actor, proposer);
                var outcome = "等待考虑或恢复行动力";
                if (eligible && input.Opinion >= 0 && input.Trust >= 0 && decision.Preferred && actor.Ap >= 2)
                {
                    var result = CharacterMarriageActions.ProposeOrAccept(data, actor, proposer, out var marriageMessage);
                    outcome = result.IsSuccess ? "同意婚约" : "婚约执行条件已变化";
                    if (result.IsSuccess)
                    {
                        Notify(events, proposer.ForceId, "Marriage", marriageMessage);
                        if (actor.ForceId != proposer.ForceId) Notify(events, actor.ForceId, "Marriage", marriageMessage);
                    }
                }
                else if (!eligible || input.Opinion < 0 || input.Trust < 0)
                {
                    CharacterMarriageActions.Decline(data, actor, proposer, out _);
                    outcome = "不合婚约条件或缺乏好感/信任，拒绝";
                }
                CharacterDecisionRules.Remember(actor, day, CharacterDecisionKind.Marriage, proposer.Id, decision, outcome);
            }

            if (actor.Id != lordId && lordId > 0 && day % 7 == actor.Id % 7)
            {
                var decision = CharacterDecisionRules.Evaluate(CharacterDecisionKind.Loyalty,
                    CharacterDecisionRules.Capture(actor, lordId, data));
                var change = decision.Preferred ? 1 : decision.Score <= -decision.Threshold ? -2 : 0;
                change = Math.Clamp(actor.Loyalty + change, 0, 100) - actor.Loyalty;
                CharacterDecisionRules.Remember(actor, day, CharacterDecisionKind.Loyalty, lordId, decision,
                    change > 0 ? "忠诚提升" : change < 0 ? "忠诚下降" : "忠诚保持");
                if (change != 0)
                {
                    actor.Loyalty = (byte)Math.Clamp(actor.Loyalty + change, 0, 100);
                    CharacterSocialHistory.Record(actor, lordId, day, "LoyaltyChanged",
                        change > 0 ? "信赖当主，忠诚提升" : "不满当主，忠诚下降");
                }
            }
            if (TryDefect(data, meta, events, actor, lordId)) continue;
            if (day % 7 != actor.Id % 7 || actor.Ap < 1 || actor.Hp < 50) continue;
            if (!socialGroups.TryGetValue(SocialLocation(actor), out var neighbors)) continue;
            var candidates = neighbors.Where(c => c.Id != actor.Id && CanSocialize(c)
                && c.ForceId == actor.ForceId && CharacterSocialActions.AreCoLocated(actor, c)
                && !TalkCoolingDown(actor, c, day))
                .Select(c => (Id: c.Id, Input: CharacterDecisionRules.Capture(actor, c.Id, data))).ToArray();
            var ranked = StrategyParallelWork.MapOrdered(candidates,
                c => (c.Id, Decision: CharacterDecisionRules.Evaluate(CharacterDecisionKind.Social, c.Input)),
                minimumParallelCount: 64);
            var selected = ranked.OrderByDescending(c => c.Decision.Score).ThenBy(c => c.Id).FirstOrDefault();
            if (selected.Decision is null) continue;
            var socialOutcome = "社交意愿不足，保持当前安排";
            if (selected.Decision.Preferred && data.Characters.TryGetValue(selected.Id, out var target))
            {
                var result = CharacterSocialActions.PerformMeeting(data, actor, target, "Talk", out _);
                socialOutcome = result.IsSuccess ? "主动交谈" : "会面条件已变化，未执行";
            }
            CharacterDecisionRules.Remember(actor, day, CharacterDecisionKind.Social, selected.Id, selected.Decision, socialOutcome);
        }
    }

    private static int Opinion(Character actor, int target, GameData data)
        => actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == target) is { } relationship
            ? CharacterRelationshipRules.Resolve(relationship, today: data.GameDate) : 0;
    private static bool CanSocialize(Character actor)
        => !actor.IsDead && !actor.IsSick && actor.ForceStatus == Character.CharacterForceStatus.Idle
           && actor.ActionStatus == Character.CharacterActionStatus.Waiting
           && actor.RecruitTask is null && actor.DiplomacyMission is null && actor.RecruitAssignment is null;

    private static (int Force, Character.CharacterLocationType Type, int A, int B) SocialLocation(Character c)
        => (c.ForceId, c.LocationType, c.LocationType switch
        {
            Character.CharacterLocationType.Stronghold => c.LocationStrongholdId > 0 ? c.LocationStrongholdId : c.StrongholdId,
            Character.CharacterLocationType.Unit => c.ActionTarget.UnitId,
            _ => c.Location.X
        }, c.LocationType == Character.CharacterLocationType.Map ? c.Location.Y : 0);

    private static bool TalkCoolingDown(Character a, Character b, int day)
        => a.Relationships.Any(r => r.TargetCharacterId == b.Id && r.LastTalkDay is int d && day - d < 1)
           || b.Relationships.Any(r => r.TargetCharacterId == a.Id && r.LastTalkDay is int d && day - d < 1);

    private static bool TryDefect(GameData data, StrategyScenarioMeta meta, StrategyDayOutcomeBuffer events,
        Character actor, int lordId)
    {
        var day = data.GameDate.TotalDays;
        if (actor.Id == lordId || lordId <= 0) return false;
        var input = CharacterDecisionRules.Capture(actor, lordId, data);
        var decision = CharacterDecisionRules.Evaluate(CharacterDecisionKind.Defection, input);
        // Hysteresis: entering intent takes 80 points; retaining it takes 65.
        var threshold = actor.DefectionWarningDay is null ? decision.Threshold : decision.Threshold - 15;
        decision = decision with { Threshold = threshold };
        var eligible = input.Loyalty <= 35 && input.Opinion < 0 && decision.Preferred;
        CharacterDecisionRules.Remember(actor, day, CharacterDecisionKind.Defection, lordId, decision,
            eligible ? "有投奔意向，仍须预警期和合法出路" : "留在当前势力");
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
