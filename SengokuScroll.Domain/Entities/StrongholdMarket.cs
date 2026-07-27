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

    /// <summary>当日成交量（合）。</summary>
    public int VolumeGo { get; set; }

    /// <summary>当日成交额（文）。</summary>
    public int TurnoverMoney { get; set; }
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

    /// <summary>初始挂单量（合）；用于计算部成/已成。</summary>
    public int OriginalQuantityGo { get; set; }

    /// <summary>挂单游戏日（0 表示未知/旧档）。</summary>
    public int CreatedYear { get; set; }

    public int CreatedMonth { get; set; }

    public int CreatedDay { get; set; }

    /// <summary>完全成交游戏日（0 表示未记录）。</summary>
    public int FilledYear { get; set; }

    public int FilledMonth { get; set; }

    public int FilledDay { get; set; }

    /// <summary>官府挂单等为 true，免交易税。</summary>
    public bool TaxExempt { get; set; }

    /// <summary>商品种类；默认粮食。</summary>
    public MarketCommodityType Commodity { get; set; } = MarketCommodityType.Food;

    /// <summary>挂卖单时是否已从卖方扣减可售库存（撮合时不再重复扣减）。</summary>
    public bool InventoryCommitted { get; set; }

    /// <summary>挂买单时是否已从买方扣减资金（撮合时不再重复扣减）。</summary>
    public bool MoneyCommitted { get; set; }

    /// <summary>挂买单已锁定资金（文）；撤单退还。</summary>
    public int CommittedMoneyGo { get; set; }

    /// <summary>挂卖单已锁定库存（合）；撤单退还。</summary>
    public int CommittedInventoryGo { get; set; }
}

/// <summary>据点订单簿市场（每 Stronghold 一个）。</summary>
public class StrongholdMarket
{
    public List<MarketOrder> Orders { get; set; } = [];

    public List<DailyPriceBar> PriceHistory { get; set; } = [];

    /// <summary>最近收盘价（文/合；legacy，等同 Food）。</summary>
    public int LastClosePriceMoneyPerGo { get; set; }

    /// <summary>各商品最近收盘价（文/单位）。</summary>
    public Dictionary<MarketCommodityType, int> LastCloseByCommodity { get; set; } = [];

    /// <summary>非粮食商品的日 K 线（粮食沿用 <see cref="PriceHistory"/>）。</summary>
    public Dictionary<MarketCommodityType, List<DailyPriceBar>> PriceHistoryByCommodity { get; set; } = [];

    public int ResolveLastClose(MarketCommodityType commodity)
    {
        if (LastCloseByCommodity.TryGetValue(commodity, out var price) && price > 0)
            return price;

        return commodity == MarketCommodityType.Food && LastClosePriceMoneyPerGo > 0
            ? LastClosePriceMoneyPerGo
            : 0;
    }

    public void SetLastClose(MarketCommodityType commodity, int priceMoneyPerUnit)
    {
        if (priceMoneyPerUnit <= 0)
            return;

        LastCloseByCommodity[commodity] = priceMoneyPerUnit;
        if (commodity == MarketCommodityType.Food)
            LastClosePriceMoneyPerGo = priceMoneyPerUnit;
    }

    public IReadOnlyList<DailyPriceBar> ResolvePriceHistory(MarketCommodityType commodity)
    {
        if (commodity != MarketCommodityType.Food
            && PriceHistoryByCommodity.TryGetValue(commodity, out var dedicated)
            && dedicated.Count > 0)
        {
            return dedicated;
        }

        return PriceHistory;
    }

    public List<DailyPriceBar> ResolveMutablePriceHistory(MarketCommodityType commodity)
    {
        if (commodity == MarketCommodityType.Food)
            return PriceHistory;

        if (!PriceHistoryByCommodity.TryGetValue(commodity, out var history))
        {
            history = [];
            PriceHistoryByCommodity[commodity] = history;
        }

        return history;
    }
}
