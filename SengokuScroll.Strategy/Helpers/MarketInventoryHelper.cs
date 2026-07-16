using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市场 Actor 库存读写（按商品种类；M4-d）。</summary>
public static class MarketInventoryHelper
{
    public static int GetStock(StrongholdActor actor, MarketCommodityType commodity)
        => commodity == MarketCommodityType.Luxury ? actor.LuxuryGoods : actor.Food;

    public static void AddStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return;

        if (commodity == MarketCommodityType.Luxury)
            actor.LuxuryGoods += quantity;
        else
            actor.Food += quantity;
    }

    public static bool TryRemoveStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return true;

        if (GetStock(actor, commodity) < quantity)
            return false;

        if (commodity == MarketCommodityType.Luxury)
            actor.LuxuryGoods -= quantity;
        else
            actor.Food -= quantity;

        return true;
    }
}
