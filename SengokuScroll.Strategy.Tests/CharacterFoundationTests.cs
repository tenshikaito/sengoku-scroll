using System.Text.Json;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public sealed class CharacterFoundationTests
{
    [Fact]
    public void Scenario_HasConfiguredPersonalitiesAndNoInventedEventViews()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
        var state = host.GetState().Value;
        Assert.True(state.Characters.Select(c => JsonSerializer.Serialize(c.Personality)).Distinct().Count() >= 11);
        Assert.All(state.Characters, c => Assert.All(c.CharacterRelationships, r => Assert.Empty(r.ViewEffects)));
        var save = StrategySimulationHost.SerializeSave(host.CaptureSave().Value);
        using var restored = new StrategySimulationHost();
        Assert.True(restored.RestoreSave(StrategySimulationHost.DeserializeSave(save).Value).IsSuccess);
        Assert.Equal(save, StrategySimulationHost.SerializeSave(restored.CaptureSave().Value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Scenario_RejectsOutOfRangePersonality(int value)
    {
        Assert.Throws<ArgumentException>(() => StrategyScenarioCharacterFactory.Create(
            new StrategyCharacterDefinition { Id = 1, Name = "test", ForceId = 1,
                Personality = new() { Action = value } }, default, Character.CharacterLocationType.Map));
    }

    [Fact]
    public void MissingPersonality_RemainsNeutral()
    {
        var character = StrategyScenarioCharacterFactory.Create(new() { Id = 1, Name = "test", ForceId = 1 },
            default, Character.CharacterLocationType.Map);
        Assert.Equal(50, character.Personality.Action);
        Assert.Equal(50, character.Personality.Friendship);
    }

    [Fact]
    public void RelationshipTone_UsesDirectionalEffectiveValueNotEnemyIdentityOrPersonality()
    {
        using var context = StrategyTestWorldFactory.Create();
        var people = context.World.GameData.Characters;
        var actor = StrategyScenarioCharacterFactory.Create(new() { Id = 1, Name = "actor", ForceId = 1 },
            default, Character.CharacterLocationType.Map);
        people[actor.Id] = actor;
        var target = StrategyScenarioCharacterFactory.Create(new() { Id = 999, Name = "target", ForceId = 1 },
            default, Character.CharacterLocationType.Map);
        people[target.Id] = target;
        actor.EnemyIds.Add(target.Id);
        actor.Relationships.Add(new() { OwnerCharacterId = actor.Id, TargetCharacterId = target.Id, Relationship = 80 });
        target.Relationships.Add(new() { OwnerCharacterId = target.Id, TargetCharacterId = actor.Id, Relationship = -80 });
        Assert.Equal("亲密", CharacterRelationsHelper.BuildRelations(actor, people).First(r => r.CharacterId == target.Id).RelationTone);
        Assert.Equal("仇视", CharacterRelationsHelper.BuildRelations(target, people).First(r => r.CharacterId == actor.Id).RelationTone);
        actor.EnemyIds.Clear();
        Assert.Contains(CharacterRelationsHelper.BuildRelations(actor, people), r => r.CharacterId == target.Id && r.RelationType == "相识");
    }

    [Fact]
    public void Expiry_RemovesOnlyExpiredTemporaryEffectsAndDoesNotMutateBaseValues()
    {
        using var context = StrategyTestWorldFactory.Create();
        var data = context.World.GameData;
        data.GameDate = new GameDate(1560, 1, 2);
        var character = StrategyScenarioCharacterFactory.Create(new() { Id = 1, Name = "actor", ForceId = 1 },
            default, Character.CharacterLocationType.Map);
        data.Characters[character.Id] = character;
        character.Loyalty = 50;
        var expired = new EntityEffect { Id = 1, Name = "expired", TargetStat = EffectTargetStat.Loyalty,
            Magnitude = 20, Duration = EffectDurationKind.Temporary, ExpiresOn = data.GameDate };
        character.ActiveEffects.Add(expired);
        Assert.Equal(50, EntityEffectHelper.ResolveEffectiveLoyalty(character, data.GameDate));
        var relation = new CharacterRelationship { TargetCharacterId = 999, Relationship = 10, Trust = 20,
            ViewEffects = [new() { Id = 2, Name = "promise", TargetStat = EffectTargetStat.Relationship,
                Magnitude = 30, Duration = EffectDurationKind.Permanent }] };
        character.Relationships.Add(relation);
        Assert.Equal(40, CharacterRelationshipRules.Resolve(relation, today: data.GameDate));
        Assert.Equal(20, CharacterRelationshipRules.Resolve(relation, trust: true, today: data.GameDate));
        EntityEffectExpiryHelper.RemoveExpired(data);
        Assert.Empty(character.ActiveEffects);
        Assert.Single(relation.ViewEffects);
        Assert.Equal(10, relation.Relationship);
    }
}
