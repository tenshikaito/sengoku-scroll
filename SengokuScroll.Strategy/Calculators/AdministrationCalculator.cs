using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>行政效率：距当主居城越远，征收效率越低（与据点腐败叠加理解）。</summary>
public static class AdministrationCalculator
{
    /// <summary>每 N 格曼哈顿距离增加 1 点行政损耗（0–100 刻度）。</summary>
    public const int DistanceLossTilesPerPoint = 4;

    /// <summary>距离造成的行政损耗上限（不含据点本地腐败）。</summary>
    public const int MaxDistanceAdministrativeLoss = 40;

    /// <summary>距势力当主居城的曼哈顿距离（格）；无居城或即居城时为 0。</summary>
    public static int CalculateCapitalManhattanDistance(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var capitalId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            stronghold.ForceId,
            gameData,
            meta);
        if (capitalId <= 0 || capitalId == stronghold.Id)
            return 0;

        if (!gameData.Strongholds.TryGetValue(capitalId, out var capital))
            return 0;

        return Math.Abs(stronghold.Location.X - capital.Location.X)
               + Math.Abs(stronghold.Location.Y - capital.Location.Y);
    }

    /// <summary>因距居城过远产生的行政损耗（0–100），并入征收效率计算。</summary>
    public static byte CalculateDistanceAdministrativeLoss(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (stronghold.LordId > 0)
            return 0;

        var distance = CalculateCapitalManhattanDistance(stronghold, gameData, meta);
        if (distance <= 0)
            return 0;

        var loss = distance / DistanceLossTilesPerPoint;
        return (byte)Math.Min(MaxDistanceAdministrativeLoss, loss);
    }

    /// <summary>有效腐败 = 本地腐败 + 距离损耗（上限 100）。</summary>
    public static byte CalculateEffectiveCorruption(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var distanceLoss = CalculateDistanceAdministrativeLoss(stronghold, gameData, meta);
        return (byte)Math.Min(100, stronghold.Corruption + distanceLoss);
    }

    /// <summary>行政效率 0–100（100 = 无距离/腐败折损时的理想状态）。</summary>
    public static byte CalculateAdministrativeEfficiencyPercent(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (stronghold.LordId > 0)
            return 100;

        var effectiveCorruption = CalculateEffectiveCorruption(stronghold, gameData, meta);
        var authorityFactor = 50 + stronghold.Authority / 2;
        var corruptionFactor = 100 - effectiveCorruption * 2 / 3;
        var score = authorityFactor * corruptionFactor / 100;
        return (byte)Math.Clamp(score, 0, 100);
    }
}
