namespace SengokuScroll.Strategy.Constants;

/// <summary>运输队软拦截常数（M4-b）。</summary>
public static class TransportConstants
{
    /// <summary>邻格存在敌军时的基础拦截权重。</summary>
    public const int AdjacentEnemyThreat = 30;

    /// <summary>同格敌军威胁（硬战前奏；M4-c 扩展战斗）。</summary>
    public const int SameTileEnemyThreat = 100;

    /// <summary>贸易队威胁减免（万分比）。</summary>
    public const int TradePurposeThreatReductionBp = 6000;

    /// <summary>补给队威胁加成（万分比）。</summary>
    public const int SupplyPurposeThreatIncreaseBp = 15000;

    /// <summary>过路税：拦截成功时扣 CargoMoney 比例（万分比）。</summary>
    public const int TollCargoMoneyBp = 500;

    /// <summary>遭遇战：扣 CargoFoodGo 比例（万分比）。</summary>
    public const int SkirmishCargoFoodBp = 1000;
}
