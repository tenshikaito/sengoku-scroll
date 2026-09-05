using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

public enum CharacterDecisionKind { Social, Marriage, Loyalty, Defection, Relief }

/// <summary>只含本人可知的标量，评分线程不持有任何世界实体引用。</summary>
public readonly record struct CharacterDecisionInput(int Opinion, int Trust, int Loyalty,
    int Temper, int Courage, int Principle, int Caution, int Friendship, int Ambition,
    int Emotion, int Hp, int Strategy, int RecentTalkPenalty, int RejectionPenalty);

public sealed record CharacterDecisionScore(int Score, int Threshold, CharacterDecisionFactor[] Factors)
{
    public bool Preferred => Score >= Threshold;
}

public static class CharacterDecisionRules
{
    private static int Stat(int value) => Math.Clamp(value, 0, 100);
    private static int Signed(int value) => Math.Clamp(value, -100, 100);

    public static CharacterDecisionInput Capture(Character actor, int targetId, GameData data)
    {
        var relation = actor.Relationships.FirstOrDefault(r => r.TargetCharacterId == targetId);
        var today = data.GameDate.TotalDays;
        var talks = 0; var rejection = 0;
        foreach (var memory in actor.SocialMemories)
        {
            var age = (long)today - memory.Day;
            if (memory.OtherCharacterId != targetId || age < 0) continue;
            if (memory.Kind == "Talk" && age < 7) talks += 7 - (int)age;
            if (memory.Kind == "MarriageDeclined" && age < 60)
                rejection = Math.Max(rejection, 12 - (int)age / 5);
        }
        var p = actor.Personality;
        return new(relation is null ? 0 : CharacterRelationshipRules.Resolve(relation, today: data.GameDate),
            relation is null ? 0 : CharacterRelationshipRules.Resolve(relation, trust: true, today: data.GameDate),
            EntityEffectHelper.ResolveEffectiveLoyalty(actor, data.GameDate),
            p.Temper, p.Courage, p.Principle, p.Action, p.Friendship, p.Ambition,
            actor.Emotion, actor.Hp, actor.Strategy, Math.Min(14, talks), rejection);
    }

    /// <summary>纯整数评分；各类行为独立权重，不把分数解释为成功概率。</summary>
    public static CharacterDecisionScore Evaluate(CharacterDecisionKind kind, CharacterDecisionInput input)
    {
        var opinion = Signed(input.Opinion); var trust = Signed(input.Trust);
        var friendship = Stat(input.Friendship) - 50; var ambition = Stat(input.Ambition) - 50;
        var caution = Stat(input.Caution) - 50; var temper = Stat(input.Temper) - 50;
        var emotion = Signed(input.Emotion);
        CharacterDecisionFactor[] factors = kind switch
        {
            CharacterDecisionKind.Social => [new("交往需要", 20), new("亲疏与信任", opinion / 2 + trust / 4),
                new("温和与重义", temper / 5 + friendship / 5), new("心情与体力", emotion / 10 - (100 - Stat(input.Hp)) / 5),
                new("近期重复交谈", -Math.Clamp(input.RecentTalkPenalty, 0, 14))],
            CharacterDecisionKind.Marriage => [new("亲疏", opinion * 3 / 4), new("信任", trust / 2),
                new("重义与慎重", friendship / 5 - caution / 5), new("心情", emotion / 10),
                new("近期拒绝经历", -Math.Clamp(input.RejectionPenalty, 0, 12))],
            CharacterDecisionKind.Loyalty => [new("对当主亲疏", opinion / 2), new("对当主信任", trust / 4),
                new("重义与野心", friendship / 5 - ambition / 5), new("心情", emotion / 10)],
            CharacterDecisionKind.Defection => [new("低忠诚", 50 - Stat(input.Loyalty)), new("对当主不满", -opinion / 2),
                new("野心", Stat(input.Ambition) / 2), new("既有情义纽带", -Stat(input.Friendship) / 2)],
            // Positive score increases the required win rate. Intelligence reduces uncertainty,
            // not devotion or courage. No hidden enemy personality enters this input.
            CharacterDecisionKind.Relief => [new("慎重", caution / 5), new("勇气", -(Stat(input.Courage) - 50) / 5),
                new("救援情义", -Math.Max(0, opinion) * Stat(input.Friendship) / 1000),
                new("判断不确定性", (100 - Stat(input.Strategy)) / 10)],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return new(factors.Sum(f => f.Value), kind switch
        {
            CharacterDecisionKind.Social => 10,
            CharacterDecisionKind.Marriage => 60,
            CharacterDecisionKind.Loyalty => 30,
            CharacterDecisionKind.Defection => 80,
            _ => 0
        }, factors);
    }

    public static void Remember(Character actor, int day, CharacterDecisionKind kind, int targetId,
        CharacterDecisionScore score, string outcome)
    {
        var behavior = kind.ToString();
        actor.RecentDecisions.RemoveAll(d => d.Behavior == behavior);
        actor.RecentDecisions.Add(new(day, behavior, targetId, score.Score, score.Threshold, outcome, score.Factors));
        if (actor.RecentDecisions.Count > 5)
            actor.RecentDecisions.RemoveRange(0, actor.RecentDecisions.Count - 5);
    }
}
