using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 野战伤亡上限：败方多为建制打散、溃散收拢，而非歼灭战。
/// 兵力悬殊时败方早溃，双方伤亡都应更低。
/// </summary>
public static class BattleCasualtyRules
{
    /// <summary>败退后至少保留的残部比例（相对战前兵数；与难度无关）。</summary>
    public static double MinDefeatSurvivorRatio(StrategyDifficulty difficulty)
        => StrategyDifficultyRules.DefaultDefeatSurvivorRatio;

    /// <summary>对瞬间战/战术战结果施加合理伤亡上限。</summary>
    public static InstantBattleOutcome CapOutcome(InstantBattleOutcome outcome, StrategyDifficulty difficulty)
    {
        if (outcome.IsSurrendered)
            return outcome;

        var atkBefore = outcome.AttackerSoldiersBefore;
        var defBefore = outcome.DefenderSoldiersBefore;
        if (atkBefore <= 0 || defBefore <= 0)
            return outcome;

        int cappedAttacker;
        int cappedDefender;
        if (outcome.AttackerWon)
        {
            cappedDefender = CapLoserCasualties(
                defBefore,
                outcome.DefenderCasualties,
                atkBefore,
                outcome.AttackerWinRatePercent,
                difficulty);
            cappedAttacker = CapWinnerCasualties(
                atkBefore,
                outcome.AttackerCasualties,
                defBefore,
                outcome.AttackerWinRatePercent);
        }
        else
        {
            var defenderWinRate = Math.Clamp(100 - outcome.AttackerWinRatePercent, 1, 99);
            cappedAttacker = CapLoserCasualties(
                atkBefore,
                outcome.AttackerCasualties,
                defBefore,
                defenderWinRate,
                difficulty);
            cappedDefender = CapWinnerCasualties(
                defBefore,
                outcome.DefenderCasualties,
                atkBefore,
                defenderWinRate);
        }

        if (cappedAttacker == outcome.AttackerCasualties && cappedDefender == outcome.DefenderCasualties)
            return outcome;

        return outcome with
        {
            AttackerCasualties = cappedAttacker,
            DefenderCasualties = cappedDefender
        };
    }

    /// <summary>缩放战术模拟伤亡并写回世界（替代直接 ApplyCasualtiesToWorld）。</summary>
    public static void ApplyCasualtiesToWorld(
        TacticalBattleResult tactical,
        GameData gameData,
        StrategyDifficulty difficulty)
    {
        var capped = CapTacticalResult(tactical, difficulty);
        ApplyScaledCasualties(capped, gameData);
    }

    /// <summary>战前预览：估算败方/胜方伤亡上限。</summary>
    public static (int LoserMin, int LoserMax, int WinnerMin, int WinnerMax) EstimateCasualtyRanges(
        int winnerBefore,
        int loserBefore,
        int winnerWinRatePercent,
        StrategyDifficulty difficulty)
    {
        var loserMax = CapLoserCasualties(
            loserBefore,
            loserBefore,
            winnerBefore,
            winnerWinRatePercent,
            difficulty);
        var loserMin = Math.Max(1, (int)Math.Ceiling(loserMax * 0.45));
        var winnerMax = CapWinnerCasualties(
            winnerBefore,
            winnerBefore,
            loserBefore,
            winnerWinRatePercent);
        var winnerMin = Math.Max(1, (int)Math.Ceiling(winnerMax * 0.35));
        return (loserMin, loserMax, winnerMin, winnerMax);
    }

    internal static int CapLoserCasualties(
        int loserBefore,
        int rawCasualties,
        int winnerBefore,
        int winnerWinRatePercent,
        StrategyDifficulty difficulty)
    {
        if (loserBefore <= 0 || rawCasualties <= 0)
            return 0;

        var forceRatio = (double)Math.Max(winnerBefore, loserBefore) / Math.Max(1, Math.Min(winnerBefore, loserBefore));
        var maxCasualtyRatio = forceRatio switch
        {
            >= 2.5 => 0.30,
            >= 1.8 => 0.38,
            >= 1.3 => 0.45,
            _ => 0.55
        };

        if (winnerWinRatePercent >= 75)
            maxCasualtyRatio *= 0.90;

        var minSurvivors = (int)Math.Ceiling(loserBefore * MinDefeatSurvivorRatio(difficulty));
        var maxFromSurvivorFloor = Math.Max(0, loserBefore - minSurvivors);
        var maxFromRatio = (int)Math.Ceiling(loserBefore * maxCasualtyRatio);

        return Math.Min(rawCasualties, Math.Min(maxFromSurvivorFloor, maxFromRatio));
    }

