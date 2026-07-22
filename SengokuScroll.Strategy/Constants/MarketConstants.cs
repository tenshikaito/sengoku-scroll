namespace SengokuScroll.Strategy.Constants;

/// <summary>据点粮食市场常数（M4-b）。</summary>
public static class MarketConstants
{
    /// <summary>无历史成交时的默认粮价（文/合）。</summary>
    public const int DefaultPriceMoneyPerGo = 50;

    /// <summary>贸易税（向卖方商户计提，万分比）。</summary>
    public const int TradeTaxBasisPoints = 500;

    /// <summary>市民预计口粮不足此天数时尝试挂买单。</summary>
    public const int CivilianBuyOrderThresholdDays = 7;

    /// <summary>买单覆盖未来天数。</summary>
    public const int CivilianBuyOrderCoverDays = 15;

    /// <summary>买单限价相对最近收盘价上浮（万分比）。</summary>
    public const int CivilianBuyPricePremiumBp = 11000;

    /// <summary>商户店铺月维持费（文；单一费用项）。</summary>
    public const int MerchantShopMonthlyMaintenance = 5000;

    /// <summary>商户重开惩罚费（文；M4-b 占位）。</summary>
    public const int MerchantShopReopenPenalty = 10_000;

    /// <summary>情报：价格相对昨收波动超过此比例时写入 Ledger（万分比）。</summary>
    public const int PriceIntelThresholdBp = 1500;

    /// <summary>官府粮库最低保留（合）；超出部分可挂卖单。</summary>
    public const int GovernmentFoodReserveGo = 20_000;

    /// <summary>官府单次卖单最小量（合）。</summary>
    public const int GovernmentMinSellQuantityGo = 500;

    /// <summary>官府单日卖单上限（合）。</summary>
    public const int GovernmentMaxSellQuantityGo = 5000;

    /// <summary>奢侈品默认售价相对粮价倍数（万分比）。</summary>
    public const int LuxuryPriceMultiplierBp = 25_000;

    /// <summary>商户余粮保留（合）。</summary>
    public const int MerchantFoodReserveGo = 5000;

    /// <summary>商户单次最大卖粮（合）。</summary>
    public const int MerchantMaxSellFoodGo = 3000;

    /// <summary>奢侈品工坊日产（单位/日）= CommerceProduction / 此除数。</summary>
    public const int LuxuryProductionDivisor = 500;

    /// <summary>官府奢侈品单次卖单上限（单位）。</summary>
    public const int GovernmentMaxLuxurySellQuantity = 200;

    /// <summary>跨据点贸易最低价差（万分比，相对卖价）。</summary>
    public const int TradeMinProfitSpreadBp = 1500;

    /// <summary>缺粮时每日民心下降点数。</summary>
    public const int PopularFeelingsFoodShortagePenalty = 1;

    /// <summary>粮足时每日民心恢复上限。</summary>
    public const int PopularFeelingsRecoveryCap = 60;

    /// <summary>税负综合分每升高 1 点扣除民心。</summary>
    public const int PopularFeelingsTaxIncreasePenaltyPerPoint = 1;

    /// <summary>单次加税民心下降上限。</summary>
    public const int PopularFeelingsTaxIncreaseMaxPenalty = 8;

    /// <summary>税负综合分每降低 1 点恢复民心。</summary>
    public const int PopularFeelingsTaxDecreaseBonusPerPoint = 1;

    /// <summary>单次减税民心恢复上限。</summary>
    public const int PopularFeelingsTaxDecreaseMaxBonus = 5;
}
