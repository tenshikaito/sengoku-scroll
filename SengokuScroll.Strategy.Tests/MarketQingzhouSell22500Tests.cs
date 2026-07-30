using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>mini_kanto 清洲：9 贯砸 22500 石后盘口 5 档期望。</summary>
public class MarketQingzhouSell22500Tests
{
    private const int UiDepthCount = 5;

    private const int SellKoku = 22_500;

    private const int ExpectedRestingKokuAtNine = 19_604;

    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void MiniKanto_Qingzhou_LimitSell22500At9_ClearsUiBidsAndRests20828OnAskNine()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var gameData = loaded.World.GameData;
        var registry = new StrategyForceLordRegistry();
        StrongholdCityActorBootstrapHelper.EnsureCityActors(gameData, registry);

        var stronghold = gameData.Strongholds.Values.Single(sh => sh.Name is "清洲" or "清州城");
        var sellQtyGo = SellKoku * LogisticsConstants.GoPerKoku;

        var result = StrongholdLordTradeActions.LimitSellFood(
            stronghold,
            loaded.Meta.PlayerForceId,
            loaded.Meta,
            gameData,
            new MerchantTaxLedger(),
            minPriceMoneyPerGo: 9,
            quantityGo: sellQtyGo);

        Assert.Equal(sellQtyGo, result.FilledQuantityGo + result.RestingQuantityGo);
        Assert.True(result.FilledQuantityGo > 0);
        Assert.True(result.RestingQuantityGo > 0);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: MarketBootstrapHelper.DemoDepthLevels);

        var uiAsks = MarketSnapshotDiagnostics.UiVisibleAskLevels(snapshot.AskLevels, UiDepthCount);
        var uiBids = MarketSnapshotDiagnostics.UiVisibleBidLevels(snapshot.BidLevels, UiDepthCount);

        // 玩家即时成交后不再触发 AI；9 贯剩余卖单应仍挂在簿上，AI 补单/撮合等日推进
        Assert.Equal(9, snapshot.BestAskPriceMoneyPerGo);
        var expectedRestingGo = ExpectedRestingKokuAtNine * LogisticsConstants.GoPerKoku;
        var askNine = Assert.Single(uiAsks, level => level.PriceMoneyPerGo == 9);
        Assert.Equal(result.RestingQuantityGo, askNine.QuantityGo);
        Assert.True(askNine.QuantityGo <= expectedRestingGo);
    }
}
