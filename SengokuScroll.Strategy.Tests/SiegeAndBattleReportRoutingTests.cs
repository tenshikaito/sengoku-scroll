using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class SiegeBattleRulesTests
{
    [Fact]
    public void Classify_DefenderOnOwnStronghold_AttackerAdjacent_IsSiege()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(4, 4));
        attacker.Directive = UnitDirective.Occupy;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(5, 4));
        defender.Directive = UnitDirective.Support;
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, new Point3(5, 4));
        stronghold.Defense = 60;

        using var ctx = StrategyTestWorldFactory.Create();
        ctx.World.GameData.Units[1] = attacker;
        ctx.World.GameData.Units[2] = defender;
        ctx.World.GameData.Strongholds[10] = stronghold;

        var kind = BattleEngagementClassifier.Classify(attacker, defender, ctx.World.GameData);
        Assert.Equal(BattleEngagementKind.Siege, kind);
        Assert.True(SiegeBattleRules.IsStrongholdGarrison(defender, ctx.World.GameData));
    }

    [Fact]
    public void Classify_AttackerInsideDefenderStronghold_IsSiege()
    {
        var castle = new Point3(5, 4);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.Defense = 60;

        using var ctx = StrategyTestWorldFactory.Create();
        ctx.World.GameData.Units[1] = attacker;
        ctx.World.GameData.Units[2] = defender;
        ctx.World.GameData.Strongholds[10] = stronghold;

        var kind = BattleEngagementClassifier.Classify(attacker, defender, ctx.World.GameData);
        Assert.Equal(BattleEngagementKind.Siege, kind);
        Assert.True(
            SiegeBattleRules.EffectiveSiegeSoldierCount(attacker, defender, ctx.World.GameData)
            > defender.Soldier);
    }
}

public class BattleReportRoutingTests
{
    [Fact]
    public void ResolveDestinations_PlayerForce_IncludesLordAndResidence()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        var attacker = ctx.World.GameData.Units[1];
        var defender = ctx.World.GameData.Units[2];
        var meta = loaded.Meta;

        var destinations = BattleReportRoutingHelper.ResolveDestinations(
            meta.PlayerForceId,
            meta,
            ctx.World.GameData,
            attacker,
            defender);

        Assert.NotEmpty(destinations);
        Assert.Contains(destinations, d => d.Label == "当主" || d.Label == "居城");
    }
}
