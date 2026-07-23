using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

public class GarrisonDeployPoolTests
{
    [Fact]
    public void ValidateDeploy_RejectsCompositionAbovePool()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var stronghold = loaded.World.GameData.Strongholds[1];
        var pools = StrongholdMilitaryBootstrapHelper.ListGarrisonTroopPools(stronghold, loaded.World.GameData);
        var ashigaruPool = pools.First(p => p.TypeId == StrategyTroopTypes.Ashigaru).Soldiers;

        var result = StrongholdDeployRules.ValidateDeploy(
            stronghold,
            loaded.Meta,
            loaded.World.GameData,
            loaded.Meta.PlayerForceId,
            commanderId: 4,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Ashigaru,
                    Soldiers = ashigaruPool + 1
                }
            ]);

        Assert.False(result.IsSuccess);
        Assert.Equal(GameError.StrongholdError.InsufficientGarrisonTroops, result.Error);
    }

    [Fact]
    public void Host_Deploy_CanUseProfessionalGarrisonPool()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        var stronghold = world.GameData.Strongholds[1];
        var cavalryPool = StrongholdMilitaryBootstrapHelper
            .ListGarrisonTroopPools(stronghold, world.GameData)
            .FirstOrDefault(p => p.TypeId == StrategyTroopTypes.Cavalry);

        if (cavalryPool is null || cavalryPool.Soldiers <= 0)
            return;

        var take = Math.Min(100, cavalryPool.Soldiers);
        var result = host.DeployFromStronghold(
            1,
            "清洲骑兵队",
            4,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Cavalry,
                    TypeName = "骑兵",
                    Soldiers = take
                }
            ]);

        Assert.True(result.IsSuccess);

        var deployed = world.GameData.Units.Values.Single(u => u.Name == "清洲骑兵队");
        Assert.Equal(take, deployed.Soldier);
        Assert.Contains(deployed.SubUnitIds, id =>
            world.GameData.SubUnits.TryGetValue(id, out var sub)
            && sub.TypeId == StrategyTroopTypes.Cavalry
            && sub.Soldier == take);
    }

    [Fact]
    public void DeployFromStronghold_ReducesLaborAvailableWhenMilitiaLeaves()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        var stronghold = world.GameData.Strongholds[1];
        var laborBefore = AgricultureLaborRules.CalculateLaborAvailable(stronghold, world.GameData);

        var deploy = host.DeployFromStronghold(
            1,
            "测试队",
            commanderId: 4,
            [
                new StrategyDeployCompositionEntry
                {
                    TypeId = StrategyTroopTypes.Ashigaru,
                    Soldiers = 500
                }
            ]);

        Assert.True(deploy.IsSuccess);
        var laborAfter = AgricultureLaborRules.CalculateLaborAvailable(stronghold, world.GameData);
        Assert.Equal(laborBefore - 500, laborAfter);
    }

    [Fact]
    public void ApplyMonthlyMaintenance_DeductsProfessionalGarrisonMaintenance()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;
        var stronghold = world.GameData.Strongholds[1];
        var force = world.GameData.Forces[stronghold.ForceId];

        var professionalMaintenance = EconomyCalculator.CalculateGarrisonProfessionalMaintenanceMoney(
            stronghold,
            world.GameData);
        Assert.True(professionalMaintenance > 0);

        var moneyBefore = stronghold.ForceActor.Money;
        ForceEconomyActions.ApplyMonthlyMaintenance(force, world.GameData);

        var baseMaintenance = EconomyCalculator.CalculateStrongholdMonthlyMaintenanceMoney(stronghold);
        Assert.Equal(
            moneyBefore - baseMaintenance - professionalMaintenance,
            stronghold.ForceActor.Money);
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
