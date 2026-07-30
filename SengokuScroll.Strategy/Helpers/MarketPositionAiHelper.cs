using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// Actor 仓位再平衡与机会抢筹：钱货比例 + 价崩大单吃进并在现价挂承接买单（砸卖同理）。
/// </summary>
public static class MarketPositionAiHelper
{
    public static int ResolveTargetMoneyShareBp(StrongholdMarketSignals signals)
    {
        var target = MarketConstants.TargetMoneyShareBp - signals.HoardBiasBp + signals.DumpBiasBp;
        return Math.Clamp(target, 1500, 8500);
    }

    /// <summary>当前资金占比（万分比）；库存按中枢计价。</summary>
    public static int ResolveMoneyShareBp(StrongholdActor actor, int referencePrice, MarketCommodityType commodity)
    {
        if (referencePrice <= 0)
            return EconomyConstants.BasisPointsPer100Percent;

        var stock = CommodityInventoryHelper.GetStock(actor, commodity);
        var inventoryValue = (long)stock * referencePrice;
        var total = actor.Money + inventoryValue;
        if (total <= 0)
            return EconomyConstants.BasisPointsPer100Percent / 2;

        return (int)(actor.Money * EconomyConstants.BasisPointsPer100Percent / total);
    }

    /// <summary>为贴近目标仓位建议买入量（合）。</summary>
    public static int CalculateBuyQuantityToBalance(
        StrongholdActor actor,
        int referencePrice,
        int targetMoneyShareBp,
        MarketCommodityType commodity)
    {
        if (referencePrice <= 0)
            return 0;

        var stock = CommodityInventoryHelper.GetStock(actor, commodity);
        var inventoryValue = (long)stock * referencePrice;
        var total = actor.Money + inventoryValue;
        if (total <= 0)
            return 0;

        var targetMoney = total * targetMoneyShareBp / EconomyConstants.BasisPointsPer100Percent;
        var surplusMoney = actor.Money - targetMoney;
        if (surplusMoney < MarketConstants.GovernmentMinSellQuantityGo)
            return 0;

        return (int)Math.Min(surplusMoney / referencePrice, int.MaxValue / 4);
    }

    /// <summary>为贴近目标仓位建议卖出量（合）。</summary>
    public static int CalculateSellQuantityToBalance(
        StrongholdActor actor,
        int referencePrice,
        int targetMoneyShareBp,
        MarketCommodityType commodity)
    {
        if (referencePrice <= 0)
            return 0;

        var stock = CommodityInventoryHelper.GetStock(actor, commodity);
        var inventoryValue = (long)stock * referencePrice;
        var total = actor.Money + inventoryValue;
        if (total <= 0 || stock <= 0)
            return 0;

        var targetMoney = total * targetMoneyShareBp / EconomyConstants.BasisPointsPer100Percent;
        var deficitMoney = targetMoney - actor.Money;
        if (deficitMoney < MarketConstants.GovernmentMinSellQuantityGo)
            return 0;

        var qty = (int)Math.Min(deficitMoney / referencePrice, stock);
        return Math.Max(0, qty);
    }

    public static bool IsMoneyHeavy(int moneyShareBp, int targetMoneyShareBp)
        => moneyShareBp > targetMoneyShareBp + MarketConstants.PositionDeadbandBp;

    public static bool IsGoodsHeavy(int moneyShareBp, int targetMoneyShareBp)
        => moneyShareBp < targetMoneyShareBp - MarketConstants.PositionDeadbandBp;

    /// <summary>对市民/官府/商户执行机会砸单（崩溃抢筹 / 过热砸货）。</summary>
    public static void EvaluateOpportunisticTrades(
        Stronghold stronghold,
        StrongholdMarketSignals signals,
        MarketAiDayContext context)
    {
        if (!context.AllowOpportunisticSmash || !MarketRules.CanTrade(stronghold, context.GameData))
            return;

        EvaluateActor(
            stronghold,
            stronghold.CivilianActor,
            signals,
            context,
            taxExempt: true,
            moneyReserve: 0,
            foodReserve: 0);

        EvaluateActor(
            stronghold,
            stronghold.ForceActor,
            signals,
            context,
            taxExempt: true,
            moneyReserve: 0,
            foodReserve: MarketConstants.GovernmentFoodReserveGo);

        foreach (var merchant in stronghold.MerchantActors)
        {
            EvaluateActor(
                stronghold,
                merchant,
                signals,
                context,
                taxExempt: false,
                moneyReserve: MarketConstants.MerchantMoneyOperatingReserve,
                foodReserve: MarketConstants.MerchantFoodReserveGo);
        }

        foreach (var religion in stronghold.ReligionActors)
        {
            EvaluateActor(
                stronghold,
                religion,
                signals,
                context,
                taxExempt: true,
                moneyReserve: 0,
                foodReserve: MarketConstants.MerchantFoodReserveGo / 2);
        }
    }

