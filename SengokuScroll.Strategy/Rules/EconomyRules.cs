using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>经济结算相关判定（M1-d 占位，月结 M3 扩展）。</summary>
public static class EconomyRules
{
    /// <summary>军事单位且兵数 &gt; 0 时参与每日粮耗结算。</summary>
    public static bool ShouldConsumeDailyFood(Unit unit)
        => unit.IsMilitary && unit.Soldier > 0;

    /// <summary>人口 &gt; 0 的据点参与市民日耗粮。</summary>
    public static bool ShouldConsumeDailyCivilianFood(Stronghold stronghold)
        => stronghold.Population > 0;

    /// <summary>城内驻军（ForceActor.Soldier）参与每日口粮消耗。</summary>
    public static bool ShouldConsumeDailyGarrisonFood(Stronghold stronghold)
        => stronghold.ForceActor.Soldier > 0;

    /// <summary>是否参与每日生产入库（M4-a：有产出配置即参与）。</summary>
    public static bool ShouldApplyDailyProduction(Stronghold stronghold)
        => stronghold.CivilianActor.AgricultureProduction > 0
           || stronghold.CivilianActor.CommerceProduction > 0;

    /// <summary>是否为每月 1 日（月结触发日，M1-d 仅占位判定）。</summary>
    public static bool IsMonthlySettlementDay(GameDate date)
        => date.Day == 1;
}
