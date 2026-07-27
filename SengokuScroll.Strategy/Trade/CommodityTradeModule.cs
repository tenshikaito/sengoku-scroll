using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Trade;

/// <summary>模块化大宗贸易入口：按商品种类统一限价买卖。</summary>
public static class CommodityTradeModule
{
    public static int LimitBuy(
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int buyerActorId,
        MarketCommodityType commodity,
        int limitPriceMoneyPerUnit,
        int quantity,
        Func<int> getBuyerMoney,
        Action<int> deductBuyerMoney,
        bool allowRestingOrder = true,
        bool commitMoneyOnRest = false,
        bool taxExemptOnRest = false)
    {
        if (quantity <= 0 && getBuyerMoney() <= 0)
            return 0;

        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            BuyerActorId = buyerActorId,
            LimitPriceMoneyPerGo = limitPriceMoneyPerUnit,
            QuantityGo = quantity,
            Commodity = commodity,
            GetBuyerMoney = getBuyerMoney,
            DeductBuyerMoney = deductBuyerMoney,
            GetBuyerStock = () => 0,
            AddBuyerStock = qty => ApplyBuyerStock(stronghold, gameData, buyerActorId, commodity, qty),
            AllowRestingOrder = allowRestingOrder,
            CommitMoneyOnRest = commitMoneyOnRest,
            TaxExemptOnRest = taxExemptOnRest,
        });

        if (result.FilledQuantityGo > 0)
            MarketAiRefreshHelper.RefreshAfterTrade(stronghold, gameData, commodity, taxLedger);

        return result.FilledQuantityGo;
    }

    public static int LimitSell(
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int sellerActorId,
        MarketCommodityType commodity,
        int limitPriceMoneyPerUnit,
        int quantity,
        Func<int> getSellerStock,
        Action<int> deductSellerStock,
        Action<int> addSellerMoney,
        bool allowRestingOrder = true,
        bool commitInventoryOnRest = false,
        bool taxExemptOnRest = false)
    {
        var result = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            SellerActorId = sellerActorId,
            LimitPriceMoneyPerGo = limitPriceMoneyPerUnit,
            QuantityGo = quantity,
            Commodity = commodity,
            GetSellerStock = getSellerStock,
            DeductSellerStock = deductSellerStock,
            AddSellerMoney = addSellerMoney,
            AllowRestingOrder = allowRestingOrder,
            CommitInventoryOnRest = commitInventoryOnRest,
            TaxExemptOnRest = taxExemptOnRest,
        });

        if (result.FilledQuantityGo > 0 || result.RestingQuantityGo > 0)
            MarketAiRefreshHelper.RefreshAfterTrade(stronghold, gameData, commodity, taxLedger);

        return result.FilledQuantityGo;
    }

    private static void ApplyBuyerStock(
        Stronghold stronghold,
        GameData gameData,
        int buyerActorId,
        MarketCommodityType commodity,
        int quantity)
    {
        if (gameData.Units.TryGetValue(buyerActorId, out var unit))
        {
            CommodityInventoryHelper.AddUnitStock(unit, commodity, quantity);
            return;
        }

        if (MarketActions.TryGetActorPublic(stronghold, buyerActorId, out var actor))
            CommodityInventoryHelper.AddStock(actor, commodity, quantity);
    }
}
