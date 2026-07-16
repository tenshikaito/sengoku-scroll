using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>据点城防：由已建城防设施的防御值累加。</summary>
public static class StrongholdDefenseRules
{
    /// <summary>累加据点已建城防设施的防御值，作为攻城战中的城防加成基础。</summary>
    public static int ResolveTotalDefense(Stronghold stronghold, GameMasterData masterData)
    {
        var total = 0;
        foreach (var typeId in stronghold.DefenseFacilityIds)
        {
            if (masterData.DefenseFacilityTypes.TryGetValue(typeId, out var facilityType))
                total += Math.Max(0, facilityType.Defense);
        }

        return total;
    }

    /// <summary>同步 byte 城防字段与设施累加值。</summary>
    public static void SyncDefenseValue(Stronghold stronghold, GameMasterData masterData)
    {
        stronghold.Defense = (byte)Math.Min(
            byte.MaxValue,
            ResolveTotalDefense(stronghold, masterData));
    }

    /// <summary>攻城战后损毁：优先移除最后一项城防设施。</summary>
    public static void ApplySiegeDamage(Stronghold stronghold, int damageAmount)
    {
        if (damageAmount <= 0 || stronghold.DefenseFacilityIds.Count == 0)
            return;

        stronghold.DefenseFacilityIds.RemoveAt(stronghold.DefenseFacilityIds.Count - 1);
    }

    /// <summary>按人口规模为新据点分配默认城防设施（大城多道防线）。</summary>
    public static IReadOnlyList<int> ResolveDefaultFacilityIds(int population)
    {
        // 业务：5 万以上人口 → 护城河+天守+城墙；3 万以上 → 天守+城墙；否则仅城墙
        if (population >= 50_000)
            return [3, 2, 1];

        if (population >= 30_000)
            return [2, 1];

        return [1];
    }

    /// <summary>内置默认城防设施类型表（城墙/天守/护城河及各自防御值）。</summary>
    public static Dictionary<int, DefenseFacilityTypeModel> CreateDefaultDefenseFacilityTypes()
        => new()
        {
            [1] = new DefenseFacilityTypeModel
            {
                Id = 1,
                Name = "城墙",
                Category = DefenseFacilityTypeModel.DefenseFacilityCategory.Wall,
                Level = Domain.Enums.Level3.Medium,
                Cost = 0,
                Maintenance = 0,
                Attack = 0,
                Defense = 25,
                Movement = 0
            },
            [2] = new DefenseFacilityTypeModel
            {
                Id = 2,
                Name = "天守",
                Category = DefenseFacilityTypeModel.DefenseFacilityCategory.Castle,
                Level = Domain.Enums.Level3.Low,
                Cost = 0,
                Maintenance = 0,
                Attack = 0,
                Defense = 15,
                Movement = 0
            },
            [3] = new DefenseFacilityTypeModel
            {
                Id = 3,
                Name = "护城河",
                Category = DefenseFacilityTypeModel.DefenseFacilityCategory.Moat,
                Level = Domain.Enums.Level3.Medium,
                Cost = 0,
                Maintenance = 0,
                Attack = 0,
                Defense = 10,
                Movement = 0
            }
        };
}
