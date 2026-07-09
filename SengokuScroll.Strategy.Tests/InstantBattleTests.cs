using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>瞬间战计算器：战力、胜率边界、确定性种子。</summary>
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
}

/// <summary>StrategySimulationHost 瞬间战联调。</summary>
public class StrategyInstantBattleHostTests
{
    [Fact]
    public void Host_AdjacentEnemies_CanPreviewAndExecuteBattle()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var preview = host.PreviewUnitAttack(1, new Point2(5, 4));
        Assert.True(preview.IsSuccess);
        Assert.Equal(2, preview.Value!.DefenderUnitId);
        Assert.InRange(preview.Value.AttackerWinRatePercent, 5, 95);

        var beforeAtt = GetUnit(host, 1);
        var beforeDef = GetUnit(host, 2);
        var attSoldiersBefore = beforeAtt.Soldier;
        var defSoldiersBefore = beforeDef.Soldier;

        Assert.True(host.OrderUnitAttack(1, new Point2(5, 4)).IsSuccess);
        var afterDay = host.AdvanceDay();
        Assert.True(afterDay.IsSuccess);
        Assert.Single(afterDay.Value!.ResolvedBattles);

        var att = GetUnit(host, 1);
        var def = GetUnit(host, 2);
        Assert.True(att.Soldier < attSoldiersBefore);
        Assert.True(def.Soldier < defSoldiersBefore);

        var messengers = GetWorld(host).GameData.Messengers.Values
            .Where(m => m.PayloadType == Domain.Entities.Types.MessengerPayloadType.BattleReport)
            .ToList();
        Assert.Equal(2, messengers.Count);

        var playerMessenger = Assert.Single(messengers, m => m.ForceId == 1);
        var enemyMessenger = Assert.Single(messengers, m => m.ForceId == 2);
        Assert.Equal(att.Location.X, playerMessenger.Location.X);
        Assert.Equal(att.Location.Y, playerMessenger.Location.Y);
        Assert.Equal(def.Location.X, enemyMessenger.Location.X);
        Assert.Equal(def.Location.Y, enemyMessenger.Location.Y);
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
        world.GameMapData.Units.Remove(tileMap.GetIndex(unit.Location));
        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }
}
