using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public sealed class MemoryRetentionTests
{
    [Fact]
    public void MissingCarrier_DropsOrphanedPayloadButKeepsInTransitPayload()
    {
        using var context = StrategyTestWorldFactory.Create();
        var data = context.World.GameData;
        data.MessageCarriers[42] = new() { Id = 42, Name = "test", Payload = new() };
        var ledger = new StrategyPendingEventStore();
        ledger.Attach(42, new() { Category = "test", Message = "in transit" });
        ledger.Attach(43, new() { Category = "test", Message = "orphan" });
        ledger.PruneMissingCarriers(data);
        Assert.Equal("in transit", Assert.Single(ledger.Snapshot()).Value.Message);
    }

    [Fact]
    public void Quotes_AreBoundedByForceAndMarket_AndPreserveFirstSameDayQuote()
    {
        var ledger = new StrategyIntelligenceLedger();
        var date = new GameDate(1, 1, 1);
        var first = new StrategyIntelligenceLedger.MarketPriceObservation(1, 2, 10, date, date, "test", 10000);
        ledger.Record(first);
        for (var i = 0; i < 10000; i++) ledger.Record(first with { PriceMoneyPerGo = i });
        Assert.Single(ledger.SnapshotAll());
        Assert.Same(first, ledger.GetLatestPrice(1, 2));
        var newer = first with { ReceivedDate = new GameDate(2, 1, 1), PriceMoneyPerGo = 20 };
        ledger.Record(newer);
        ledger.Record(first);
        Assert.Same(newer, ledger.GetLatestPrice(1, 2));
        ledger.Record(first with { ObserverForceId = 3 });
        Assert.Equal(2, ledger.SnapshotAll().Count);
    }

    [Fact]
    public void LegacyQuoteHistory_IsCompactedOnRestoreWithoutLosingLatest()
    {
        var ledger = new StrategyIntelligenceLedger();
        var history = Enumerable.Range(1, 1000).Select(year => new StrategyIntelligenceLedger.MarketPriceObservation(
            1, 2, year, new GameDate(year, 1, 1), new GameDate(year, 1, 1), "test", 10000));
        ledger.Restore(history);
        Assert.Equal(1000, Assert.Single(ledger.SnapshotAll()).PriceMoneyPerGo);
    }

    [Fact]
    public void Tariffs_OnlyForgetRemovedUnits_NotTreasuryAccruals()
    {
        using var context = StrategyTestWorldFactory.Create();
        var unitId = context.World.GameData.Units.Keys.First();
        var ledger = new TariffTaxLedger();
        ledger.MarkTransitCharged(unitId, 1);
        ledger.Accrue(1, 123);
        ledger.PruneRemovedConvoys(context.World.GameData);
        Assert.True(ledger.HasChargedTransit(unitId, 1));
        context.World.GameData.Units.Remove(unitId);
        ledger.PruneRemovedConvoys(context.World.GameData);
        Assert.False(ledger.HasChargedTransit(unitId, 1));
        Assert.Equal(123, ledger.CollectForStronghold(1));
    }
}
