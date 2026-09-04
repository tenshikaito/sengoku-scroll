using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式市场系统接口。</summary>
public interface IStrategyMarketSystem : IGameSystem
{
}

/// <summary>
/// 市场系统：日初撮合存量挂单 → AI 补单 → 日 K（M4-b）。
/// 先撮合再补单，且补单时不 prune 既有挂单，避免日推进后簿面被清空。
/// </summary>
public class StrategyMarketSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    MerchantTaxLedger taxLedger,
    StrategyIntelligenceLedger intelligenceLedger) : IStrategyMarketSystem
{
    /// <summary>在气候之后、经济之前执行。</summary>
    public int Order { get; } = 8;

    /// <inheritdoc />
    public void Update()
    {
        var world = context.GameWorldContext.GameWorld;
        var gameData = world.GameData;
        var date = gameData.GameDate;
        var dayContext = new MarketAiDayContext
        {
            World = world,
            GameData = gameData,
            ScenarioMeta = scenarioMeta,
            TaxLedger = taxLedger,
            AllowOpportunisticSmash = false,
        };

        foreach (var stronghold in context.GameWorldContext.EachStronghold())
        {
            if (!MarketRules.CanTrade(stronghold, gameData))
                continue;

            MarketActions.RemoveZeroQuantityOrders(stronghold, date);

            var signals = MarketContextSignalsHelper.Resolve(stronghold, dayContext);

            var fallback = stronghold.Market.LastClosePriceMoneyPerGo > 0
                ? stronghold.Market.LastClosePriceMoneyPerGo
                : MarketConstants.DefaultPriceMoneyPerGo;

            var previousClose = fallback;
            var result = MarketCalculator.MatchOrders(stronghold, fallback, date);
            MarketActions.ApplyMatchResult(stronghold, result, taxLedger, date);

            MarketBootstrapHelper.EnsureMarketMakerInventories(stronghold);

            // 阶段2：限价补单（低买高卖 + 邻城套利锚价）；不在日更中机会砸单，避免清空簿面
            CivilianMarketAiHelper.EvaluateAndPlaceBuyOrders(stronghold, signals, dayContext);
            GovernmentMarketAiHelper.EvaluateAndPlaceBuyOrders(stronghold, signals, dayContext);
            GovernmentMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold, signals, dayContext);
            MerchantMarketAiHelper.EvaluateAndPlaceOrders(
                stronghold,
                MarketCommodityType.Food,
                signals,
                dayContext);
            HorseMarketAiHelper.EvaluateAndPlaceOrders(stronghold);
            MerchantMarketAiHelper.EvaluateAndPlaceOrders(
                stronghold,
                MarketCommodityType.Horse,
                signals,
                dayContext);

            MarketActions.RemoveDeprecatedCommodityOrders(stronghold);
            MarketActions.SetDailyBarDate(stronghold, date);

            RecordLocalPriceIntel(stronghold, date);
            MaybeRecordPriceIntel(stronghold, date, previousClose, result.SessionClose);
        }

        UnitTradeActions.ProcessAutoTradePolicies(gameData, taxLedger);
    }

    private void RecordLocalPriceIntel(Stronghold stronghold, GameDate date)
    {
        var price = stronghold.Market.ResolveLastClose(MarketCommodityType.Food);
        if (price <= 0)
            return;

        var observers = stronghold.MerchantActors
            .Select(merchant => merchant.ForceId)
            .Append(stronghold.ForceId)
            .Where(forceId => forceId > 0)
            .Distinct();

        foreach (var observerForceId in observers)
        {
            intelligenceLedger.Record(new StrategyIntelligenceLedger.MarketPriceObservation(
                observerForceId,
                stronghold.Id,
                price,
                date,
                date,
                "LocalMarket",
                EconomyConstants.BasisPointsPer100Percent));
        }
    }

    private void MaybeRecordPriceIntel(
        Stronghold stronghold,
        GameDate date,
        int previousClose,
        int newClose)
    {
        if (previousClose <= 0 || newClose <= 0)
            return;

        var deltaBp = Math.Abs(newClose - previousClose) * EconomyConstants.BasisPointsPer100Percent / previousClose;
        if (deltaBp < MarketConstants.PriceIntelThresholdBp)
            return;

        intelligenceLedger.Record(new StrategyIntelligenceLedger.MarketPriceObservation(
            stronghold.ForceId,
            stronghold.Id,
            newClose,
            date,
            date,
            "Market",
            EconomyConstants.BasisPointsPer100Percent));
    }
}
