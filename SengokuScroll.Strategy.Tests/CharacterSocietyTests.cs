using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public sealed class CharacterSocietyTests
{
    [Fact]
    public void DisconnectedMultiplayerLordStillRequiresExplicitMarriageConsent()
    {
        var (context, originalMeta) = World(); using var scope = context;
        var meta = StrategyScenarioLoader.ApplyLoadOptions(originalMeta, new() { IsMultiplayer = true });
        var data = context.World.GameData; var lord = data.Characters[1]; var npc = Add(data, 501);
        Rel(lord, npc, 80);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, npc, lord, out _).IsSuccess);
        CharacterSocietyActions.AdvanceDay(data, meta, context.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(0, lord.SpouseId);
        Assert.Equal(npc.Id, lord.PendingMarriageFromId);
    }
    [Fact]
    public void SocialMemories_AreNotExposedToAnotherMultiplayerForce()
    {
        using var host = new SengokuScroll.Strategy.Hosting.StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto", new() { IsMultiplayer = true }).IsSuccess);
        var root = System.Text.Json.Nodes.JsonNode.Parse(
            SengokuScroll.Strategy.Hosting.StrategySimulationHost.SerializeSave(host.CaptureSave().Value))!;
        root["runtimeState"]!["Characters"]!["1"]!["SocialMemories"] = System.Text.Json.Nodes.JsonNode.Parse(
            System.Text.Json.JsonSerializer.Serialize(new[] { new CharacterSocialMemory(1, 1, 2, "Test", "private") }));
        Assert.True(host.RestoreSave(SengokuScroll.Strategy.Hosting.StrategySimulationHost.DeserializeSave(root.ToJsonString()).Value).IsSuccess);
        Assert.Single(host.GetState().Value.Characters.First(c => c.Id == 1).SocialMemories);
        using var perspective = host.UsePlayerForce(3).Value;
        Assert.All(host.GetState().Value.Characters.Where(c => c.ForceId != 3), c => Assert.Empty(c.SocialMemories));
    }

    [Fact]
    public void ExpiredProposalCannotBeAcceptedAndDailyAiIsIdempotent()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, a, b, out _).IsSuccess);
        data.GameDate = data.GameDate.AddDays(30);
        var events = context.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        CharacterSocietyActions.AdvanceDay(data, meta, events);
        Assert.Equal(0, b.PendingMarriageFromId);
        Assert.Equal(0, a.SpouseId);
        var memories = b.SocialMemories.Count;
        CharacterSocietyActions.AdvanceDay(data, meta, events);
        Assert.Equal(memories, b.SocialMemories.Count);
    }
    private static (StrategyTestContext Context, StrategyScenarioMeta Meta) World()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        return (StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta), loaded.Meta);
    }
    private static Character Add(GameData data, int id, int force = 1, int castle = 1)
    {
        var c = StrategyScenarioCharacterFactory.Create(new() { Id = id, Name = $"Person{id}", ForceId = force,
            StrongholdId = castle, BirthYear = 1530 }, data.Strongholds[castle].Location,
            Character.CharacterLocationType.Stronghold, castle);
        c.Ap = 100; c.Money = 10000;
        data.Characters[id] = c; return c;
    }
    private static void Rel(Character a, Character b, int value)
        => a.Relationships.Add(new() { OwnerCharacterId = a.Id, TargetCharacterId = b.Id,
            Relationship = (sbyte)value, Trust = (sbyte)value });

    [Fact]
    public void Meetings_CoolDownBothDirectionsAndDoNotChargeRejectedAttempt()
    {
        var (context, _) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Assert.True(CharacterSocialActions.PerformMeeting(data, a, b, "Talk", out _).IsSuccess);
        var ap = b.Ap;
        Assert.False(CharacterSocialActions.PerformMeeting(data, b, a, "Talk", out _).IsSuccess);
        Assert.Equal(ap, b.Ap);
        Assert.True(CharacterSocialActions.PerformMeeting(data, a, b, "Gift", out _).IsSuccess);
        var money = a.Money; var receiverMoney = b.Money;
        data.GameDate = data.GameDate.AddDays(6);
        Assert.False(CharacterSocialActions.PerformMeeting(data, a, b, "Gift", out _).IsSuccess);
        Assert.Equal(money, a.Money); Assert.Equal(receiverMoney, b.Money);
        data.GameDate = data.GameDate.AddDays();
        Assert.True(CharacterSocialActions.PerformMeeting(data, a, b, "Gift", out _).IsSuccess);
    }

    [Fact]
    public void Memories_BoundedButCooldownSurvivesEvictionAndSaveRestore()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Assert.True(CharacterSocialActions.PerformMeeting(data, a, b, "Gift", out _).IsSuccess);
        for (int i = 0; i < 1000; i++) CharacterSocialHistory.Record(a, b.Id, data.GameDate.TotalDays, "Test", "test");
        Assert.Equal(64, a.SocialMemories.Count);
        var save = StrategyWorldSaveService.Capture(context.World, "mini_kanto", 1,
            context.Services.GetRequiredService<StrategyVisibilityLedger>(), meta);
        StrategyWorldSaveService.Apply(save, context.World);
        Assert.Equal(64, data.Characters[a.Id].SocialMemories.Count);
        Assert.False(CharacterSocialActions.PerformMeeting(data, data.Characters[a.Id], data.Characters[b.Id], "Gift", out _).IsSuccess);
    }

    [Fact]
    public void MarriageProposalAndWarningSurviveWorldRestore()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, a, b, out _).IsSuccess);
        a.DefectionWarningDay = data.GameDate.TotalDays;
        var save = StrategyWorldSaveService.Capture(context.World, "mini_kanto", 1,
            context.Services.GetRequiredService<StrategyVisibilityLedger>(), meta);
        StrategyWorldSaveService.Apply(save, context.World);
        Assert.Equal(a.DefectionWarningDay, data.Characters[a.Id].DefectionWarningDay);
        Assert.Equal(a.Id, data.Characters[b.Id].PendingMarriageFromId);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, data.Characters[b.Id], data.Characters[a.Id], out _).IsSuccess);
        Assert.Equal(b.Id, data.Characters[a.Id].SpouseId);
    }

    [Fact]
    public void PositiveRelationshipsHaveDiminishingReturns()
    {
        var (context, _) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Rel(a, b, 80); Rel(b, a, 80);
        Assert.True(CharacterSocialActions.PerformMeeting(data, a, b, "Gift", out _).IsSuccess);
        Assert.Equal(81, a.Relationships[0].Relationship); Assert.Equal(81, b.Relationships[0].Relationship);
    }

    [Fact]
    public void Marriage_NeedsReciprocalConsentAndCannotBeRepeated()
    {
        var (context, _) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, a, b, out _).IsSuccess);
        Assert.Equal(0, a.SpouseId); Assert.Equal(0, b.SpouseId);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, b, a, out _).IsSuccess);
        Assert.Equal(b.Id, a.SpouseId); Assert.Equal(a.Id, b.SpouseId);
        var ap = a.Ap;
        Assert.False(CharacterMarriageActions.ProposeOrAccept(data, a, b, out _).IsSuccess);
        Assert.Equal(ap, a.Ap);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Marriage_RejectsMinorOrCloseKin(bool minor)
    {
        var (context, _) = World(); using var scope = context;
        var data = context.World.GameData; var a = Add(data, 501); var b = Add(data, 502);
        if (minor) b.Birthday = data.GameDate.AddYears(-17);
        else { a.FatherId = 900; b.FatherId = 900; }
        Assert.False(CharacterMarriageActions.ProposeOrAccept(data, a, b, out _).IsSuccess);
        Assert.Equal(100, a.Ap); Assert.Equal(0, b.PendingMarriageFromId);
    }

    [Fact]
    public void NpcAcceptsTrustedProposal_ButHumanLordNeverAutoConsents()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData;
        var lord = data.Characters[1]; var npc = Add(data, 501);
        Rel(npc, lord, 80);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, lord, npc, out _).IsSuccess);
        CharacterSocietyActions.AdvanceDay(data, meta, context.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(lord.Id, npc.SpouseId);
        lord.SpouseId = npc.SpouseId = 0;
        data.GameDate = data.GameDate.AddDays(31);
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, npc, lord, out _).IsSuccess);
        CharacterSocietyActions.AdvanceDay(data, meta, context.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(0, lord.SpouseId); Assert.Equal(npc.Id, lord.PendingMarriageFromId);
    }

    [Fact]
    public void Defection_RequiresThirtyDayWarningAndSafeUnassignedCharacter()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData;
        var castle = data.Strongholds.Values.First(s => s.ForceId != 1
            && StrategyStrongholdLordHelper.ResolveForceLordCharacterId(s.ForceId, meta, data) > 0);
        var actor = Add(data, 501, 1, castle.Id);
        actor.Personality.Ambition = 90; actor.Loyalty = 10;
        Rel(actor, data.Characters[1], -80);
        Rel(actor, data.Characters[StrategyStrongholdLordHelper.ResolveForceLordCharacterId(castle.ForceId, meta, data)], 80);
        var events = context.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        CharacterSocietyActions.AdvanceDay(data, meta, events);
        Assert.NotNull(actor.DefectionWarningDay); Assert.Equal(1, actor.ForceId);
        data.GameDate = data.GameDate.AddDays(29);
        CharacterSocietyActions.AdvanceDay(data, meta, events); Assert.Equal(1, actor.ForceId);
        actor.Position = 1;
        data.GameDate = data.GameDate.AddDays();
        CharacterSocietyActions.AdvanceDay(data, meta, events); Assert.Equal(1, actor.ForceId);
        actor.Position = 0;
        data.GameDate = data.GameDate.AddDays();
        CharacterSocietyActions.AdvanceDay(data, meta, events);
        Assert.Equal(castle.ForceId, actor.ForceId);
        Assert.Contains(actor.SocialMemories, m => m.Kind == "Defected");
    }

    [Fact]
    public void ReconciliationCancelsDefectionWarning()
    {
        var (context, meta) = World(); using var scope = context;
        var data = context.World.GameData; var actor = Add(data, 501);
        actor.Personality.Ambition = 90; actor.Loyalty = 10; Rel(actor, data.Characters[1], -80);
        var events = context.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        CharacterSocietyActions.AdvanceDay(data, meta, events); Assert.NotNull(actor.DefectionWarningDay);
        actor.Relationships[0].Relationship = 0; data.GameDate = data.GameDate.AddDays();
        CharacterSocietyActions.AdvanceDay(data, meta, events);
        Assert.Null(actor.DefectionWarningDay);
    }
}
