using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class AgricultureLaborAndHarvestTests
{
    [Fact]
    public void LaborCapacity_ScalesWithPopulation()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        stronghold.Population = 10_000;

        var capacity = AgricultureLaborRules.CalculateLaborCapacity(stronghold);

        Assert.Equal(5_000, capacity);
    }

    [Fact]
    public void MilitiaAway_ReducesAvailableLabor()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        stronghold.Population = 2_000;
        stronghold.ForceActor.Soldier = 800;
        world.GameData.Strongholds[1] = stronghold;

        var awayUnit = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Point3(3, 0));
        awayUnit.Soldier = 400;
        awayUnit.ActionTarget.StrongholdId = 1;
        var sub = new SubUnit
        {
            Id = 1,
            TypeId = StrategyTroopTypes.Ashigaru,
            TypeName = "足轻",
            ForceId = 1,
            StrongholdId = 1,
            UnitId = 2,
            Soldier = 400
        };
        awayUnit.SubUnitIds.Add(sub.Id);
        world.GameData.Units[2] = awayUnit;
        world.GameData.SubUnits[1] = sub;

        var away = AgricultureLaborRules.CountMilitiaAway(stronghold, world.GameData);
        var available = AgricultureLaborRules.CalculateLaborAvailable(stronghold, world.GameData);

        Assert.Equal(400, away);
        Assert.Equal(600, available);
    }

    [Fact]
    public void Harvest_GrossScalesWithCycleProgress()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        stronghold.CivilianActor.AgricultureProduction = 10_000;
        var harvestEvent = new HarvestEventDefinition(9, 1, 10_000);

        var full = AgricultureCalculator.CalculateGrossHarvestGo(
            stronghold,
            harvestEvent,
            AgricultureConstants.ProgressBasisPoints);
        var half = AgricultureCalculator.CalculateGrossHarvestGo(
            stronghold,
            harvestEvent,
            AgricultureConstants.ProgressBasisPoints / 2);

        Assert.Equal(10_000, full);
        Assert.Equal(5_000, half);
    }

    [Fact]
    public void GarrisonBootstrap_SplitsProfessionalTroopsFromMilitiaPool()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        stronghold.Population = 8_000;
        stronghold.ForceActor.Soldier = 2_000;
        world.GameData.Strongholds[1] = stronghold;

        StrongholdMilitaryBootstrapHelper.InitializeGarrisonComposition(stronghold, world.GameData);

        var pools = StrongholdMilitaryBootstrapHelper.ListGarrisonTroopPools(stronghold, world.GameData)
            .ToDictionary(p => p.TypeId, p => p.Soldiers);

        Assert.True(stronghold.ForceActor.Soldier < 2_000);
        Assert.True(pools.GetValueOrDefault(StrategyTroopTypes.Ashigaru) > 0);
        Assert.True(
            pools.GetValueOrDefault(StrategyTroopTypes.Cavalry)
            + pools.GetValueOrDefault(StrategyTroopTypes.Archer)
            + pools.GetValueOrDefault(StrategyTroopTypes.Matchlock) > 0);
    }
}
