using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>野战攻/守角色判定（AP 先手，无 buff）。</summary>
public class BattleEngagementResolverTests
{
    [Fact]
    public void ResolveRoles_SingleAttack_OrdererIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Common.Types.Point3(1, 0));
        a.Ap = 3;
        b.Ap = 10;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, false);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.False(both);
    }

    [Fact]
    public void ResolveRoles_MutualAttack_HigherApIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Common.Types.Point3(1, 0));
        a.Ap = 8;
        b.Ap = 3;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, true);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.True(both);
    }

    [Fact]
    public void ResolveRoles_MutualAttack_EqualAp_LowerIdIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Common.Types.Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(1, 2, new Common.Types.Point3(1, 0));
        a.Ap = 5;
        b.Ap = 5;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, true);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.True(both);
    }
}
