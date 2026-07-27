using SengokuScroll.Domain;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>按 master 定义读写 Actor 物资库存。</summary>
public static class CommodityInventoryHelper
{
    public static int GetStock(StrongholdActor actor, MarketCommodityType commodity)
        => commodity switch
        {
            MarketCommodityType.Horse => actor.Horse,
            _ => actor.Food,
        };

    public static void AddStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return;

        switch (commodity)
        {
            case MarketCommodityType.Horse:
                actor.Horse += quantity;
                break;
            default:
                actor.Food += quantity;
                break;
        }
    }

    public static bool TryRemoveStock(StrongholdActor actor, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return true;

        if (GetStock(actor, commodity) < quantity)
            return false;

        switch (commodity)
        {
            case MarketCommodityType.Horse:
                actor.Horse -= quantity;
                break;
            default:
                actor.Food -= quantity;
                break;
        }

        return true;
    }

    public static int GetUnitStock(Unit unit, MarketCommodityType commodity)
        => commodity switch
        {
            MarketCommodityType.Horse => unit.Horse,
            _ => unit.Food,
        };

    public static void AddUnitStock(Unit unit, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return;

        switch (commodity)
        {
            case MarketCommodityType.Horse:
                unit.Horse += quantity;
                break;
            default:
                unit.Food += quantity;
                break;
        }
    }

    public static bool TryRemoveUnitStock(Unit unit, MarketCommodityType commodity, int quantity)
    {
        if (quantity <= 0)
            return true;

        if (GetUnitStock(unit, commodity) < quantity)
            return false;

        switch (commodity)
        {
            case MarketCommodityType.Horse:
                unit.Horse -= quantity;
                break;
            default:
                unit.Food -= quantity;
                break;
        }

        return true;
    }

    public static int ResolveDefaultPrice(GameMasterData? masterData, MarketCommodityType commodity)
    {
        if (masterData?.Commodities.TryGetValue((int)commodity, out var def) == true
            && def.DefaultPriceMoneyPerUnit > 0)
        {
            return def.DefaultPriceMoneyPerUnit;
        }

        return commodity switch
        {
            MarketCommodityType.Horse => 120,
            _ => Constants.MarketConstants.DefaultPriceMoneyPerGo,
        };
    }

    public static bool IsTradeEnabled(GameMasterData? masterData, MarketCommodityType commodity)
    {
        if (masterData?.Commodities.TryGetValue((int)commodity, out var def) == true)
            return def.TradeEnabled;

        return commodity is MarketCommodityType.Food or MarketCommodityType.Horse;
    }
}
