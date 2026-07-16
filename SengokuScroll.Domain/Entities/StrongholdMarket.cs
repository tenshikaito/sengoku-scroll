using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>据点粮食市场日 K 线（文/合，整型）。</summary>
public class DailyPriceBar
{
    public GameDate Date { get; set; }

    public int Open { get; set; }

    public int High { get; set; }

    public int Low { get; set; }

    public int Close { get; set; }
}

/// <summary>市场挂单（M4-b 连续撮合）。</summary>
public class MarketOrder
{
    public int Id { get; set; }

    /// <summary>Buy | Sell</summary>
    public required string Side { get; set; }

    /// <summary>挂单主体 Actor Id（官府/商户/市民聚合等）。</summary>
    public int ActorId { get; set; }

    public int PriceMoneyPerGo { get; set; }

    public int QuantityGo { get; set; }

    /// <summary>官府挂单等为 true，免交易税。</summary>
    public bool TaxExempt { get; set; }

    /// <summary>商品种类；默认粮食。</summary>
    public MarketCommodityType Commodity { get; set; } = MarketCommodityType.Food;
}

/// <summary>据点订单簿市场（每 Stronghold 一个）。</summary>
public class StrongholdMarket
{
    public List<MarketOrder> Orders { get; set; } = [];

    public List<DailyPriceBar> PriceHistory { get; set; } = [];

    /// <summary>最近收盘价（文/合）。</summary>
    public int LastClosePriceMoneyPerGo { get; set; }
}
