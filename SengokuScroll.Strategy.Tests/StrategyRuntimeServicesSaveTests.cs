using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public sealed class StrategyRuntimeServicesSaveTests
{
    [Fact]
    public void JsonRoundTrip_RestoresIntelTaxesTributeAndPendingReports()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var source = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var services = source.Services;

        services.GetRequiredService<StrategyIntelligenceLedger>().Record(new(
            ObserverForceId: 1,
            SubjectStrongholdId: 9,
            PriceMoneyPerGo: 73,
            AsOfDate: new GameDate(2, 3, 4),
            ReceivedDate: new GameDate(2, 3, 6),
            SourceType: "Merchant",
            ReliabilityBp: 8_500));
        services.GetRequiredService<StrategyEspionageIntelLedger>().RecordMission(
            1,
            EspionageIntelTargetKind.Stronghold,
            9,
            EspionageIntelScope.Both,
            EspionageIntelPrecision.Exact,
            new GameDate(2, 3, 6));

        services.GetRequiredService<StrategyPendingEventStore>().Attach(701, new StrategyEventDto
        {
            Category = "StrategicReport",
            Message = "在途事件详情"
        });
        services.GetRequiredService<StrategyPendingBattleReportStore>().Attach(702, new StrategyBattleResultDto
        {
            AttackerWon = true,
            AttackerUnitId = 1,
            DefenderUnitId = 2,
            AttackerForceId = 1,
            DefenderForceId = 2,
            AttackerName = "甲",
            DefenderName = "乙",
            AttackerSoldiersBefore = 100,
            DefenderSoldiersBefore = 90,
            AttackerCasualties = 10,
            DefenderCasualties = 30,
            AttackerSoldiersAfter = 90,
            DefenderSoldiersAfter = 60,
            AttackerWinRatePercent = 65,
            ResolutionSeed = 123,
            ResolutionRoll = 20,
            EngagementKind = "FieldBattle",
            LogEntries = [],
            FactorNotes = []
        });

        services.GetRequiredService<MerchantTaxLedger>().Accrue(1, 88, 321);
        var tariff = services.GetRequiredService<TariffTaxLedger>();
        tariff.Accrue(1, 654);
        tariff.MarkTransitCharged(77, 1);
        services.GetRequiredService<MonthlyTaxCollectionLedger>()
            .RecordMonthlyMoneyTaxes(1, 1_000, 500, 200, 100);
        services.GetRequiredService<StrategyTributeLedger>()
            .RecordArrival(2, 1, "本城", 2_000, 300);
        Assert.True(services.GetRequiredService<StrategyMessageLedger>().TryAccept("report:701"));
        services.GetRequiredService<StrategyForceLordRegistry>().SetLordCharacterId(1, 42);
        services.GetRequiredService<StrategyFieldEngagementRegistry>().SetStandoffDays(10, 20, 3);
        var occupiedStronghold = source.World.GameData.Strongholds.Values.First();
        services.GetRequiredService<StrategyWarOccupationRegistry>().RecordOccupation(
            occupiedStronghold,
            originalForceId: 2,
            occupierForceId: 1,
            occupiedDate: new GameDate(2, 3, 5));

        var captured = StrategyRuntimeServicesSaveService.Capture(services);
        var json = JsonSerializer.Serialize(captured);
        var roundTripped = JsonSerializer.Deserialize<JsonElement>(json);

        using var target = StrategyTestWorldFactory.Create();
        Assert.True(StrategyRuntimeServicesSaveService.TryRestore(roundTripped, target.Services));

        var price = target.Services.GetRequiredService<StrategyIntelligenceLedger>()
            .GetLatestPrice(1, 9);
        Assert.NotNull(price);
        Assert.Equal(73, price.PriceMoneyPerGo);
        Assert.Equal(new GameDate(2, 3, 6), price.ReceivedDate);
        Assert.NotNull(target.Services.GetRequiredService<StrategyEspionageIntelLedger>()
            .TryGet(1, EspionageIntelTargetKind.Stronghold, 9));
        Assert.Equal("在途事件详情", target.Services
            .GetRequiredService<StrategyPendingEventStore>()
            .Take(701)?.Message);
        Assert.Equal(123, target.Services
            .GetRequiredService<StrategyPendingBattleReportStore>()
            .Take(702)?.ResolutionSeed);
        Assert.Equal(321, target.Services.GetRequiredService<MerchantTaxLedger>().GetAccrued(1, 88));
        Assert.Equal(654, target.Services.GetRequiredService<TariffTaxLedger>().GetAccrued(1));
        Assert.True(target.Services.GetRequiredService<TariffTaxLedger>().HasChargedTransit(77, 1));
        Assert.True(target.Services.GetRequiredService<MonthlyTaxCollectionLedger>()
            .ConsumeMoneyTributeObligation(1) > 0);

        var monthly = target.Services.GetRequiredService<StrategyTributeLedger>()
            .ConsumeMonthlySettlement(2, 4);
        Assert.Equal(2_000, monthly.TotalFood);
        Assert.Equal(300, monthly.TotalMoney);
        Assert.False(target.Services.GetRequiredService<StrategyMessageLedger>().TryAccept("report:701"));
        Assert.True(target.Services.GetRequiredService<StrategyForceLordRegistry>()
            .TryGetLordCharacterId(1, out var lordCharacterId));
        Assert.Equal(42, lordCharacterId);
        Assert.Equal(3, target.Services.GetRequiredService<StrategyFieldEngagementRegistry>()
            .GetStandoffDays(10, 20));
        var occupation = Assert.Single(target.Services
            .GetRequiredService<StrategyWarOccupationRegistry>()
            .GetEntriesForStronghold(occupiedStronghold.Id));
        Assert.Equal(2, occupation.OriginalForceId);
        Assert.Equal(new GameDate(2, 3, 5), occupation.OccupiedDate);
    }
}
