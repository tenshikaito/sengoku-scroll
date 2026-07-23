namespace SengokuScroll.Domain.Entities.Types;

/// <summary>
/// <see cref="Entities.EntityEffect"/> 作用的目标属性。
/// 同一枚举在不同挂载点语义不同，展示文案由 Strategy 层 formatter 分叉（见 EntityEffectHelper）。
/// </summary>
public enum EffectTargetStat : byte
{
    /// <summary>
    /// 关系亲疏值（-100~100）。
    /// 势力外交看法 → 文案「外交关系」；角色间看法 → 文案「亲疏」。
    /// </summary>
    Relationship = 0,

    /// <summary>信任度（-100~100）。外交/角色看法均可叠加。</summary>
    Trust = 1,

    /// <summary>对所属势力的忠诚度（0–100）；通常挂载于 Character.ActiveEffects。</summary>
    Loyalty = 2,

    /// <summary>势力间外交关系修正；仅用于 Force.Diplomacies.ViewEffects，不用于角色看法。</summary>
    Diplomacy = 3,

    /// <summary>个人观感；角色间看法专用，不影响势力外交数值。</summary>
    PersonalOpinion = 4,

    /// <summary>据点农业产出修正；挂载于 Stronghold/Force ActiveEffects。</summary>
    Agriculture = 5,

    /// <summary>据点商业产出修正。</summary>
    Commerce = 6,

    /// <summary>单位/驻军士气修正。</summary>
    Morale = 7,
}
