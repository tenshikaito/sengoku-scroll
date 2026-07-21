using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>部队合并、分兵与居城出征 Host 联调。</summary>
public class UnitMergeSplitDeployTests
{
    [Fact]
    public void Host_AdjacentFriendlyUnits_CanMergeSubUnits()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        var source = world.GameData.Units[1];
        var target = CloneAdjacentFriendlyUnit(world, source, new Point3(9, 8));

        var sourceSubCount = source.SubUnitIds.Count;
        var targetSubCount = target.SubUnitIds.Count;
        var totalSoldiers = source.Soldier + target.Soldier;

        var result = host.OrderUnitMerge(source.Id, target.Id);
        Assert.True(result.IsSuccess);
        Assert.False(world.GameData.Units.ContainsKey(source.Id));
        Assert.True(world.GameData.Units.TryGetValue(target.Id, out var merged));
        Assert.Equal(totalSoldiers, merged.Soldier);
        Assert.True(merged.SubUnitIds.Count <= sourceSubCount + targetSubCount);
    }

    [Fact]
    public void Host_UnitWithSubUnits_CanSplitToAdjacentTile()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        var parent = world.GameData.Units[1];
        var splitSubId = parent.SubUnitIds[^1];
        var splitSoldiers = world.GameData.SubUnits[splitSubId].Soldier;
        var beforeCount = world.GameData.Units.Count;

        var result = host.OrderUnitSplit(parent.Id, [splitSubId], new Point2(9, 8), "分遣队");
        Assert.True(result.IsSuccess);
        Assert.Equal(beforeCount + 1, world.GameData.Units.Count);

        var newUnit = world.GameData.Units.Values.Single(u =>
            u.Id != parent.Id && u.ForceId == parent.ForceId && u.Location.IsSameTile(new Point3(9, 8)));
        Assert.Equal(splitSoldiers, newUnit.Soldier);
        Assert.DoesNotContain(splitSubId, parent.SubUnitIds);
        Assert.True(parent.Soldier >= UnitSplitRules.MinSoldiersPerSide);
    }

    [Fact]
    public void Host_LordResidence_CanDeployFromGarrison()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        var stronghold = world.GameData.Strongholds[1];
        var garrisonBefore = stronghold.ForceActor.Soldier;
        var unitsBefore = world.GameData.Units.Count;

        var result = host.DeployFromStronghold(
            1,
            "清洲出征队",
            4,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Ashigaru,
                    TypeName = "足轻",
                    Soldiers = 500
                }
            ]);
        Assert.True(result.IsSuccess);
        Assert.Equal(unitsBefore + 1, world.GameData.Units.Count);

        var deployed = world.GameData.Units.Values.Single(u =>
            u.ForceId == 1 && u.Location.IsSameTile(stronghold.Location) && u.Name == "清洲出征队");
        Assert.Equal(500, deployed.Soldier);
        Assert.Equal(garrisonBefore - 500, stronghold.ForceActor.Soldier);
        Assert.Equal(4, deployed.LeaderId);
    }

    [Fact]
    public void Host_Deploy_RejectsNonLordResidence()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var result = host.DeployFromStronghold(
            3,
            "冈崎队",
            1,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Ashigaru,
                    Soldiers = 200
                }
            ]);
        Assert.False(result.IsSuccess);
    }

    private static Unit CloneAdjacentFriendlyUnit(GameWorld world, Unit template, Point3 location)
    {
        var id = world.GameData.Units.Keys.Max() + 1;
        var unit = new Unit
        {
            Id = id,
            Name = $"{template.Name}二队",
            ForceId = template.ForceId,
            Location = location,
            Soldier = 1200,
            Food = 1000,
            Ap = template.Ap,
            Movement = template.Movement,
            Morale = template.Morale,
            Training = template.Training,
            IsMilitary = true,
            Status = UnitStatus.Waiting,
            Stance = UnitStance.Normal,
            Directive = UnitDirective.Move,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget { RoutePoints = new Queue<Point2>() }
        };

        var subId = world.GameData.SubUnits.Keys.DefaultIfEmpty(0).Max() + 1;
        var sub = new SubUnit
        {
            Id = subId,
            TypeId = StrategyTroopTypes.Ashigaru,
            TypeName = "足轻",
            ForceId = template.ForceId,
            UnitId = id,
            Soldier = unit.Soldier
        };
        world.GameData.SubUnits[subId] = sub;
        unit.SubUnitIds.Add(subId);
        world.GameData.Units[id] = unit;
        MapLocationActions.RegisterUnit(world, unit);
        return unit;
    }

    private static GameWorld GetWorld(StrategySimulationHost host)
    {
        var field = typeof(StrategySimulationHost).GetField(
            "simulation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scope = field!.GetValue(host)!;
        return (GameWorld)scope.GetType().GetProperty("World")!.GetValue(scope)!;
    }
}
