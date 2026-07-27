namespace SengokuScroll.Strategy.Constants;

/// <summary>据点粮食市场常数（M4-b）。</summary>
public static class MarketConstants
{
    /// <summary>无历史成交时的默认粮价（文/合）。</summary>
    public const int DefaultPriceMoneyPerGo = 50;

    /// <summary>贸易税（向卖方商户计提，万分比）。</summary>
    public const int TradeTaxBasisPoints = 500;

    /// <summary>市民预计口粮不足此天数时挂买单并提高近价档权重。</summary>
    public const int CivilianBuyOrderThresholdDays = 7;

    /// <summary>买单覆盖未来天数。</summary>
    public const int CivilianBuyOrderCoverDays = 15;

    /// <summary>推荐挂单深度（中枢上下各约 N 档；非固定，随预算与 Actor 略浮动）。</summary>
    public const int MarketRecommendedDepthLevels = 20;

    /// <summary>预算紧张时的最少尝试档数。</summary>
    public const int MarketMinDepthLevels = 8;

    /// <summary>单笔 AI 挂单深度硬上限。</summary>
    public const int MarketMaxDepthLevels = 24;

    /// <summary>兼容旧名 / UI 默认展示档数。</summary>
    public const int MarketDepthLevels = MarketRecommendedDepthLevels;

    /// <summary>兼容旧名。</summary>
    public const int MerchantMarketDepthLevels = MarketRecommendedDepthLevels;

    /// <summary>商户店铺月维持费（文；单一费用项）。</summary>
    public const int MerchantShopMonthlyMaintenance = 5000;

    /// <summary>商户营运准备金（文；低于此不挂粮买单）。</summary>
    public const int MerchantMoneyOperatingReserve = 15_000;

    /// <summary>商户重开惩罚费（文；M4-b 占位）。</summary>
    public const int MerchantShopReopenPenalty = 10_000;

    /// <summary>情报：价格相对昨收波动超过此比例时写入 Ledger（万分比）。</summary>
    public const int PriceIntelThresholdBp = 1500;

    /// <summary>官府粮库最低保留（合）；超出部分可挂卖单。</summary>
    public const int GovernmentFoodReserveGo = 20_000;

    /// <summary>官府/商户 AI 单层目标挂单量（合）；远档可低于此值而不挂。</summary>
    public const int GovernmentMinSellQuantityGo = 500;

    /// <summary>官府单日卖单总上限（合，多档合计）。</summary>
    public const int GovernmentMaxSellQuantityGo = 5000;

    /// <summary>官府单日买单总上限（合，多档合计）。</summary>
    public const int GovernmentMaxBuyQuantityGo = 5000;

    /// <summary>商户余粮保留（合）。</summary>
    public const int MerchantFoodReserveGo = 5000;

    /// <summary>商户单层卖单上限（合）。</summary>
    public const int MerchantMaxSellFoodGoPerLevel = 150_000;

    /// <summary>商户订单簿卖粮总上限（合）。</summary>
    public const int MerchantMaxTotalSellFoodGo = 400_000;

    /// <summary>商户订单簿买粮总资金上限（文）。</summary>
    public const int MerchantMaxTotalBuyMoney = 200_000;

    /// <summary>商户单层买单上限（合）。</summary>
    public const int MerchantMaxBuyFoodGoPerLevel = 80_000;

    /// <summary>市民单层买单上限（合）。</summary>
    public const int CivilianMaxBuyFoodGoPerLevel = 200_000;

    /// <summary>跨据点贸易最低价差（万分比，相对卖价）。</summary>
    public const int TradeMinProfitSpreadBp = 1500;

    /// <summary>卖价低于中枢此比例（万分比）时触发市民/官府捡漏买。</summary>
    public const int BargainBuyDiscountThresholdBp = 500;

    /// <summary>捡漏采购覆盖天数。</summary>
    public const int BargainBuyCoverDays = 3;

    /// <summary>官府马匹单次卖单上限（匹）。</summary>
    public const int GovernmentMaxHorseSellQuantity = 50;

    /// <summary>官府马匹单次买单上限（匹）。</summary>
    public const int GovernmentMaxHorseBuyQuantity = 30;

    /// <summary>官府马匹捡漏买资金上限（文）。</summary>
    public const int GovernmentMaxHorseBuyMoney = 50_000;

    /// <summary>商户马匹保留（匹）。</summary>
    public const int MerchantHorseReserve = 5;

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
