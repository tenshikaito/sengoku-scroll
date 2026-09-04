using SengokuScroll.Domain;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class EspionageIntelRulesTests
{
    [Fact]
    public void ApplyUnitMask_WithShowAllyIntel_RevealsAlliedForceWithoutEspionage()
    {
        var gameData = CreateGameData();
        gameData.Forces[1].Diplomacies.Add(new Domain.Entities.Diplomacy
        {
            ForceId = 1,
            TargetForceId = 2,
            Relation = Domain.Entities.Diplomacy.DiplomacyRelation.Allied
        });
        gameData.Forces[2].Diplomacies.Add(new Domain.Entities.Diplomacy
        {
            ForceId = 2,
            TargetForceId = 1,
            Relation = Domain.Entities.Diplomacy.DiplomacyRelation.Allied
        });

        var dto = CreateForeignUnitDto();
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null) with
        {
            ShowAllyIntel = true
        };

        var masked = EspionageIntelRules.ApplyUnitMask(dto, playerForceId: 1, gameData, ledger: null, options);

        Assert.Null(masked.SoldiersDisplay);
        Assert.Equal(4200, masked.Soldiers);
        Assert.Equal(900, masked.Food);
    }

    [Fact]
    public void ApplyUnitMask_WithShowAllyIntel_StillMasksOuterVassal()
    {
        var gameData = CreateGameData();
        var outerVassal = StrategyTestWorldBuilder.CreateTestForce(3);
        outerVassal.Status = Domain.Entities.Force.ForceStatus.OuterVassal;
        outerVassal.SuzerainForceId = 2;
        gameData.Forces[3] = outerVassal;
        gameData.Forces[1].Diplomacies.Add(new Domain.Entities.Diplomacy
        {
            ForceId = 1,
            TargetForceId = 2,
            Relation = Domain.Entities.Diplomacy.DiplomacyRelation.Allied
        });

        var dto = CreateForeignUnitDto() with { ForceId = 3, Id = 3 };
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null) with
        {
            ShowAllyIntel = true
        };

        var masked = EspionageIntelRules.ApplyUnitMask(dto, playerForceId: 1, gameData, ledger: null, options);

        Assert.Equal("未知", masked.SoldiersDisplay);
    }

    private static StrategyUnitStateDto CreateForeignUnitDto()
        => new()
        {
            Id = 2,
            Name = "敌部队",
            ForceId = 2,
            X = 3,
            Y = 3,
            Soldiers = 4200,
            Food = 900,
            Money = 500,
            Ap = 5,
            Movement = 5,
            Status = "Waiting",
            Directive = "Move",
            Stance = "Normal",
            SiegeMode = "None",
            DirectiveTargetId = 0,
            Route = [],
            Morale = 72,
            Training = 65,
            CultureName = "日本",
            ReligionName = "神道教",
            Composition = [],
            SupplyStatus = "Sufficient",
            FoodDaysRemaining = 10,
            InTransitSupplies = [],
            MapVisible = true,
            UnitKind = "Military"
        };

    [Fact]
    public void ApplyUnitMask_WithoutEspionage_HidesForeignMilitaryIntel()
    {
        var dto = CreateForeignUnitDto();

        var masked = EspionageIntelRules.ApplyUnitMask(dto, playerForceId: 1, gameData: CreateGameData(), ledger: null);

        Assert.Equal("未知", masked.SoldiersDisplay);
        Assert.Equal("未知", masked.MoraleBand);
        Assert.Equal(0, masked.Food);
    }

    [Fact]
    public void ApplyUnitMask_WithFuzzyMilitaryEspionage_ShowsBands()
    {
        var ledger = new StrategyEspionageIntelLedger();
        ledger.RecordMission(
            observerForceId: 1,
            EspionageIntelTargetKind.Unit,
            targetId: 2,
            EspionageIntelScope.Military,
            EspionageIntelPrecision.Fuzzy,
            acquiredDate: new Domain.Types.GameDate(1560, 1, 1));

        var dto = CreateForeignUnitDto();

        var masked = EspionageIntelRules.ApplyUnitMask(dto, playerForceId: 1, gameData: CreateGameData(), ledger);

        Assert.Equal("中", masked.SoldiersDisplay);
        Assert.Equal("高", masked.MoraleBand);
        Assert.Equal(0, masked.Food);
    }

    [Fact]
    public void PruneExpired_RemovesIntelAfterTwoMonths()
    {
        var ledger = new StrategyEspionageIntelLedger();
        ledger.RecordMission(
            observerForceId: 1,
            EspionageIntelTargetKind.Stronghold,
            targetId: 9,
            EspionageIntelScope.Both,
            EspionageIntelPrecision.Exact,
            acquiredDate: new Domain.Types.GameDate(1560, 1, 1));

        ledger.PruneExpired(new Domain.Types.GameDate(1560, 2, 28));
        Assert.NotNull(ledger.TryGet(1, EspionageIntelTargetKind.Stronghold, 9));

        ledger.PruneExpired(new Domain.Types.GameDate(1560, 3, 1));
        Assert.Null(ledger.TryGet(1, EspionageIntelTargetKind.Stronghold, 9));
    }

    [Fact]
    public void RecordMission_IsIsolatedByObserverForce()
    {
        var ledger = new StrategyEspionageIntelLedger();
        ledger.RecordMission(
            observerForceId: 1,
            EspionageIntelTargetKind.Stronghold,
            targetId: 9,
            EspionageIntelScope.Military,
            EspionageIntelPrecision.Exact,
            acquiredDate: new Domain.Types.GameDate(1560, 1, 1));

        Assert.NotNull(ledger.TryGet(1, EspionageIntelTargetKind.Stronghold, 9));
        Assert.Null(ledger.TryGet(2, EspionageIntelTargetKind.Stronghold, 9));
    }

    private static GameData CreateGameData()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Forces[2] = StrategyTestWorldBuilder.CreateTestForce(2);
        return world.GameData;
    }
}
