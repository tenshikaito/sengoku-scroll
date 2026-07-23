using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 从剧本亲属/仇敌字段同步到 <see cref="CharacterRelationship"/> 基线数值。
/// 不写入 ViewEffects；看法条目由事件或 IntelEntityBootstrapHelper 演示 seed 填充。
/// </summary>
public static class CharacterRelationshipBootstrapHelper
{
    /// <summary>
    /// 为父母/配偶/师父/子女/仇敌建立默认 Relationship、Trust。
    /// 已有同目标条目时跳过（不覆盖事件修改）。
    /// </summary>
    public static void EnsureKinshipRelationships(
        Character character,
        IReadOnlyDictionary<int, Character> characters)
    {
        EnsureRelationship(character, character.FatherId, relationship: 60, trust: 70);
        EnsureRelationship(character, character.MotherId, relationship: 60, trust: 70);
        EnsureRelationship(character, character.SpouseId, relationship: 55, trust: 65);
        EnsureRelationship(character, character.MasterId, relationship: 40, trust: 50);

        foreach (var enemyId in character.EnemyIds)
            EnsureRelationship(character, enemyId, relationship: -80, trust: -90);

        foreach (var other in characters.Values)
        {
            if (other.Id == character.Id || other.IsDead)
                continue;

            if (other.FatherId == character.Id || other.MotherId == character.Id)
                EnsureRelationship(character, other.Id, relationship: 55, trust: 65);
        }
    }

    private static void EnsureRelationship(
        Character owner,
        int targetId,
        sbyte relationship,
        sbyte trust)
    {
        if (targetId <= 0 || targetId == owner.Id)
            return;

        if (owner.Relationships.Any(r => r.TargetCharacterId == targetId))
            return;

        owner.Relationships.Add(new CharacterRelationship
        {
            OwnerCharacterId = owner.Id,
            TargetCharacterId = targetId,
            Relationship = relationship,
            Trust = trust,
        });
    }
}
