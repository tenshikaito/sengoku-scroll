using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>当主在据点内政菜单直接以官府库在大宗市场砸单。</summary>
public static class StrongholdLordTradeActions
{
    public readonly record struct LordLimitTradeResult(int FilledQuantityGo, int RestingQuantityGo)
    {
        public bool HasTradeEffect => FilledQuantityGo > 0 || RestingQuantityGo > 0;
    }

    public static bool CanLordTradeAtStronghold(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (stronghold.ForceId != playerForceId)
            return false;

        if (!MarketRules.CanTrade(stronghold, gameData))
            return false;

        return StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, stronghold);
    }

    /// <summary>官府限价买入粮食：先吃卖单，未成交部分以官府买单挂簿。</summary>
    public static LordLimitTradeResult LimitBuyFood(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanLordTradeAtStronghold(stronghold, playerForceId, meta, gameData))
            return default;

        var actor = stronghold.ForceActor;
        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            BuyerActorId = actor.Id,
            LimitPriceMoneyPerGo = maxPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetBuyerMoney = () => actor.Money,
            DeductBuyerMoney = amount => actor.Money -= amount,
            AddBuyerStock = qty => CommodityInventoryHelper.AddStock(actor, MarketCommodityType.Food, qty),
            AllowRestingOrder = true,
            CommitMoneyOnRest = true,
            TaxExemptOnRest = true,
        });

        if (result.HasTradeEffect)
        {
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        return new LordLimitTradeResult(result.FilledQuantityGo, result.RestingQuantityGo);
    }

    /// <summary>官府限价卖出粮食：先吃买单，未成交部分以官府卖单挂簿。</summary>
    public static LordLimitTradeResult LimitSellFood(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanLordTradeAtStronghold(stronghold, playerForceId, meta, gameData))
            return default;

        var actor = stronghold.ForceActor;
        var result = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            SellerActorId = actor.Id,
            LimitPriceMoneyPerGo = minPriceMoneyPerGo,
            QuantityGo = quantityGo,
            Commodity = MarketCommodityType.Food,
            GetSellerStock = () => actor.Food,
            DeductSellerStock = qty => actor.Food -= qty,
            AddSellerMoney = amount => actor.Money += amount,
            AllowRestingOrder = true,
            CommitInventoryOnRest = true,
            TaxExemptOnRest = true,
        });

        if (result.HasTradeEffect)
        {
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        return new LordLimitTradeResult(result.FilledQuantityGo, result.RestingQuantityGo);
    }

    /// <summary>官府限价买入马匹。</summary>
    public static LordLimitTradeResult LimitBuyHorse(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerUnit,
        int quantity)
        => LimitBuyCommodity(
            stronghold,
            playerForceId,
            meta,
            gameData,
            taxLedger,
            MarketCommodityType.Horse,
            maxPriceMoneyPerUnit,
            quantity);

    /// <summary>官府限价卖出马匹。</summary>
    public static LordLimitTradeResult LimitSellHorse(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerUnit,
        int quantity)
        => LimitSellCommodity(
            stronghold,
            playerForceId,
            meta,
            gameData,
            taxLedger,
            MarketCommodityType.Horse,
            minPriceMoneyPerUnit,
            quantity);

    private static LordLimitTradeResult LimitBuyCommodity(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        MarketCommodityType commodity,
        int maxPriceMoneyPerUnit,
        int quantity)
    {
        if (!CanLordTradeAtStronghold(stronghold, playerForceId, meta, gameData))
            return default;

        var actor = stronghold.ForceActor;
        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            BuyerActorId = actor.Id,
            LimitPriceMoneyPerGo = maxPriceMoneyPerUnit,
            QuantityGo = quantity,
            Commodity = commodity,
            GetBuyerMoney = () => actor.Money,
            DeductBuyerMoney = amount => actor.Money -= amount,
            AddBuyerStock = qty => CommodityInventoryHelper.AddStock(actor, commodity, qty),
            AllowRestingOrder = true,
            CommitMoneyOnRest = true,
            TaxExemptOnRest = true,
        });

        if (result.HasTradeEffect)
        {
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        return new LordLimitTradeResult(result.FilledQuantityGo, result.RestingQuantityGo);
    }

    private static LordLimitTradeResult LimitSellCommodity(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        MarketCommodityType commodity,
        int minPriceMoneyPerUnit,
        int quantity)
    {
        if (!CanLordTradeAtStronghold(stronghold, playerForceId, meta, gameData))
            return default;

        var actor = stronghold.ForceActor;
        var result = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = taxLedger,
            SellerActorId = actor.Id,
            LimitPriceMoneyPerGo = minPriceMoneyPerUnit,
            QuantityGo = quantity,
            Commodity = commodity,
            GetSellerStock = () => CommodityInventoryHelper.GetStock(actor, commodity),
            DeductSellerStock = qty => CommodityInventoryHelper.TryRemoveStock(actor, commodity, qty),
            AddSellerMoney = amount => actor.Money += amount,
            AllowRestingOrder = true,
            CommitInventoryOnRest = true,
            TaxExemptOnRest = true,
        });

        if (result.HasTradeEffect)
        {
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        return new LordLimitTradeResult(result.FilledQuantityGo, result.RestingQuantityGo);
    }

    /// <summary>撤销官府挂单并退还已锁定资源。</summary>
    public static bool CancelMarketOrder(
        Stronghold stronghold,
        int playerForceId,
        StrategyScenarioMeta meta,
        GameData gameData,
        int orderId,
        MarketCommodityType commodity,
        out GameError? error)
    {
        error = null;
        if (!CanLordTradeAtStronghold(stronghold, playerForceId, meta, gameData))
        {
            error = GameError.MarketError.TradeNotAllowed;
            return false;
        }

        if (!MarketActions.TryCancelOrder(
                stronghold,
                stronghold.ForceActor.Id,
                orderId,
                commodity,
                out error))
        {
            return false;
        }

        if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

        return true;
    }
}
