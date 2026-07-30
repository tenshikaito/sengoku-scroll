using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市场 Actor 库存读写（委托 <see cref="CommodityInventoryHelper"/>）。</summary>
public static class MarketInventoryHelper
{
    public static int GetStock(StrongholdActor actor, MarketCommodityType commodity)
        => CommodityInventoryHelper.GetStock(actor, commodity);

    public static void AddStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
        => CommodityInventoryHelper.AddStock(actor, commodity, quantity);

    public static bool TryRemoveStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
        => CommodityInventoryHelper.TryRemoveStock(actor, commodity, quantity);

    /// <summary>检查库存是否足够，不扣减。</summary>
    public static bool TryPeekStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
        => quantity > 0 && GetStock(actor, commodity) >= quantity;
}
