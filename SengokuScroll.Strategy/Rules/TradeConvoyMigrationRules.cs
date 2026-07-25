namespace SengokuScroll.Strategy.Rules;

/// <summary>SupplyConvoy → UnitKind.Merchant/Convoy 迁移开关（渐进式）。</summary>
public static class TradeConvoyMigrationRules
{
    /// <summary>false=沿用 SupplyConvoy；true=AI 贸易派遣优先创建 Merchant Unit。</summary>
    public const bool PreferUnitTradeConvoys = false;
}