    internal static int CapWinnerCasualties(
        int winnerBefore,
        int rawCasualties,
        int loserBefore,
        int winnerWinRatePercent)
    {
        if (winnerBefore <= 0 || rawCasualties <= 0)
            return 0;

        var forceRatio = (double)winnerBefore / Math.Max(1, loserBefore);
        var maxRatio = forceRatio switch
        {
            >= 2.5 => 0.10,
            >= 1.8 => 0.14,
            >= 1.3 => 0.18,
            _ => 0.26
        };

        if (winnerWinRatePercent >= 80)
            maxRatio *= 0.85;

        var cap = Math.Max(1, (int)Math.Ceiling(winnerBefore * maxRatio));
        return Math.Min(rawCasualties, cap);
    }

    private static TacticalBattleResult CapTacticalResult(TacticalBattleResult tactical, StrategyDifficulty difficulty)
    {
        var cappedOutcome = CapOutcome(tactical.Outcome, difficulty);
        if (cappedOutcome.AttackerCasualties == tactical.Outcome.AttackerCasualties
            && cappedOutcome.DefenderCasualties == tactical.Outcome.DefenderCasualties)
        {
            return tactical;
        }

        var atkScale = tactical.Outcome.AttackerCasualties > 0
            ? (double)cappedOutcome.AttackerCasualties / tactical.Outcome.AttackerCasualties
            : 1.0;
        var defScale = tactical.Outcome.DefenderCasualties > 0
            ? (double)cappedOutcome.DefenderCasualties / tactical.Outcome.DefenderCasualties
            : 1.0;

        var scaledCasualties = ScaleCasualtiesBySide(tactical, atkScale, defScale);
        return new TacticalBattleResult
        {
            Outcome = cappedOutcome,
            LogEntries = tactical.LogEntries,
            CasualtiesByUnitId = scaledCasualties,
            SubUnitSoldiersAfter = tactical.SubUnitSoldiersAfter,
            IsSurrounded = tactical.IsSurrounded,
            AttackerParticipantCount = tactical.AttackerParticipantCount,
            DefenderParticipantCount = tactical.DefenderParticipantCount,
            AttackerParticipantUnitIds = tactical.AttackerParticipantUnitIds,
            DefenderParticipantUnitIds = tactical.DefenderParticipantUnitIds,
            MovementInitiative = tactical.MovementInitiative
        };
    }

    private static Dictionary<int, int> ScaleCasualtiesBySide(
        TacticalBattleResult tactical,
        double attackerScale,
        double defenderScale)
    {
        var attackerIds = tactical.AttackerParticipantUnitIds.ToHashSet();
        var scaled = new Dictionary<int, int>();
        foreach (var (unitId, raw) in tactical.CasualtiesByUnitId)
        {
            var scale = attackerIds.Contains(unitId) ? attackerScale : defenderScale;
            scaled[unitId] = Math.Max(0, (int)Math.Round(raw * scale));
        }

        return scaled;
    }

    private static void ApplyScaledCasualties(TacticalBattleResult result, GameData gameData)
    {
        foreach (var (unitId, casualties) in result.CasualtiesByUnitId)
        {
            if (!gameData.Units.TryGetValue(unitId, out var unit))
                continue;

            if (unit.SubUnitIds.Count > 0)
            {
                TacticalBattleSimulator.DistributeCasualtiesToSubUnits(unit, casualties, gameData);
                unit.Soldier = unit.SubUnitIds.Sum(id =>
                    gameData.SubUnits.TryGetValue(id, out var s) ? Math.Max(0, s.Soldier) : 0);
            }
            else
            {
                unit.Soldier = Math.Max(0, unit.Soldier - casualties);
            }

            if (unit.Soldier == 0)
                unit.Status = UnitStatus.Chaos;
        }
    }
}
