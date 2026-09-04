using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Persistence;

/// <summary>
/// 捕获不属于 GameData、但会影响后续仿真的单局服务状态。
/// </summary>
internal static class StrategyRuntimeServicesSaveService
{
    public static JsonElement Capture(IServiceProvider services)
    {
        var snapshot = new StrategyRuntimeServicesSnapshot
        {
            MarketPriceObservations = [.. services
                .GetRequiredService<StrategyIntelligenceLedger>()
                .SnapshotAll()],
            EspionageIntel = [.. services
                .GetRequiredService<StrategyEspionageIntelLedger>()
                .Snapshot()],
            PendingBattleReports = new Dictionary<int, StrategyBattleResultDto>(services
                .GetRequiredService<StrategyPendingBattleReportStore>()
                .Snapshot()),
            PendingEvents = new Dictionary<int, StrategyEventDto>(services
                .GetRequiredService<StrategyPendingEventStore>()
                .Snapshot()),
            MerchantTaxAccruals = [.. services
                .GetRequiredService<MerchantTaxLedger>()
                .Snapshot()],
            TariffTaxAccruals = [.. services
                .GetRequiredService<TariffTaxLedger>()
                .SnapshotAccruals()],
            ChargedTariffTransits = [.. services
                .GetRequiredService<TariffTaxLedger>()
                .SnapshotTransitCharges()],
            MonthlyTaxObligations = [.. services
                .GetRequiredService<MonthlyTaxCollectionLedger>()
                .Snapshot()],
            Tribute = services.GetRequiredService<StrategyTributeLedger>().Snapshot(),
            SeenMessageKeys = [.. services
                .GetRequiredService<StrategyMessageLedger>()
                .Snapshot()],
            ForceLords = new Dictionary<int, int>(services
                .GetRequiredService<StrategyForceLordRegistry>()
                .Snapshot()),
            WarOccupations = [.. services
                .GetRequiredService<StrategyWarOccupationRegistry>()
                .Snapshot()],
            FieldStandoffs = [.. services
                .GetRequiredService<StrategyFieldEngagementRegistry>()
                .Snapshot()]
        };

        return JsonSerializer.SerializeToElement(
            snapshot,
            StrategyWorldSaveService.RuntimeStateSerializationOptions);
    }

    public static bool TryRestore(JsonElement? runtimeServices, IServiceProvider services)
    {
        if (runtimeServices is null)
            return false;

        StrategyRuntimeServicesSnapshot? snapshot;
        try
        {
            snapshot = runtimeServices.Value.Deserialize<StrategyRuntimeServicesSnapshot>(
                StrategyWorldSaveService.RuntimeStateSerializationOptions);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (snapshot is null
            || snapshot.MarketPriceObservations is null
            || snapshot.EspionageIntel is null
            || snapshot.PendingBattleReports is null
            || snapshot.PendingEvents is null
            || snapshot.MerchantTaxAccruals is null
            || snapshot.TariffTaxAccruals is null
            || snapshot.ChargedTariffTransits is null
            || snapshot.MonthlyTaxObligations is null
            || snapshot.Tribute is null
            || snapshot.SeenMessageKeys is null
            || snapshot.ForceLords is null
            || snapshot.WarOccupations is null
            || snapshot.FieldStandoffs is null)
            return false;

        services.GetRequiredService<StrategyIntelligenceLedger>()
            .Restore(snapshot.MarketPriceObservations);
        var scenarioMeta = services.GetService<StrategyScenarioMeta>();
        services.GetRequiredService<StrategyEspionageIntelLedger>()
            .Restore(snapshot.EspionageIntel, scenarioMeta?.PlayerForceId ?? 0);
        services.GetRequiredService<StrategyPendingBattleReportStore>()
            .Restore(snapshot.PendingBattleReports);
        services.GetRequiredService<StrategyPendingEventStore>()
            .Restore(snapshot.PendingEvents);
        services.GetRequiredService<MerchantTaxLedger>()
            .Restore(snapshot.MerchantTaxAccruals);
        services.GetRequiredService<TariffTaxLedger>()
            .Restore(snapshot.TariffTaxAccruals, snapshot.ChargedTariffTransits);
        services.GetRequiredService<MonthlyTaxCollectionLedger>()
            .Restore(snapshot.MonthlyTaxObligations);
        services.GetRequiredService<StrategyTributeLedger>()
            .Restore(snapshot.Tribute);
        services.GetRequiredService<StrategyMessageLedger>()
            .Restore(snapshot.SeenMessageKeys);
        services.GetRequiredService<StrategyForceLordRegistry>()
            .Restore(snapshot.ForceLords);
        services.GetRequiredService<StrategyWarOccupationRegistry>()
            .Restore(snapshot.WarOccupations);
        services.GetRequiredService<StrategyFieldEngagementRegistry>()
            .Restore(snapshot.FieldStandoffs);
        return true;
    }
}

internal sealed class StrategyRuntimeServicesSnapshot
{
    public List<StrategyIntelligenceLedger.MarketPriceObservation> MarketPriceObservations { get; init; } = [];

    public List<StrategyEspionageIntelLedger.Record> EspionageIntel { get; init; } = [];

    public Dictionary<int, StrategyBattleResultDto> PendingBattleReports { get; init; } = [];

    public Dictionary<int, StrategyEventDto> PendingEvents { get; init; } = [];

    public List<MerchantTaxLedger.Accrual> MerchantTaxAccruals { get; init; } = [];

    public List<TariffTaxLedger.Accrual> TariffTaxAccruals { get; init; } = [];

    public List<TariffTaxLedger.TransitCharge> ChargedTariffTransits { get; init; } = [];

    public List<MonthlyTaxCollectionLedger.Obligation> MonthlyTaxObligations { get; init; } = [];

    public StrategyTributeLedger.State Tribute { get; init; } = new([], []);

    public List<string> SeenMessageKeys { get; init; } = [];

    public Dictionary<int, int> ForceLords { get; init; } = [];

    public List<StrategyWarOccupationRegistry.WarOccupationEntry> WarOccupations { get; init; } = [];

    public List<StrategyFieldEngagementRegistry.Standoff> FieldStandoffs { get; init; } = [];
}
