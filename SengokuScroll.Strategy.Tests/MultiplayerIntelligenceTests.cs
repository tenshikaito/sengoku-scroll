using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public sealed class MultiplayerIntelligenceTests
{
    [Fact]
    public void DeliveredBattleReport_IsPrivateAndDeduplicatedAcrossRuntimeRestore()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        var meta = StrategyScenarioLoader.ApplyLoadOptions(loaded.Meta, new() { IsMultiplayer = true });
        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);
        var delivery = context.Services.GetRequiredService<BattleReportDeliveryHelper>();
        var mail = context.Services.GetRequiredService<StrategyPrivateEventLedger>();
        var report = new SengokuScroll.Strategy.Models.StrategyBattleResultDto
        {
            AttackerWon = true, AttackerUnitId = 1, DefenderUnitId = 2, AttackerForceId = 1, DefenderForceId = 3,
            AttackerName = "甲", DefenderName = "乙", AttackerSoldiersBefore = 100, DefenderSoldiersBefore = 90,
            AttackerCasualties = 10, DefenderCasualties = 30, AttackerSoldiersAfter = 90, DefenderSoldiersAfter = 60,
            AttackerWinRatePercent = 65, ResolutionSeed = 123, ResolutionRoll = 20,
            EngagementKind = "FieldBattle", LogEntries = [], FactorNotes = []
        };
        var location = context.World.GameData.Strongholds.Values.First(s => s.ForceId == 3).Location;
        delivery.NotifyPlayerBattleReportArrivedFromMessenger(report, location, context.World.GameData, 3);
        Assert.Empty(mail.Read(1).Entries);
        Assert.Equal(123, Assert.Single(mail.Read(3).Entries).Event.BattleResult!.ResolutionSeed);
        var saved = StrategyRuntimeServicesSaveService.Capture(context.Services);
        Assert.True(StrategyRuntimeServicesSaveService.TryRestore(saved, context.Services));
        delivery.NotifyPlayerBattleReportArrivedFromMessenger(report, location, context.World.GameData, 3);
        Assert.Single(mail.Read(3).Entries);
        delivery.NotifyPlayerBattleReportArrivedFromMessenger(report, location, context.World.GameData, 1);
        Assert.Single(mail.Read(1).Entries);
    }
    [Fact]
    public void CommanderEscape_UsesSubunitTableEvenWhenIdsOverlap()
    {
        using var context = StrategyTestWorldFactory.Create();
        var data = context.World.GameData;
        var unit = data.Units.Values.First();
        unit.LeaderId = 101;
        unit.SubUnitIds.Clear(); unit.SubUnitIds.Add(unit.Id);
        data.SubUnits[unit.Id] = new() { Id = unit.Id, LeaderId = 202 };
        Assert.Equal(new[] { 101, 202 }, UnitCommanderEscapeHelper.CollectCommanderIds(unit, data).Order());
    }
    [Fact]
    public void Mailbox_IsPrivateNonDestructiveBoundedAndAcknowledged()
    {
        var ledger = new StrategyPrivateEventLedger();
        ledger.Add(1, new() { Category = "BattleReportArrived", Message = "private one" });
        ledger.Add(2, new() { Category = "EconomyMonthly", Message = "private two" });
        Assert.Equal("private one", Assert.Single(ledger.Read(1).Entries).Event.Message);
        Assert.Equal("private two", Assert.Single(ledger.Read(2).Entries).Event.Message);
        Assert.Single(ledger.Read(1).Entries);
        Assert.False(ledger.Acknowledge(1, 2));
        Assert.True(ledger.Acknowledge(1, 1));
        Assert.True(ledger.Acknowledge(1, 1));
        Assert.Empty(ledger.Read(1).Entries);
        Assert.Single(ledger.Read(2).Entries);
        for (var i = 0; i <= StrategyPrivateEventLedger.CapacityPerForce; i++)
            ledger.Add(1, new() { Category = "Test", Message = "entry" });
        Assert.True(ledger.Read(1).HistoryTruncated);
        Assert.Equal(100, ledger.Read(1).Entries.Count);
        Assert.Equal(StrategyPrivateEventLedger.CapacityPerForce, ledger.Snapshot()[0].Entries.Count);
        var restored = new StrategyPrivateEventLedger();
        restored.Restore(ledger.Snapshot());
        Assert.Equal(JsonSerializer.Serialize(ledger.Snapshot()), JsonSerializer.Serialize(restored.Snapshot()));
    }

    [Fact]
    public void UnaddressedEvents_AreNeverBroadcastInMultiplayer()
    {
        var ledger = new StrategyPrivateEventLedger();
        var buffer = new StrategyDayOutcomeBuffer(new() { PlayerForceId = 1, HasHumanControlConfiguration = true }, ledger);
        buffer.AddEvent(new() { Category = "Diagnostic", Message = "secret" });
        Assert.Empty(ledger.Snapshot());
    }

    [Fact]
    public void MultiplayerMonthlyReports_AreProducedForEachForceAndRestored()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto", new() { IsMultiplayer = true }).IsSuccess);
        Assert.True(host.AdvanceDays(31).IsSuccess);
        foreach (var forceId in new[] { 1, 3 })
        {
            var batch = host.ReadPrivateEvents(forceId);
            Assert.Contains(batch.Entries, e => e.Event.Category == "EconomyMonthly");
            Assert.All(batch.Entries, e => Assert.Equal(forceId, e.Event.RecipientForceId));
        }
        var saved = StrategySimulationHost.DeserializeSave(StrategySimulationHost.SerializeSave(host.CaptureSave().Value)).Value;
        using var restored = new StrategySimulationHost();
        Assert.True(restored.RestoreSave(saved).IsSuccess);
        Assert.Equal(JsonSerializer.Serialize(host.GetState().Value.Characters),
            JsonSerializer.Serialize(restored.GetState().Value.Characters));
        Assert.Equal(JsonSerializer.Serialize(host.ReadPrivateEvents(3)), JsonSerializer.Serialize(restored.ReadPrivateEvents(3)));
        Assert.True(restored.AcknowledgePrivateEvents(3, restored.ReadPrivateEvents(3).LastSequence));
        Assert.Empty(restored.ReadPrivateEvents(3).Entries);
        Assert.NotEmpty(restored.ReadPrivateEvents(1).Entries);
    }

    [Fact]
    public void HiddenCastleChanges_DoNotChangeLastSeenOwnerDefenseOrGarrison()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var ledger = context.Services.GetRequiredService<StrategyVisibilityLedger>();
        var castle = loaded.World.GameData.Strongholds.Values.First(s => s.ForceId != 1);
        var today = loaded.World.GameData.GameDate.TotalDays;
        var snapshots = ledger.SnapshotAll().Select(s => s.ForceId == 1 ? s with
        { Castles = [new(castle.Id, castle.Name, castle.ForceId, castle.Location, 20, 432, today)],
          KnownStrongholdIds = [castle.Id] } : s).ToArray();
        ledger.RestoreAll(snapshots);
        var observation = Assert.Single(ledger.ObserveStrongholds(1, today));
        castle.ForceId = 1; castle.Defense = 99; castle.ForceActor.Soldier = 90000;
        var unchanged = Assert.Single(ledger.ObserveStrongholds(1, today));
        Assert.Equal(observation.ForceId, unchanged.ForceId);
        Assert.Equal(20, unchanged.Defense);
        Assert.Equal(432, unchanged.ForceActor.Soldier);
        var unit = loaded.World.GameData.Units.Values.First();
        unit.DirectiveTargetId = castle.Id;
        Assert.Same(unchanged, StrategyUnitAIRules.ResolveDirectiveHostileStronghold(unit, loaded.World.GameData, [unchanged]));
        Assert.Equal(1000, Assert.Single(ledger.ObserveStrongholds(1, today + 91)).ForceActor.Soldier);
        var runtime = StrategyRuntimeServicesSaveService.Capture(context.Services);
        Assert.True(StrategyRuntimeServicesSaveService.TryRestore(runtime, context.Services));
        Assert.Equal(432, Assert.Single(ledger.ObserveStrongholds(1, today)).ForceActor.Soldier);
        Assert.Throws<ArgumentException>(() => new ForceVisibilityState().UnpackExploredBits([], 10, 10));
    }

    [Fact]
    public void MultiplayerVisibility_DoesNotDependOnRequestPerspective()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        var meta = StrategyScenarioLoader.ApplyLoadOptions(loaded.Meta, new() { IsMultiplayer = true });
        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);
        var ledger = context.Services.GetRequiredService<StrategyVisibilityLedger>();
        ledger.Recompute(context.World, meta);
        var before = JsonSerializer.Serialize(ledger.SnapshotAll());
        ledger.Recompute(context.World, StrategyForcePerspective.Create(meta, context.World.GameData, 3));
        Assert.Equal(before, JsonSerializer.Serialize(ledger.SnapshotAll()));
    }
}
