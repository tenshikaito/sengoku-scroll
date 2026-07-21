using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>?????????????????????/summary>
public class InstantBattleCalculatorTests
{
    [Fact]
    public void WinRate_ClampedBetween5And95()
    {
        var strong = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        strong.Soldier = 10_000;
        var weak = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        weak.Soldier = 10;

        var rate = InstantBattleCalculator.ComputeAttackerWinRatePercent(strong, weak);
        Assert.InRange(rate, 5, 95);
    }

    [Fact]
    public void Resolve_SameSeed_ProducesSameOutcome()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 100;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 80;

        var a = InstantBattleCalculator.Resolve(attacker, defender, 42);
        var b = InstantBattleCalculator.Resolve(attacker, defender, 42);

        Assert.Equal(a.AttackerWon, b.AttackerWon);
        Assert.Equal(a.AttackerCasualties, b.AttackerCasualties);
        Assert.Equal(a.DefenderCasualties, b.DefenderCasualties);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Resolve_SmallArmy_StillTakesCasualties(int soldiers)
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = soldiers;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = soldiers;

        var outcome = InstantBattleCalculator.Resolve(attacker, defender, 42);

        Assert.True(outcome.AttackerCasualties > 0);
        Assert.True(outcome.DefenderCasualties > 0);
    }
}

/// <summary>StrategySimulationHost ??????/summary>
public class StrategyInstantBattleHostTests
{
    [Fact]
    public void Host_AdjacentEnemies_CanPreviewAndExecuteBattle()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        IsolateEnemyUnitsForMarchTest(GetWorld(host));
        Teleport(GetWorld(host), 2, new Point3(9, 8));

        var preview = host.PreviewUnitAttack(1, new Point2(9, 8));
        Assert.True(preview.IsSuccess);
        Assert.Equal(2, preview.Value!.DefenderUnitId);
        Assert.InRange(preview.Value.AttackerWinRatePercent, 5, 95);

        var beforeAtt = GetUnit(host, 1);
        var beforeDef = GetUnit(host, 2);
        var attSoldiersBefore = beforeAtt.Soldier;
        var defSoldiersBefore = beforeDef.Soldier;

        Assert.True(host.OrderUnitAttack(1, new Point2(9, 8)).IsSuccess);

        var sawBattleReport = false;
        for (var day = 0; day < 14; day++)
        {
            var afterDay = host.AdvanceDay();
            Assert.True(afterDay.IsSuccess);
            if (afterDay.Value!.Events.Any(e =>
                    e.Category == "BattleReportArrived" && e.BattleResult is not null))
            {
                sawBattleReport = true;
                break;
            }
        }

        // ???????????????????????????????????        Assert.True(sawBattleReport, "??????????????????");
    }

    [Fact]
    public void Host_NonAdjacentEnemy_ReturnsAttackRangeError()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        Teleport(world, 2, new Point3(7, 4));

        var preview = host.PreviewUnitAttack(1, new Point2(7, 4));
        Assert.False(preview.IsSuccess);
        Assert.Equal(GameError.TargetLocationNotAdjacent.Code, preview.Error!.Code);
    }

    private static GameWorld GetWorld(StrategySimulationHost host)
    {
        var field = typeof(StrategySimulationHost).GetField("simulation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scope = field!.GetValue(host)!;
        return (GameWorld)scope.GetType().GetProperty("World")!.GetValue(scope)!;
    }

    private static Domain.Entities.Unit GetUnit(StrategySimulationHost host, int id)
    {
        return GetWorld(host).GameData.Units[id];
    }

    private static void Teleport(GameWorld world, int unitId, Point3 location)
    {
        var unit = world.GameData.Units[unitId];
        var tileMap = world.GameMapMasterData.TileMap;
        var __idx = tileMap.GetIndex(unit.Location); if (world.GameMapData.Units.TryGetValue(__idx, out var __list)) { __list.Remove(unit.Id); if (__list.Count==0) world.GameMapData.Units.Remove(__idx); }
        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }

    private static void IsolateEnemyUnitsForMarchTest(GameWorld world)
    {
        foreach (var unit in world.GameData.Units.Values.Where(u => u.ForceId != 1).ToList())
            Teleport(world, unit.Id, new Point3(9, 9));
    }
}
