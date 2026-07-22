using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>从角色实体与剧本关系字段组装人际关系列表（含推断关系）。</summary>
public static class CharacterRelationsHelper
{
    public static IReadOnlyList<StrategyCharacterRelationDto> BuildRelations(
        Character character,
        IReadOnlyDictionary<int, Character> characters)
    {
        var list = new List<StrategyCharacterRelationDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string relationType, int targetId)
        {
            if (targetId <= 0 || targetId == character.Id)
                return;
            if (!characters.TryGetValue(targetId, out var target) || target.IsDead)
                return;

            var key = $"{relationType}:{targetId}";
            if (!seen.Add(key))
                return;

            list.Add(new StrategyCharacterRelationDto
            {
                RelationType = relationType,
                CharacterId = targetId,
                CharacterName = target.Name
            });
        }

        Add("父亲", character.FatherId);
        Add("母亲", character.MotherId);
        Add("配偶", character.SpouseId);
        Add("师父", character.MasterId);
        Add("上司", character.LeaderId);

        foreach (var enemyId in character.EnemyIds)
            Add("仇敌", enemyId);

        foreach (var other in characters.Values)
        {
            if (other.Id == character.Id || other.IsDead)
                continue;

            if (other.FatherId == character.Id || other.MotherId == character.Id)
                Add("子女", other.Id);

            if (other.LeaderId == character.Id)
                Add("下属", other.Id);

            if (other.EnemyIds.Contains(character.Id))
                Add("仇敌", other.Id);
        }

        if (character.SpouseId > 0
            && characters.TryGetValue(character.SpouseId, out var spouse)
            && !spouse.IsDead)
        {
            Add("岳父", spouse.FatherId);
            Add("岳母", spouse.MotherId);
        }

        return list
            .OrderBy(r => RelationSortKey(r.RelationType))
            .ThenBy(r => r.CharacterName, StringComparer.Ordinal)
            .ToList();
    }

    private static int RelationSortKey(string relationType)
        => relationType switch
        {
            "父亲" => 0,
            "母亲" => 1,
            "配偶" => 2,
            "子女" => 3,
            "岳父" => 4,
            "岳母" => 5,
            "师父" => 6,
            "上司" => 7,
            "下属" => 8,
            "仇敌" => 9,
            _ => 99
        };
}
