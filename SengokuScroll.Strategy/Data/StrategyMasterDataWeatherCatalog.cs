using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Data;

/// <summary>野战天气主数据（与 <see cref="Battle.BattleWeatherEvaluator"/> 规则一致）。</summary>
internal static class StrategyMasterDataWeatherCatalog
{
    public static IReadOnlyList<StrategyMasterDataEntryDto> BuildEntries()
    {
        return
        [
            Entry(
                1,
                "晴",
                "无特殊天气修正。",
                attackerWinRateDelta: 0,
                defenderWinRateDelta: 0,
                archerMatchlockScale: 1.0,
                triggerHint: "非雨/寒/暑且无区域标签时"),
            Entry(
                2,
                "雨",
                "梅雨/台风季或高暴雨率区域；远程火力 ×0.88。",
                attackerWinRateDelta: -2,
                defenderWinRateDelta: -1,
                archerMatchlockScale: 0.88,
                triggerHint: "6–8 月或区域暴雨/洪涝率偏高"),
            Entry(
                3,
                "寒",
                "冬季或高寒潮/暴雪率区域；双方胜率下降。",
                attackerWinRateDelta: -3,
                defenderWinRateDelta: -2,
                archerMatchlockScale: 1.0,
                triggerHint: "11–2 月或区域寒潮/暴雪率偏高"),
            Entry(
                4,
                "暑",
                "盛夏或高干旱率区域；进攻方胜率略降。",
                attackerWinRateDelta: -1,
                defenderWinRateDelta: 0,
                archerMatchlockScale: 1.0,
                triggerHint: "7–8 月或区域干旱率偏高"),
        ];
    }

    private static StrategyMasterDataEntryDto Entry(
        int id,
        string name,
        string description,
        int attackerWinRateDelta,
        int defenderWinRateDelta,
        double archerMatchlockScale,
        string triggerHint)
        => new()
        {
            Id = id,
            Name = name,
            Description = description,
            Fields = new Dictionary<string, string>
            {
                ["attackerWinRateDelta"] = attackerWinRateDelta.ToString(),
                ["defenderWinRateDelta"] = defenderWinRateDelta.ToString(),
                ["archerMatchlockScale"] = archerMatchlockScale.ToString("0.##"),
                ["triggerHint"] = triggerHint,
                ["description"] = description,
            },
        };
}
