namespace SengokuScroll.Domain.Entities;

/// <summary>
/// 角色对另一角色的关系记录（结构参照 <see cref="Diplomacy"/>）。
/// 持有方视角：OwnerCharacterId → TargetCharacterId。
/// </summary>
public sealed class CharacterRelationship
{
    /// <summary>关系持有者（视角方）角色 Id。</summary>
    public int OwnerCharacterId { get; set; }

    /// <summary>关系对象角色 Id。</summary>
    public int TargetCharacterId { get; set; }

    /// <summary>亲疏关系值（-100 恶劣 ~ 100 亲密）。</summary>
    public sbyte Relationship { get; set; }

    /// <summary>信任度（-100 不信任 ~ 100 完全信任）。</summary>
    public sbyte Trust { get; set; }
    public int? LastTalkDay { get; set; }
    public int? LastGiftDay { get; set; }
    public int? LastMarriageProposalDay { get; set; }

    /// <summary>持有方对该对象的看法条目（影响 Tab 子集；仅作用于私人亲疏/信赖/观感）。</summary>
    public List<EntityEffect> ViewEffects { get; set; } = [];
}
