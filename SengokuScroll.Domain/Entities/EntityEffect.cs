using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 实体增减益 / 看法条目（影响 Tab、关系看法等共用结构）。
/// </summary>
/// <remarks>
/// <para><b>ActiveEffects</b>：挂载于 Force / Stronghold / Character，供「影响」Tab 与 Loyalty 等派生计算。</para>
/// <para><b>ViewEffects</b>：挂载于 Diplomacy 或 CharacterRelationship，供「本家/对方看法」「本人/对本人看法」Tab；不直接改外交关系枚举 Relation。</para>
/// </remarks>
public sealed class EntityEffect
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public EffectTargetStat TargetStat { get; set; }

    /// <summary>影响幅度；符号表示增减，具体语义由 TargetStat 决定。</summary>
    public int Magnitude { get; set; }

    public EffectDurationKind Duration { get; set; }

    public string? Description { get; set; }

    /// <summary>临时效果到期日；Permanent/LongTerm 可为 default。</summary>
    public GameDate? ExpiresOn { get; set; }
}
