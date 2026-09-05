using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public sealed class CharacterDecisionTests
{
    private static CharacterDecisionInput Neutral => new(0, 0, 50, 50, 50, 50, 50, 50, 50, 0, 100, 50, 0, 0);
    private static string State(Character actor) => JsonSerializer.Serialize(actor, StrategyWorldSaveService.RuntimeStateSerializationOptions);

    [Fact]
    public void SameRelationship_DifferentPersonalityChangesMarriageChoice()
    {
        var input = Neutral with { Opinion = 60, Trust = 30 };
        Assert.True(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Marriage,
            input with { Friendship = 100, Caution = 0 }).Preferred);
        Assert.False(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Marriage,
            input with { Friendship = 0, Caution = 100 }).Preferred);
    }

    [Fact]
    public void RecentRejectionCanChangeBorderlineMarriageDecision()
    {
        var input = Neutral with { Opinion = 60, Trust = 40 };
        Assert.True(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Marriage, input).Preferred);
        Assert.False(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Marriage,
            input with { RejectionPenalty = 12 }).Preferred);
    }

    [Fact]
    public void WarmthAndConditionCanChangeSocialChoiceToWaiting()
    {
        Assert.True(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Social, Neutral).Preferred);
        Assert.False(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Social,
            Neutral with { Temper = 0, Friendship = 0, Emotion = -100, Hp = 50 }).Preferred);
    }

    [Fact]
    public void LoyaltyAndDefectionUseDifferentWeights()
    {
        var loyal = Neutral with { Opinion = 50, Friendship = 100, Ambition = 0 };
        Assert.True(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Loyalty, loyal).Preferred);
        Assert.False(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Loyalty,
            loyal with { Friendship = 0, Ambition = 100 }).Preferred);
        var unhappy = Neutral with { Opinion = -80, Loyalty = 10, Ambition = 90 };
        Assert.True(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Defection, unhappy).Preferred);
        Assert.False(CharacterDecisionRules.Evaluate(CharacterDecisionKind.Defection,
            unhappy with { Friendship = 100 }).Preferred);
    }

    [Fact]
    public void ReliefCautionAndJudgmentChangeRiskNotRelationship()
    {
        var brave = CharacterDecisionRules.Evaluate(CharacterDecisionKind.Relief,
            Neutral with { Courage = 100, Caution = 0, Strategy = 100 });
        var cautious = CharacterDecisionRules.Evaluate(CharacterDecisionKind.Relief,
            Neutral with { Courage = 0, Caution = 100, Strategy = 0 });
        Assert.True(brave.Score < cautious.Score);
        Assert.Equal(0, Neutral.Opinion);
    }

    [Fact]
    public void MemoryEffectsDecayIgnoreFutureAndDoNotApplyRelationshipTwice()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var actor = NewCharacter(501);
        var data = ctx.World.GameData; var today = data.GameDate.TotalDays;
        actor.Relationships.Add(new() { OwnerCharacterId = actor.Id, TargetCharacterId = 502, Relationship = 60, Trust = 40 });
        actor.SocialMemories.AddRange([
            new(1, today, 502, "Talk", "talk"), new(2, today, 502, "MarriageDeclined", "declined"),
            new(3, today + 1, 502, "Talk", "future"), new(4, today, 503, "Talk", "someone else")]);
        var before = State(actor);
        var input = CharacterDecisionRules.Capture(actor, 502, data);
        Assert.Equal(7, input.RecentTalkPenalty); Assert.Equal(12, input.RejectionPenalty);
        Assert.Equal(60, input.Opinion); Assert.Equal(before, State(actor));
        data.GameDate = data.GameDate.AddDays(61);
        input = CharacterDecisionRules.Capture(actor, 502, data);
        Assert.Equal(0, input.RecentTalkPenalty); Assert.Equal(0, input.RejectionPenalty);
        Assert.Equal(60, input.Opinion);
    }

    [Fact]
    public void OrderedParallelScoresMatchSequentialAndFactorSums()
    {
        var inputs = Enumerable.Range(0, 4096).Select(i => Neutral with
        { Opinion = i % 201 - 100, Friendship = i % 101, Caution = (i * 3) % 101 }).ToArray();
        foreach (var kind in Enum.GetValues<CharacterDecisionKind>())
        {
            var expected = inputs.Select(i => CharacterDecisionRules.Evaluate(kind, i)).ToArray();
            var actual = StrategyParallelWork.MapOrdered(inputs, i => CharacterDecisionRules.Evaluate(kind, i), 64);
            Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
            Assert.All(actual, result => Assert.Equal(result.Score, result.Factors.Sum(f => f.Value)));
        }
    }

    [Fact]
    public void DecisionHistoryBoundedAndJsonRoundTrips()
    {
        var actor = NewCharacter(501);
        for (var day = 0; day < 1000; day++)
            foreach (var kind in Enum.GetValues<CharacterDecisionKind>())
                CharacterDecisionRules.Remember(actor, day, kind, 502,
                    CharacterDecisionRules.Evaluate(kind, Neutral), "evaluated, not executed");
        Assert.Equal(5, actor.RecentDecisions.Count);
        Assert.All(actor.RecentDecisions, d => Assert.Equal(999, d.Day));
        Assert.Empty(actor.SocialMemories);
        var json = State(actor);
        Assert.Equal(json, State(JsonSerializer.Deserialize<Character>(json, StrategyWorldSaveService.RuntimeStateSerializationOptions)!));
    }

    [Fact]
    public void DecisionStateIsPrivateAndPersistsThroughFullSave()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto", new() { IsMultiplayer = true }).IsSuccess);
        var root = System.Text.Json.Nodes.JsonNode.Parse(StrategySimulationHost.SerializeSave(host.CaptureSave().Value))!;
        root["runtimeState"]!["Characters"]!["1"]!["RecentDecisions"] = System.Text.Json.Nodes.JsonNode.Parse(
            JsonSerializer.Serialize(new[] { new CharacterDecisionRecord(1, "Marriage", 2, 70, 60, "private", [new("trust", 70)]) }));
        Assert.True(host.RestoreSave(StrategySimulationHost.DeserializeSave(root.ToJsonString()).Value).IsSuccess);
        Assert.Single(host.GetState().Value.Characters.First(c => c.Id == 1).RecentDecisions);
        var saved = StrategySimulationHost.SerializeSave(host.CaptureSave().Value);
        Assert.True(host.RestoreSave(StrategySimulationHost.DeserializeSave(saved).Value).IsSuccess);
        Assert.Equal(saved, StrategySimulationHost.SerializeSave(host.CaptureSave().Value));
        using var perspective = host.UsePlayerForce(3).Value;
        Assert.All(host.GetState().Value.Characters.Where(c => c.ForceId != 3), c => Assert.Empty(c.RecentDecisions));
    }

    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(100, 0, true)]
    public void NpcMarriageActuallyUsesPersonality(byte friendship, byte caution, bool accepts)
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var data = ctx.World.GameData; var proposer = data.Characters[1];
        var actor = NewCharacter(501);
        actor.Personality.Friendship = friendship; actor.Personality.Action = caution;
        actor.Relationships.Add(new() { OwnerCharacterId = actor.Id, TargetCharacterId = 1, Relationship = 60, Trust = 30 });
        data.Characters[actor.Id] = actor;
        Assert.True(CharacterMarriageActions.ProposeOrAccept(data, proposer, actor, out _).IsSuccess);
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(accepts ? 1 : 0, actor.SpouseId);
        Assert.Contains(actor.RecentDecisions, d => d.Behavior == "Marriage");
        var json = State(actor);
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(json, State(actor));
    }

    [Fact]
    public void RestingNpcIsNotInterruptedBySocialAi()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var actor = NewCharacter(501); actor.ActionStatus = Character.CharacterActionStatus.Resting;
        ctx.World.GameData.Characters[501] = actor;
        CharacterSocietyActions.AdvanceDay(ctx.World.GameData, loaded.Meta, ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Equal(100, actor.Ap); Assert.Empty(actor.RecentDecisions); Assert.Empty(actor.SocialMemories);
    }

    [Theory]
    [InlineData(false, 502)]
    [InlineData(true, 503)]
    public void SocialSelectionHasStableTieBreakAndSkipsCoolingDownTarget(bool cooling, int expected)
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var data = ctx.World.GameData;
        foreach (var character in data.Characters.Values) character.ForceStatus = Character.CharacterForceStatus.Task;
        while (data.GameDate.TotalDays % 7 != 501 % 7) data.GameDate = data.GameDate.AddDays();
        var actor = NewCharacter(501);
        foreach (var id in new[] { 503, 502 }) // Reverse insertion must not determine the winner.
        {
            data.Characters[id] = NewCharacter(id);
            actor.Relationships.Add(new() { OwnerCharacterId = 501, TargetCharacterId = id,
                Relationship = 80, Trust = 80, LastTalkDay = cooling && id == 502 ? data.GameDate.TotalDays : null });
        }
        data.Characters[501] = actor;
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>());
        Assert.Contains(actor.SocialMemories, m => m.Kind == "Talk" && m.OtherCharacterId == expected);
        Assert.Equal("主动交谈", actor.RecentDecisions.Single(d => d.Behavior == "Social").Outcome);
    }

    [Fact]
    public void DefectionIntentUsesHysteresisAndCancelsOnReconciliation()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json"));
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var data = ctx.World.GameData; var actor = NewCharacter(501);
        actor.Loyalty = 10; actor.Personality.Ambition = 90;
        var relation = new CharacterRelationship { OwnerCharacterId = 501, TargetCharacterId = 1, Relationship = -80 };
        actor.Relationships.Add(relation); data.Characters[501] = actor;
        var events = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, events);
        var warningDay = actor.DefectionWarningDay; Assert.NotNull(warningDay);
        relation.Relationship = -20; data.GameDate = data.GameDate.AddDays();
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, events);
        Assert.Equal(warningDay, actor.DefectionWarningDay);
        Assert.Equal(65, actor.RecentDecisions.Single(d => d.Behavior == "Defection").Threshold);
        relation.Relationship = 0; data.GameDate = data.GameDate.AddDays();
        CharacterSocietyActions.AdvanceDay(data, loaded.Meta, events);
        Assert.Null(actor.DefectionWarningDay);
    }

    private static Character NewCharacter(int id)
    {
        var actor = StrategyScenarioCharacterFactory.Create(new() { Id = id, Name = $"person{id}", ForceId = 1,
            StrongholdId = 1, BirthYear = 1530 }, default, Character.CharacterLocationType.Stronghold, 1);
        actor.Ap = 100; actor.Hp = 100; actor.Emotion = 0;
        return actor;
    }
}
