using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>私人关系唯一有效值规则；身份标签与当前亲疏独立，方向为 owner → target。</summary>
public static class CharacterRelationshipRules
{
    public static bool IsActive(EntityEffect effect, GameDate? today)
        => effect.Duration != EffectDurationKind.Temporary || today is null
            || effect.ExpiresOn is GameDate expiry && expiry.TotalDays > today.Value.TotalDays;

    public static int Resolve(CharacterRelationship relationship, bool trust = false, GameDate? today = null)
    {
        var stat = trust ? EffectTargetStat.Trust : EffectTargetStat.Relationship;
        var delta = relationship.ViewEffects.Where(e => IsActive(e, today)
            && (e.TargetStat == stat || !trust && e.TargetStat == EffectTargetStat.Diplomacy))
            .Sum(e => (long)e.Magnitude);
        return (int)Math.Clamp((trust ? relationship.Trust : relationship.Relationship) + delta, -100L, 100L);
    }

    public static string Tone(int value) => value switch
    {
        >= 75 => "亲密", >= 25 => "友好", > -25 => "普通", > -75 => "险恶", _ => "仇视"
    };
}
