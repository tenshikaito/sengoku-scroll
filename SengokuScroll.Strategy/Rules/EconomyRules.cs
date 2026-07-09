using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>经济结算相关判定（M1-d 占位，月结 M3 扩展）。</summary>
public static class EconomyRules
{
    /// <summary>军事单位且兵数 &gt; 0 时参与每日粮耗结算。</summary>
    public static bool ShouldConsumeDailyFood(Unit unit)
        => unit.IsMilitary && unit.Soldier > 0;

    /// <summary>是否为每月 1 日（月结触发日，M1-d 仅占位判定）。</summary>
    public static bool IsMonthlySettlementDay(GameDate date)
        => date.Day == 1;
}
