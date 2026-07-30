using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>城内 Actor 现货砸单（不触发 AI Refresh，避免日更递归）。</summary>
public static class ActorMarketTradeActions
{
    public static int SmashBuyFood(
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        StrongholdActor buyer,
        int limitPriceMoneyPerGo,
        int quantityGo,
        bool allowRestingOrder,
        bool taxExempt)
    {
        if (quantityGo <= 0 || limitPriceMoneyPerGo <= 0)
            return 0;

        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            BuyerActorId = buyer.Id,
            LimitPriceMoneyPerGo = limitPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetBuyerMoney = () => buyer.Money,
            DeductBuyerMoney = amount => buyer.Money -= amount,
            AddBuyerStock = qty => CommodityInventoryHelper.AddStock(buyer, MarketCommodityType.Food, qty),
            GetBuyerStock = () => buyer.Food,
            AllowRestingOrder = allowRestingOrder,
            CommitMoneyOnRest = true,
            TaxExemptOnRest = taxExempt,
        });

        return result.FilledQuantityGo;
    }

    public static int SmashSellFood(
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        StrongholdActor seller,
        int limitPriceMoneyPerGo,
        int quantityGo,
        bool allowRestingOrder,
        bool taxExempt)
    {
        if (quantityGo <= 0 || limitPriceMoneyPerGo <= 0)
            return 0;

        var result = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            SellerActorId = seller.Id,
            LimitPriceMoneyPerGo = limitPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetSellerStock = () => seller.Food,
            DeductSellerStock = qty =>
            {
                CommodityInventoryHelper.TryRemoveStock(seller, MarketCommodityType.Food, qty);
            },
            AddSellerMoney = amount => seller.Money += amount,
            AllowRestingOrder = allowRestingOrder,
            CommitInventoryOnRest = true,
            TaxExemptOnRest = taxExempt,
        });

        return result.FilledQuantityGo;
    }
}
