using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public sealed class ReleaseQualityRegressionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CorruptV2_IsRejectedWithoutReplacingOpenWorld(bool multiplayer, bool missingRuntime)
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto", new() { IsMultiplayer = multiplayer }).IsSuccess);
        Assert.True(host.AdvanceDays(3).IsSuccess);
        var before = StrategySimulationHost.SerializeSave(host.CaptureSave().Value);
        var root = JsonNode.Parse(before)!;
        if (missingRuntime) root.AsObject().Remove("runtimeState");
        else root["runtimeState"]!["Units"] = null;
        var parsed = StrategySimulationHost.DeserializeSave(root.ToJsonString());
        Assert.True(parsed.IsSuccess);
        Assert.False(host.RestoreSave(parsed.Value).IsSuccess);
        Assert.Equal(before, StrategySimulationHost.SerializeSave(host.CaptureSave().Value));
    }

    [Fact]
    public void LegacyV1_StillLoadsWithoutRuntimeState()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
        var root = JsonNode.Parse(StrategySimulationHost.SerializeSave(host.CaptureSave().Value))!;
        root["formatVersion"] = 1;
        root.AsObject().Remove("runtimeState");
        Assert.True(host.RestoreSave(StrategySimulationHost.DeserializeSave(root.ToJsonString()).Value).IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JsonRestoreAndContinue_EqualsUninterruptedSixtyDays(bool multiplayer)
    {
        using var continuous = new StrategySimulationHost();
        using var split = new StrategySimulationHost();
        Assert.True(continuous.LoadScenario("mini_kanto", new() { IsMultiplayer = multiplayer, AllForcesAiControlled = true }).IsSuccess);
        Assert.True(split.LoadScenario("mini_kanto", new() { IsMultiplayer = multiplayer, AllForcesAiControlled = true }).IsSuccess);
        Assert.True(continuous.AdvanceDays(30).IsSuccess);
        Assert.True(continuous.AdvanceDays(30).IsSuccess);
        Assert.True(split.AdvanceDays(30).IsSuccess);
        using var restored = new StrategySimulationHost();
        Assert.True(restored.RestoreSave(StrategySimulationHost.DeserializeSave(
            StrategySimulationHost.SerializeSave(split.CaptureSave().Value)).Value).IsSuccess);
        Assert.True(restored.AdvanceDays(30).IsSuccess);
        Assert.Equal(StrategySimulationHost.SerializeSave(continuous.CaptureSave().Value),
            StrategySimulationHost.SerializeSave(restored.CaptureSave().Value));
    }

    [Fact]
    public void Successor_IsResolvedWithoutRegistryAndSurvivesRestore()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var data = context.World.GameData;
        var registry = context.Services.GetRequiredService<StrategyForceLordRegistry>();
        var previous = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(1, loaded.Meta, data, registry);
        var successor = data.Characters.Values.First(c => c.ForceId == 1 && c.Id != previous && !c.IsDead);
        data.Forces[1].Successor = successor.Id;
        data.Characters[previous].IsDead = true;
        ForceSuccessionRules.TryResolveAfterLordRemoved(1, 3, false, true, data, context.World.GameMasterData,
            loaded.Meta, registry, context.Services.GetRequiredService<StrategyDayOutcomeBuffer>(), previous);
        Assert.Equal(successor.Id, StrategyStrongholdLordHelper.ResolveForceLordCharacterId(1, loaded.Meta, data));
        Assert.Equal(successor.Name, StrategyStrongholdLordHelper.ResolveForceLordName(1, loaded.Meta, data));
        var save = StrategyWorldSaveService.Capture(context.World, "mini_kanto", 1,
            context.Services.GetRequiredService<SengokuScroll.Strategy.Vision.StrategyVisibilityLedger>(), loaded.Meta);
        StrategyWorldSaveService.Apply(save, context.World);
        Assert.Equal(successor.Id, StrategyStrongholdLordHelper.ResolveForceLordCharacterId(1, loaded.Meta, data));
    }

    [Fact]
    public void FailedReports_HidePayloadAndDoNotConsumeSuccessfulDeliveryAcrossRestore()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        var meta = StrategyScenarioLoader.ApplyLoadOptions(loaded.Meta, new() { IsMultiplayer = true });
        using var context = StrategySimulationBootstrap.CreateScope(loaded.World, meta);
        var delivery = new BattleReportDeliveryHelper(
            new MessageCarrierDispatchHelper(context.GameContext, new NoPath()),
            context.Services.GetRequiredService<StrategyPendingBattleReportStore>(),
            context.Services.GetRequiredService<StrategyPendingEventStore>(),
            context.Services.GetRequiredService<StrategyDayOutcomeBuffer>(), meta);
        var mail = context.Services.GetRequiredService<StrategyPrivateEventLedger>();
        var destination = new BattleReportRoutingHelper.BattleReportDestination(
            context.World.GameData.Strongholds[1].Location, 1, "居城");
        var battle = new StrategyBattleResultDto
        {
            AttackerWon = true, AttackerUnitId = 1, DefenderUnitId = 2, AttackerForceId = 1, DefenderForceId = 3,
            AttackerName = "secret A", DefenderName = "secret B", AttackerSoldiersBefore = 100, DefenderSoldiersBefore = 90,
            AttackerCasualties = 10, DefenderCasualties = 30, AttackerSoldiersAfter = 90, DefenderSoldiersAfter = 60,
            AttackerWinRatePercent = 65, ResolutionSeed = 123, ResolutionRoll = 20,
            EngagementKind = "FieldBattle", LogEntries = [], FactorNotes = []
        };
        var report = new StrategyEventDto { Category = "Siege", Message = "secret garrison", RecipientForceId = 1, OccurrenceKey = "test" };
        var attacker = context.World.GameData.Units.Values.First(u => u.ForceId == 1 && u.IsMilitary);
        var defender = context.World.GameData.Units.Values.First(u => u.ForceId != 1 && u.IsMilitary);
        delivery.DeliverDecisiveBattleReport(1, attacker.Location, context.World.GameData, default, attacker, defender, battle);
        delivery.NotifyPlayerBattleReportArrived(battle, destination, false, deliveryFailed: true);
        delivery.NotifyPlayerStrategicReportArrived(report, destination, deliveryFailed: true);
        Assert.Equal(2, mail.Read(1).Entries.Count);
        Assert.All(mail.Read(1).Entries, entry =>
        {
            Assert.Equal("ReportDeliveryFailed", entry.Event.Category);
            Assert.Null(entry.Event.BattleResult);
            Assert.Null(entry.Event.DetailMessage);
            Assert.DoesNotContain("secret", entry.Event.Message);
        });
        var runtime = StrategyRuntimeServicesSaveService.Capture(context.Services);
        var deliveryState = delivery.Snapshot();
        Assert.True(StrategyRuntimeServicesSaveService.TryRestore(runtime, context.Services));
        delivery.Restore(deliveryState);
        delivery.NotifyPlayerBattleReportArrived(battle, destination, false, deliveryFailed: true);
        delivery.NotifyPlayerStrategicReportArrived(report, destination, deliveryFailed: true);
        Assert.Equal(2, mail.Read(1).Entries.Count);
        delivery.NotifyPlayerBattleReportArrived(battle, destination, false);
        delivery.NotifyPlayerStrategicReportArrived(report, destination);
        delivery.NotifyPlayerBattleReportArrived(battle, destination, false);
        delivery.NotifyPlayerStrategicReportArrived(report, destination);
        Assert.Equal(4, mail.Read(1).Entries.Count);
        Assert.Single(mail.Read(1).Entries, e => e.Event.BattleResult is not null);
        Assert.Single(mail.Read(1).Entries, e => e.Event.DetailMessage == report.Message);
        Assert.Empty(mail.Read(3).Entries);
    }

    private sealed class NoPath : SengokuScroll.Domain.Services.Pathfinding.IPathfindingService
    {
        public List<SengokuScroll.Domain.Services.Pathfinding.PathNode>? CalculatePath(
            SengokuScroll.Domain.Entities.Abstraction.IMovable movable, SengokuScroll.Common.Types.Point2 target) => null;
        public List<SengokuScroll.Domain.Services.Pathfinding.PathNode>? CalculatePathFrom(
            SengokuScroll.Common.Types.Point2 start, SengokuScroll.Common.Types.Point2 target,
            SengokuScroll.Domain.Entities.Abstraction.IMovable movable) => null;
        public List<SengokuScroll.Domain.Services.Pathfinding.PathNode>? CalculatePathFrom(
            SengokuScroll.Common.Types.Point2 start, SengokuScroll.Common.Types.Point2 target,
            SengokuScroll.Domain.Entities.Abstraction.IMovable movable, Func<SengokuScroll.Common.Types.Point2, bool>? blocked) => null;
    }
}
