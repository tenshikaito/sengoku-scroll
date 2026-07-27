using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>寺社 Actor 在城内现货砸单（与商队相同：仅撮合，不挂簿）。</summary>
public static class ReligionTradeActions
{
    public static bool CanTradeAtStronghold(StrongholdActor religion, Stronghold stronghold, GameData gameData)
        => religion.Type == ActorType.Regligion
           && religion.StrongholdId == stronghold.Id
           && MarketRules.CanTrade(stronghold, gameData);

    public static int SmashBuyFood(
        StrongholdActor religion,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanTradeAtStronghold(religion, stronghold, gameData))
            return 0;

        return MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            BuyerActorId = religion.Id,
            LimitPriceMoneyPerGo = maxPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetBuyerMoney = () => religion.Money,
            DeductBuyerMoney = amount => religion.Money -= amount,
            AddBuyerStock = qty => religion.Food += qty,
            AllowRestingOrder = false,
        }).FilledQuantityGo;
    }

    public static int SmashSellFood(
        StrongholdActor religion,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanTradeAtStronghold(religion, stronghold, gameData))
            return 0;

        return MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            SellerActorId = religion.Id,
            LimitPriceMoneyPerGo = minPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetSellerStock = () => religion.Food,
            DeductSellerStock = qty => religion.Food -= qty,
            AddSellerMoney = amount => religion.Money += amount,
            AllowRestingOrder = false,
        }).FilledQuantityGo;
    }
}
