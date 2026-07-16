using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Tests.Fixtures;

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
