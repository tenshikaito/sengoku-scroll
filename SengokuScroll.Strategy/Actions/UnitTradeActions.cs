using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>贸易 Unit 在城内市价砸单（不吃簿挂单）。</summary>
public static class UnitTradeActions
{
    public static bool CanTradeAtStronghold(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (!unit.InStronghold
            || unit.LocationStrongholdId != stronghold.Id
            || unit.Soldier <= 0
            || !MarketRules.CanTrade(stronghold))
        {
            return false;
        }

        if (unit.ForceId == stronghold.ForceId)
            return true;

        if (!gameData.Forces.TryGetValue(unit.ForceId, out var unitForce)
            || !gameData.Forces.TryGetValue(stronghold.ForceId, out var holderForce))
        {
            return false;
        }

        return DiplomacyRules.IsAlly(unitForce, holderForce).IsSuccess;
    }

    /// <summary>市价买入粮食：吃卖单，成交进 Unit.Food/Money。</summary>
    public static int SmashBuyFood(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanTradeAtStronghold(unit, stronghold, gameData) || maxPriceMoneyPerGo <= 0)
            return 0;

        var remaining = quantityGo > 0 ? quantityGo : int.MaxValue / 1000;
        var totalBought = 0;

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == MarketCommodityType.Food)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();

        foreach (var sell in sells)
        {
            if (remaining <= 0 || unit.Money <= 0)
                break;

            if (sell.PriceMoneyPerGo > maxPriceMoneyPerGo)
                break;

            if (!MarketActions.TryGetActorPublic(stronghold, sell.ActorId, out var seller))
                continue;

            var affordableQty = unit.Money / sell.PriceMoneyPerGo;
            var qty = Math.Min(Math.Min(remaining, sell.QuantityGo), affordableQty);
            if (qty <= 0)
                continue;

            var totalMoney = sell.PriceMoneyPerGo * qty;
            if (!MarketInventoryHelper.TryRemoveStock(seller, MarketCommodityType.Food, qty))
                continue;

            unit.Money -= totalMoney;
            unit.Food += qty;
            seller.Money += totalMoney;

            if (!sell.TaxExempt)
            {
                var tax = EconomyCalculator.ApplyBasisPointsTax(
                    totalMoney,
                    MarketConstants.TradeTaxBasisPoints);
                if (tax > 0 && seller.Money >= tax)
                {
                    seller.Money -= tax;
                    taxLedger.Accrue(stronghold.Id, seller.Id, tax);
                }
            }

            sell.QuantityGo -= qty;
            totalBought += qty;
            remaining -= qty;
        }

        stronghold.Market.Orders.RemoveAll(o => o.QuantityGo <= 0);
        return totalBought;
    }

    /// <summary>市价卖出粮食：吃买单。</summary>
    public static int SmashSellFood(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerGo,
        int quantityGo)
    {
        if (!CanTradeAtStronghold(unit, stronghold, gameData) || minPriceMoneyPerGo <= 0)
            return 0;

        var remaining = quantityGo > 0 ? Math.Min(quantityGo, unit.Food) : unit.Food;
        var totalSold = 0;

        var buys = stronghold.Market.Orders
            .Where(o => MarketRules.IsBuyOrder(o) && o.Commodity == MarketCommodityType.Food)
            .OrderByDescending(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();

        foreach (var buy in buys)
        {
            if (remaining <= 0)
                break;

            if (buy.PriceMoneyPerGo < minPriceMoneyPerGo)
                break;

            if (!MarketActions.TryGetActorPublic(stronghold, buy.ActorId, out var buyer))
                continue;

            var qty = Math.Min(remaining, buy.QuantityGo);
            if (qty <= 0)
                continue;

            var totalMoney = buy.PriceMoneyPerGo * qty;
            if (buyer.Money < totalMoney)
            {
                qty = buyer.Money / buy.PriceMoneyPerGo;
                if (qty <= 0)
                    continue;

                totalMoney = buy.PriceMoneyPerGo * qty;
            }

            unit.Food -= qty;
            buyer.Money -= totalMoney;
            MarketInventoryHelper.AddStock(buyer, MarketCommodityType.Food, qty);
            unit.Money += totalMoney;

            buy.QuantityGo -= qty;
            totalSold += qty;
            remaining -= qty;
        }

        stronghold.Market.Orders.RemoveAll(o => o.QuantityGo <= 0);
        return totalSold;
    }

    /// <summary>按 TradePolicy 自动砸单（日更，市场撮合后）。</summary>
    public static void ProcessAutoTradePolicies(
        GameData gameData,
        MerchantTaxLedger taxLedger)
    {
        foreach (var unit in gameData.Units.Values)
        {
            if (unit.TradePolicy == UnitTradePolicy.None || !unit.InStronghold)
                continue;

            if (!gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var stronghold))
                continue;

            if (!CanTradeAtStronghold(unit, stronghold, gameData))
                continue;

            switch (unit.TradePolicy)
            {
                case UnitTradePolicy.WaitBuyFood:
                    SmashBuyFood(
                        unit,
                        stronghold,
                        gameData,
                        taxLedger,
                        unit.TradeLimitPriceMoneyPerGo,
                        unit.TradeQuantityGo);
                    break;
                case UnitTradePolicy.WaitSellFood:
                    SmashSellFood(
                        unit,
                        stronghold,
                        gameData,
                        taxLedger,
                        unit.TradeLimitPriceMoneyPerGo,
                        unit.TradeQuantityGo);
                    break;
            }
        }
    }
}
