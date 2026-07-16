using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Battle;

/// <summary>按 SubUnit 兵种构成加权有效战力。</summary>
public static class BattleCompositionCalculator
{
    /// <summary>按子队兵种构成加权计算单位有效战力；无编制时回退整队估算。</summary>
    public static int ComputeEffectivePower(
        Unit unit,
        GameData gameData,
        TerrainType? defenderTerrain = null)
    {
        if (unit.SubUnitIds.Count == 0)
            return InstantBattleCalculator.ComputeEffectivePower(unit);

        var total = 0;
        foreach (var subUnitId in unit.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subUnitId, out var sub) || sub.Soldier <= 0)
                continue;

            var attack = sub.Attack > 0 ? sub.Attack : unit.Attack > 0 ? unit.Attack : BattleConstants.DefaultCombatStat;
            var defense = sub.Defense > 0 ? sub.Defense : unit.Defense > 0 ? unit.Defense : BattleConstants.DefaultCombatStat;
            // 业务：有效战力 = 兵数 × (攻+防)/20 × 兵种地形系数
            var typeScale = ResolveTroopTypeScale(sub.TypeId, defenderTerrain);
            total += (int)Math.Round(sub.Soldier * (attack + defense) / 20.0 * typeScale);
        }

        return total > 0 ? total : InstantBattleCalculator.ComputeEffectivePower(unit);
    }

    /// <summary>兵种基础系数与守方地形加成/惩罚的复合倍率。</summary>
    public static double ResolveTroopTypeScale(byte typeId, TerrainType? terrain)
    {
        // 业务：骑兵 +8%、弓 +4%、铁炮 +6% 基础加成
        var baseScale = typeId switch
        {
            StrategyTroopTypes.Cavalry => 1.08,
            StrategyTroopTypes.Archer => 1.04,
            StrategyTroopTypes.Matchlock => 1.06,
            _ => 1.0
        };

        if (terrain is null)
            return baseScale;

        // 业务：地形与兵种匹配——平原利骑、山地利弓、山地克骑等
        return (typeId, terrain) switch
        {
            (StrategyTroopTypes.Cavalry, TerrainType.Plain) => baseScale * 1.10,
            (StrategyTroopTypes.Cavalry, TerrainType.Hill) => baseScale * 1.05,
            (StrategyTroopTypes.Cavalry, TerrainType.Mountain) => baseScale * 0.72,
            (StrategyTroopTypes.Archer, TerrainType.Hill) => baseScale * 1.08,
            (StrategyTroopTypes.Archer, TerrainType.Mountain) => baseScale * 1.10,
            (StrategyTroopTypes.Matchlock, TerrainType.Plain) => baseScale * 1.08,
            (StrategyTroopTypes.Matchlock, TerrainType.Hill or TerrainType.Mountain) => baseScale * 0.82,
            (StrategyTroopTypes.Ashigaru, TerrainType.Mountain) => baseScale * 0.95,
            _ => baseScale
        };
    }
}