    private static void EvaluateActor(
        Stronghold stronghold,
        StrongholdActor actor,
        StrongholdMarketSignals signals,
        MarketAiDayContext context,
        bool taxExempt,
        int moneyReserve,
        int foodReserve)
    {
        var commodity = MarketCommodityType.Food;
        var fair = MarketMakerAiHelper.ResolveFairReferencePrice(stronghold, commodity);
        if (fair <= 0)
            return;

        var targetShare = ResolveTargetMoneyShareBp(signals);
        var spendableMoney = Math.Max(0, actor.Money - moneyReserve);
        var sellableFood = Math.Max(0, actor.Food - foodReserve);
        if (spendableMoney <= 0 && sellableFood <= 0)
            return;

        var moneyShare = ResolveMoneyShareBp(actor, fair, commodity);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var bestBid = MarketMakerAiHelper.ResolveBestBid(stronghold, commodity, actor.Id);

        var crash = signals.PriceCrashObserved
            || (bestAsk > 0
                && (fair - bestAsk) * EconomyConstants.BasisPointsPer100Percent / fair
                >= MarketConstants.OpportunityCrashDiscountBp);
        var rally = signals.PriceRallyObserved
            || (bestBid > 0
                && (bestBid - fair) * EconomyConstants.BasisPointsPer100Percent / fair
                >= MarketConstants.OpportunityRallyPremiumBp);

        // 业务：开战囤积 — 钱多或价崩时无视溢价也抢筹；丰收预期 — 货多时抢砸
        var wantBuy = (IsMoneyHeavy(moneyShare, targetShare) && (crash || bestAsk > 0 && bestAsk <= fair))
                      || (signals.HoardBiasBp > 0 && IsMoneyHeavy(moneyShare, targetShare))
                      || crash && spendableMoney > fair * MarketConstants.GovernmentMinSellQuantityGo;
        var wantSell = (IsGoodsHeavy(moneyShare, targetShare) && (rally || bestBid >= fair))
                       || (signals.DumpBiasBp > 0 && IsGoodsHeavy(moneyShare, targetShare))
                       || rally && sellableFood >= MarketConstants.GovernmentMinSellQuantityGo;

        if (wantBuy && spendableMoney > 0 && bestAsk > 0)
        {
            var balanceQty = CalculateBuyQuantityToBalance(actor, fair, targetShare, commodity);
            var qty = Math.Max(
                balanceQty,
                (int)((long)balanceQty * MarketConstants.OpportunitySmashSizeBp
                      / EconomyConstants.BasisPointsPer100Percent));
            if (crash)
                qty = Math.Max(qty, spendableMoney / Math.Max(1, bestAsk));

            qty = Math.Min(qty, spendableMoney / bestAsk);
            if (qty < MarketConstants.GovernmentMinSellQuantityGo && !crash)
                return;
            if (qty <= 0)
                return;

            // 业务：价崩时以现价（卖一）大单吃进；吃完后同价挂买单承接
            var limit = crash ? bestAsk : Math.Min(fair, bestAsk);
            var followQty = crash
                ? Math.Max(
                    qty * MarketConstants.OpportunityRestingFollowBp
                    / EconomyConstants.BasisPointsPer100Percent,
                    MarketConstants.GovernmentMinSellQuantityGo)
                : 0;
            var orderQty = qty + followQty;

            ActorMarketTradeActions.SmashBuyFood(
                stronghold,
                context.GameData,
                context.TaxLedger,
                actor,
                limit,
                orderQty,
                allowRestingOrder: crash || followQty > 0,
                taxExempt);
            return;
        }

        if (wantSell && sellableFood > 0 && bestBid > 0)
        {
            var balanceQty = CalculateSellQuantityToBalance(actor, fair, targetShare, commodity);
            var qty = Math.Max(
                balanceQty,
                (int)((long)balanceQty * MarketConstants.OpportunitySmashSizeBp
                      / EconomyConstants.BasisPointsPer100Percent));
            if (rally || signals.DumpBiasBp > 0)
                qty = Math.Max(qty, sellableFood / 4);

            qty = Math.Min(qty, sellableFood);
            if (qty < MarketConstants.GovernmentMinSellQuantityGo && !rally)
                return;
            if (qty <= 0)
                return;

            var limit = rally ? bestBid : Math.Max(fair, bestBid);
            var followQty = rally || signals.DumpBiasBp > 0
                ? Math.Max(
                    qty * MarketConstants.OpportunityRestingFollowBp
                    / EconomyConstants.BasisPointsPer100Percent,
                    MarketConstants.GovernmentMinSellQuantityGo)
                : 0;

            ActorMarketTradeActions.SmashSellFood(
                stronghold,
                context.GameData,
                context.TaxLedger,
                actor,
                limit,
                qty + followQty,
                allowRestingOrder: followQty > 0,
                taxExempt);
        }
    }
}
