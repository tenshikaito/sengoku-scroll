using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Actions;

namespace SengokuScroll.Strategy.Tests;

public class WarAndStackingRulesTests
{
    [Fact]
    public void CanMilitaryStack_SameForce_True()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        Assert.True(WarRules.CanMilitaryStack(1, 1, data));
    }

    [Fact]
    public void CanMilitaryStack_PeacetimeAllies_False()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        Assert.False(WarRules.CanMilitaryStack(1, 2, data));
    }

    [Fact]
    public void CreateWar_JoinSameSide_AllowsStack()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        data.Forces[3] = StrategyTestWorldBuilder.CreateTestForce(3);

        var war = WarRules.CreateWar(data, 1, 2, data.GameDate);
        Assert.True(WarRules.TryJoinWar(war, 3, joinAggressorSide: true));
        Assert.True(WarRules.CanMilitaryStack(1, 3, data));
        Assert.True(WarRules.AreWarEnemies(3, 2, data));
    }

    [Fact]
    public void AddWarScore_UsesParticipantPerspectiveAndClampsAtOneHundred()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        var war = WarRules.CreateWar(data, 1, 2, data.GameDate);

        Assert.Equal(15, WarRules.AddWarScore(
            war, 1, 2, 15, data.GameDate, "BattleVictory"));
        Assert.Equal(15, WarRules.GetWarScoreForForce(war, 1));
        Assert.Equal(-15, WarRules.GetWarScoreForForce(war, 2));

        Assert.Equal(85, WarRules.AddWarScore(
            war, 1, 2, 200, data.GameDate, "StrongholdOccupied"));
        Assert.Equal(100, war.AggressorWarScore);
        Assert.Equal(2, war.WarScoreEvents.Count);

        Assert.Equal(200, WarRules.AddWarScore(
            war, 2, 1, 250, data.GameDate, "Counterattack"));
        Assert.Equal(-100, war.AggressorWarScore);
        Assert.Equal(100, WarRules.GetWarScoreForForce(war, 2));
    }

    [Fact]
    public void StrategyWarScore_BattleAndOccupationUseDesignedRanges()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(10, 1, new Common.Types.Point3(1, 1));
        var defender = StrategyTestWorldBuilder.CreateTestUnit(11, 2, new Common.Types.Point3(2, 1));
        attacker.Soldier = 800;
        defender.Soldier = 0;
        data.Units[10] = attacker;
        data.Units[11] = defender;
        var war = WarRules.CreateWar(data, 1, 2, data.GameDate);

        var battleDelta = StrategyWarScoreRules.RecordBattleOutcome(
            data,
            attacker,
            defender,
            new InstantBattleOutcome(true, 70, 200, 1_000, 1, 1, 1_000, 1_000));
        Assert.InRange(battleDelta, 5, 15);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(
            20, 1, new Common.Types.Point3(3, 1));
        stronghold.Scale = 30;
        var occupationDelta = StrategyWarScoreRules.RecordStrongholdOccupation(
            data, stronghold, 1, 2);
        Assert.Equal(30, occupationDelta);
        Assert.Equal(battleDelta + occupationDelta, war.AggressorWarScore);
    }

    [Fact]
    public void SeparatePeace_DetachesParticipantBattlefieldButKeepsMainWarRunning()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var data = ctx.World.GameData;
        data.Forces[2] = StrategyTestWorldBuilder.CreateTestForce(2);
        data.Forces[3] = StrategyTestWorldBuilder.CreateTestForce(3);
        var war = WarRules.CreateWar(data, 1, 2, data.GameDate);
        Assert.True(WarRules.TryJoinWar(war, 3, joinAggressorSide: true));

        var ally = StrategyTestWorldBuilder.CreateTestUnit(30, 3, new Common.Types.Point3(4, 4));
        var defender = StrategyTestWorldBuilder.CreateTestUnit(31, 2, new Common.Types.Point3(4, 4));
        ally.Soldier = 500;
        defender.Soldier = 500;
        ally.BattlefieldId = 40;
        defender.BattlefieldId = 40;
        data.Units[30] = ally;
        data.Units[31] = defender;
        data.Battlefields[40] = new Battlefield
        {
            Id = 40,
            Kind = BattlefieldKind.Field,
            Location = new Common.Types.Point2(4, 4),
            WarId = war.Id,
            SideAUnitIds = [30],
            SideBUnitIds = [31],
        };

        Assert.True(ForceDiplomacyActions.TrySetRelation(
            data, 3, 2, Diplomacy.DiplomacyRelation.Neutral, out _));

        Assert.False(war.IsEnded);
        Assert.DoesNotContain(3, war.AggressorForceIds);
        Assert.True(data.Battlefields[40].IsClosed);
        Assert.Equal(0, ally.BattlefieldId);
        Assert.True(WarRules.AreWarEnemies(1, 2, data));
    }

    [Fact]
    public void SameTile_TwoFriendlyUnits_IndexHasBoth()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var world = ctx.World;
        var a = StrategyTestWorldBuilder.CreateTestUnit(10, 1, new Common.Types.Point3(3, 3));
        var b = StrategyTestWorldBuilder.CreateTestUnit(11, 1, new Common.Types.Point3(3, 3));
        world.GameData.Units[10] = a;
        world.GameData.Units[11] = b;
        Domain.Actions.MapLocationActions.RegisterUnit(world, a);
        Domain.Actions.MapLocationActions.RegisterUnit(world, b);

        var idx = world.GameMapMasterData.TileMap.GetIndex(a.Location);
        Assert.Equal(2, world.GameMapData.Units[idx].Count);
        Assert.Contains(10, world.GameMapData.Units[idx]);
        Assert.Contains(11, world.GameMapData.Units[idx]);
    }
}
