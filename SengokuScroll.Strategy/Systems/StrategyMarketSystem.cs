using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式市场系统接口。</summary>
public interface IStrategyMarketSystem : IGameSystem
{
}

/// <summary>
/// 市场系统：市民 AI 挂单 → 连续撮合 → 日 K 线（M4-b）。
/// </summary>
public class StrategyMarketSystem(
    IGameContext context,
    MerchantTaxLedger taxLedger,
    StrategyIntelligenceLedger intelligenceLedger) : IStrategyMarketSystem
{
    /// <summary>在气候之后、经济之前执行。</summary>
    public int Order { get; } = 8;

    /// <inheritdoc />
    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var date = gameData.GameDate;

        // 阶段1：逐可贸易据点——AI 挂单
        foreach (var stronghold in context.GameWorldContext.EachStronghold())
        {
            if (!MarketRules.CanTrade(stronghold))
                continue;

            CivilianMarketAiHelper.EvaluateAndPlaceBuyOrders(stronghold);
            GovernmentMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);
            GovernmentLuxuryMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);
            MerchantMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);

            // 阶段2：连续撮合成交、写 K 线、记录价格波动情报
            var fallback = stronghold.Market.LastClosePriceMoneyPerGo > 0
                ? stronghold.Market.LastClosePriceMoneyPerGo
                : MarketConstants.DefaultPriceMoneyPerGo;

            var previousClose = fallback;
            var result = MarketCalculator.MatchOrders(stronghold, fallback);
            MarketActions.ApplyMatchResult(stronghold, result, taxLedger);
            MarketActions.SetDailyBarDate(stronghold, date);

            MaybeRecordPriceIntel(stronghold, date, previousClose, result.SessionClose);
        }

        UnitTradeActions.ProcessAutoTradePolicies(gameData, taxLedger);
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
        // 业务：价格波动超过阈值时记入情报台账供 AI/玩家参考
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
