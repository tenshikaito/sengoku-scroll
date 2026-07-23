using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 实体增减益映射、文案格式化与 Loyalty 等派生计算。
/// </summary>
/// <remarks>
/// 看法条目展示须选用对应 formatter：
/// FormatTargetStat（影响 Tab）、FormatDiplomacyViewTargetStat（势力看法）、FormatCharacterViewTargetStat（角色看法）。
/// </remarks>
public static class EntityEffectHelper
{
    /// <summary>汇总指定 TargetStat 的 Magnitude 代数和。</summary>
    public static int SumMagnitude(IEnumerable<EntityEffect> effects, EffectTargetStat target)
        => effects.Where(e => e.TargetStat == target).Sum(e => e.Magnitude);

    /// <summary>基础忠诚 + ActiveEffects 中 Loyalty 条目叠加，钳制 0–100。</summary>
    public static byte ResolveEffectiveLoyalty(Character character)
    {
        var baseValue = character.Loyalty;
        var delta = SumMagnitude(character.ActiveEffects, EffectTargetStat.Loyalty);
        return (byte)Math.Clamp(baseValue + delta, 0, 100);
    }

    /// <summary>将 Duration + Magnitude 格式化为 UI「程度」列（如「永久 = -100」）。</summary>
    public static string FormatDuration(EffectDurationKind duration, int magnitude)
        => duration switch
        {
            EffectDurationKind.Permanent => $"永久 = {FormatSigned(magnitude)}",
            EffectDurationKind.LongTerm => $"长期 = {FormatSigned(magnitude)}",
            _ => $"临时 = {FormatSigned(magnitude)}",
        };

    /// <summary>影响 Tab 默认文案（ActiveEffects 等通用场景）。</summary>
    public static string FormatTargetStat(EffectTargetStat target)
        => target switch
        {
            EffectTargetStat.Relationship => "外交关系",
            EffectTargetStat.Trust => "信赖",
            EffectTargetStat.Loyalty => "忠诚",
            EffectTargetStat.Diplomacy => "外交关系",
            EffectTargetStat.PersonalOpinion => "个人观感",
            EffectTargetStat.Agriculture => "农业",
            EffectTargetStat.Commerce => "商业",
            EffectTargetStat.Morale => "士气",
            _ => target.ToString(),
        };

    /// <summary>势力外交看法 · 影响列文案。</summary>
    public static string FormatDiplomacyViewTargetStat(EffectTargetStat target)
        => target switch
        {
            EffectTargetStat.Relationship or EffectTargetStat.Diplomacy => "外交关系",
            EffectTargetStat.Trust => "信赖",
            _ => FormatTargetStat(target),
        };

    /// <summary>角色间看法 · 影响列文案（不含外交关系）。</summary>
    public static string FormatCharacterViewTargetStat(EffectTargetStat target)
        => target switch
        {
            EffectTargetStat.Relationship => "亲疏",
            EffectTargetStat.Trust => "信赖",
            EffectTargetStat.PersonalOpinion => "个人观感",
            EffectTargetStat.Diplomacy => "亲疏",
            _ => FormatTargetStat(target),
        };

    private static string FormatSigned(int value)
        => value >= 0 ? $"+{value}" : value.ToString();
}
