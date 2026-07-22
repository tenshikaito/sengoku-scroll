using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class StrategyUnitAIRulesTests
{
    [Fact]
    public void ToDto_MiniKanto_StrongholdsHaveStabilityDefenseAndFacilities()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta);

        Assert.All(dto.Strongholds, s =>
        {
            Assert.True(s.Stability > 0, $"{s.Name} 治安应为非零");
            Assert.True(s.PopularFeelings > 0, $"{s.Name} 民心应为非零");
            Assert.True(s.Defense > 0, $"{s.Name} 城防应为设施累加值");
            Assert.NotEmpty(s.DefenseFacilities);
            Assert.Equal(s.Defense, s.DefenseFacilities.Sum(f => f.Defense));
        });
    }

    [Fact]
    public void EvaluateDirective_AllForcesAiControlled_PromotesPlayerMoveToOccupy()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = loaded.Meta.PlayerForceId,
            AllForcesAiControlled = true
        };
        var playerUnit = loaded.World.GameData.Units.Values.First(u => u.ForceId == meta.PlayerForceId);
        playerUnit.Directive = UnitDirective.Move;

        var decision = StrategyUnitAIRules.EvaluateDirective(
            playerUnit,
            loaded.World.GameData,
            meta.PlayerForceId,
            loaded.World.GameMapMasterData,
            meta);

        Assert.Equal(UnitDirective.Occupy, playerUnit.Directive);
        Assert.True(decision.Changed);
        Assert.Equal("PromoteOccupy", decision.Code);
    }

    [Fact]
    public void EvaluateDirective_LowMoraleSameTileEnemy_SwitchesToRetreat()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var enemy = loaded.World.GameData.Units.Values.First(u => u.ForceId != loaded.Meta.PlayerForceId);
        enemy.Morale = 20;
        enemy.Directive = UnitDirective.Occupy;

        var playerUnit = loaded.World.GameData.Units.Values.First(u => u.ForceId == loaded.Meta.PlayerForceId);
        playerUnit.Location = enemy.Location;

        StrategyUnitAIRules.EvaluateDirective(enemy, loaded.World.GameData, loaded.Meta.PlayerForceId);

        Assert.Equal(UnitDirective.Retreat, enemy.Directive);
    }

    [Fact]
    public void EvaluateDirective_OutnumberedSameTile_SwitchesToRetreat()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var enemy = loaded.World.GameData.Units.Values.First(u => u.ForceId != loaded.Meta.PlayerForceId);
        enemy.Morale = 80;
        enemy.Soldier = 500;
        enemy.Directive = UnitDirective.Occupy;

        var playerUnit = loaded.World.GameData.Units.Values.First(u => u.ForceId == loaded.Meta.PlayerForceId);
        playerUnit.Soldier = 3000;
        playerUnit.Location = enemy.Location;

        StrategyUnitAIRules.EvaluateDirective(enemy, loaded.World.GameData, loaded.Meta.PlayerForceId);

        Assert.Equal(UnitDirective.Retreat, enemy.Directive);
    }

    [Fact]
    public void TryExecuteDailyAction_RetreatDirective_DoesNotQueueAttack()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var enemy = loaded.World.GameData.Units.Values.First(u => u.ForceId != loaded.Meta.PlayerForceId);
        enemy.Directive = UnitDirective.Retreat;
        enemy.Status = UnitStatus.Waiting;
        enemy.Stance = UnitStance.Normal;
        enemy.ActionTarget.UnitId = 0;
        enemy.ActionTarget.RoutePoints.Clear();

        var playerUnit = loaded.World.GameData.Units.Values.First(u => u.ForceId == loaded.Meta.PlayerForceId);
        playerUnit.Location = new Point3(enemy.Location.X + 1, enemy.Location.Y);

        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();
        var hostiles = StrategyUnitAIRules.ResolveHostileUnits(enemy, loaded.World.GameData);
        var hostileStrongholds = StrategyUnitAIRules.ResolveHostileStrongholds(enemy, loaded.World.GameData);

        StrategyUnitAIRules.TryExecuteDailyAction(
            enemy,
            loaded.World.GameData,
            pathfinding,
            hostiles,
            hostileStrongholds);

        Assert.Equal(UnitDirective.Retreat, enemy.Directive);
        Assert.NotEqual(UnitStance.Attacking, enemy.Stance);
        Assert.Equal(0, enemy.ActionTarget.UnitId);
    }

    [Fact]
    public void ExecuteDailyAction_ReturnsThoughtChainSteps()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();

        var unit = loaded.World.GameData.Units[2];
        unit.Directive = UnitDirective.Occupy;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();

        var hostiles = StrategyUnitAIRules.ResolveHostileUnits(unit, loaded.World.GameData);
        var decision = StrategyUnitAIRules.ExecuteDailyAction(
            unit,
            loaded.World.GameData,
            pathfinding,
            hostiles,
            StrategyUnitAIRules.ResolveHostileStrongholds(unit, loaded.World.GameData),
            mapMaster: loaded.World.GameMapMasterData);

        Assert.NotEmpty(decision.Steps);
        Assert.False(string.IsNullOrWhiteSpace(decision.Code));
        Assert.False(string.IsNullOrWhiteSpace(decision.Message));
        Assert.Contains(decision.Steps, s => s.Contains("方针"));
    }

    [Fact]
    public void EvaluateDirective_ReturnsThoughtWhenRetreating()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var enemy = loaded.World.GameData.Units.Values.First(u => u.ForceId != loaded.Meta.PlayerForceId);
        enemy.Morale = 20;
        enemy.Directive = UnitDirective.Occupy;

        var playerUnit = loaded.World.GameData.Units.Values.First(u => u.ForceId == loaded.Meta.PlayerForceId);
        playerUnit.Location = enemy.Location;

        var decision = StrategyUnitAIRules.EvaluateDirective(enemy, loaded.World.GameData, loaded.Meta.PlayerForceId);

        Assert.True(decision.Changed);
        Assert.Equal("Retreat", decision.ToDirective);
        Assert.NotEmpty(decision.Steps);
        Assert.Equal(UnitDirective.Retreat, enemy.Directive);
    }

    [Fact]
    public void TryExecuteDailyAction_Occupy_PrefersHigherThreatSameTileEnemy()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();

        var attacker = loaded.World.GameData.Units[2];
        attacker.Directive = UnitDirective.Occupy;
        attacker.Status = UnitStatus.Waiting;
        attacker.Stance = UnitStance.Normal;
        attacker.Soldier = 2500;
        attacker.ActionTarget.UnitId = 0;
        attacker.ActionTarget.RoutePoints.Clear();
        attacker.Location = new Point3(4, 4);

        var weak = StrategyTestWorldBuilder.CreateTestUnit(20, 1, new Point3(4, 4));
        weak.Soldier = 400;
        weak.Morale = 80;
        var strong = StrategyTestWorldBuilder.CreateTestUnit(21, 1, new Point3(4, 4));
        strong.Soldier = 2200;
        strong.Morale = 80;
        strong.Stance = UnitStance.Attacking;
        strong.ActionTarget.UnitId = attacker.Id;
        strong.Directive = UnitDirective.Occupy;
        loaded.World.GameData.Units[20] = weak;
        loaded.World.GameData.Units[21] = strong;
        loaded.World.GameData.Units[1].Location = new Point3(0, 0);
        Domain.Actions.MapLocationActions.RegisterUnit(loaded.World, weak);
        Domain.Actions.MapLocationActions.RegisterUnit(loaded.World, strong);

        var hostiles = StrategyUnitAIRules.ResolveHostileUnits(attacker, loaded.World.GameData);
        var decision = StrategyUnitAIRules.ExecuteDailyAction(
            attacker,
            loaded.World.GameData,
            pathfinding,
            hostiles,
            [],
            mapMaster: loaded.World.GameMapMasterData);

        Assert.True(decision.IsSuccess);
        Assert.True(decision.Code is "EngageAdjacent" or "EngageInCity");
        Assert.Equal(UnitStance.Attacking, attacker.Stance);
        Assert.True(attacker.ActionTarget.UnitId is 20 or 21);
        Assert.Contains(decision.Steps, s => s.Contains("候选敌军"));
    }
}
