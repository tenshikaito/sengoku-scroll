using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Trade;

namespace SengokuScroll.Strategy.Actions;

/// <summary>贸易 Unit 在城内限价砸单（仅撮合，不挂簿）。</summary>
public static class UnitTradeActions
{
    public static bool CanTradeAtStronghold(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (unit.Kind != UnitKind.Merchant
            || !unit.InStronghold
            || unit.LocationStrongholdId != stronghold.Id
            || !MarketRules.CanTrade(stronghold, gameData))
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

    public static int SmashBuyFood(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerGo,
        int quantityGo)
        => SmashBuy(
            unit,
            stronghold,
            gameData,
            taxLedger,
            MarketCommodityType.Food,
            maxPriceMoneyPerGo,
            quantityGo);

    public static int SmashSellFood(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerGo,
        int quantityGo)
        => SmashSell(
            unit,
            stronghold,
            gameData,
            taxLedger,
            MarketCommodityType.Food,
            minPriceMoneyPerGo,
            quantityGo);

    public static int SmashBuyHorse(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int maxPriceMoneyPerUnit,
        int quantity)
        => SmashBuy(
            unit,
            stronghold,
            gameData,
            taxLedger,
            MarketCommodityType.Horse,
            maxPriceMoneyPerUnit,
            quantity);

    public static int SmashSellHorse(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        int minPriceMoneyPerUnit,
        int quantity)
        => SmashSell(
            unit,
            stronghold,
            gameData,
            taxLedger,
            MarketCommodityType.Horse,
            minPriceMoneyPerUnit,
            quantity);

    public static int SmashBuy(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        MarketCommodityType commodity,
        int maxPriceMoneyPerUnit,
        int quantity)
    {
        if (!CanTradeAtStronghold(unit, stronghold, gameData))
            return 0;

        return CommodityTradeModule.LimitBuy(
            stronghold,
            gameData,
            taxLedger,
            unit.Id,
            commodity,
            maxPriceMoneyPerUnit,
            quantity,
            () => unit.Money,
            amount => unit.Money -= amount,
            allowRestingOrder: false);
    }

    public static int SmashSell(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        MerchantTaxLedger taxLedger,
        MarketCommodityType commodity,
        int minPriceMoneyPerUnit,
        int quantity)
    {
        if (!CanTradeAtStronghold(unit, stronghold, gameData))
            return 0;

        return CommodityTradeModule.LimitSell(
            stronghold,
            gameData,
            taxLedger,
            unit.Id,
            commodity,
            minPriceMoneyPerUnit,
            quantity,
            () => Helpers.CommodityInventoryHelper.GetUnitStock(unit, commodity),
            qty => Helpers.CommodityInventoryHelper.TryRemoveUnitStock(unit, commodity, qty),
            amount => unit.Money += amount,
            allowRestingOrder: false);
    }

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
