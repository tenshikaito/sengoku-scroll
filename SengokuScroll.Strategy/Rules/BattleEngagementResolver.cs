using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 野战接敌时的攻/守角色判定（业务规则）。
/// </summary>
/// <remarks>
/// <para>业务约定：</para>
/// <list type="bullet">
///   <item>单方下达攻击：下达方担任攻方，目标担任守方。</item>
///   <item>双方互下攻击：日推进结算时比较移动力，移动力高者先进攻并担任攻方；相同则单位 Id 较小者担任攻方（确定性）。</item>
///   <item>移动力仅决定行动顺序与攻/守角色，不提供额外数值 buff。</item>
/// </list>
/// </remarks>
public static class BattleEngagementResolver
{
    /// <summary>判定本场野战的攻方与守方单位。</summary>
    public static (Unit Attacker, Unit Defender, bool BothOrderedAttack) ResolveRoles(
        Unit unitA,
        Unit unitB,
        bool aOrderedAttackOnB,
        bool bOrderedAttackOnA)
    {
        // 业务：仅 A 攻击 B → A 为攻方
        if (aOrderedAttackOnB && !bOrderedAttackOnA)
            return (unitA, unitB, false);

        // 业务：仅 B 攻击 A → B 为攻方
        if (bOrderedAttackOnA && !aOrderedAttackOnB)
            return (unitB, unitA, false);

        // 业务：互下攻击令 → 移动力高者为攻方；相同则 Id 小者为攻方（确定性）
        if (aOrderedAttackOnB && bOrderedAttackOnA)
        {
            if (unitA.Movement != unitB.Movement)
                return unitA.Movement > unitB.Movement ? (unitA, unitB, true) : (unitB, unitA, true);

            return unitA.Id < unitB.Id ? (unitA, unitB, true) : (unitB, unitA, true);
        }

        return (unitA, unitB, false);
    }
}
