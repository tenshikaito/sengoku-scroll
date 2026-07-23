using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class StrategyFogDtoRulesTests
{
    private static StrategyStrongholdStateDto CreateTestStrongholdDto() => new()
    {
        Id = 2,
        Name = "犬山",
        TypeId = 3,
        TypeName = "山城",
        ForceId = 3,
        X = 4,
        Y = 4,
        Food = 72000000,
        Population = 36000,
        Stability = 70,
        AdministrativeEfficiency = 80,
        PopularFeelings = 65,
        IsLordResidence = false,
        LordId = 6,
        MayorId = 0,
        IsDirectRule = false,
        LordName = "酒井忠次",
        Morale = 78,
        Training = 62,
        CultureName = "日本",
        ReligionName = "神道教",
        Money = 9000000,
        GarrisonSoldiers = 1200,
        MilitiaSoldiers = 1200,
        TotalSoldiers = 2400,
        Technologies = [],
        LaborCapacity = 18000,
        LaborAvailable = 18000,
        MilitiaAway = 0,
        LaborRatioPercent = 100,
        EffectiveCropPattern = "Single",
        EarlyCropProgressPercent = 0,
        LateCropProgressPercent = 0,
        ThirdCropProgressPercent = 0,
        GarrisonTroopPools = [],
        StandingGarrisonUnits = [],
        CropCycles = [],
        AgricultureProductionPotential = 0,
        KnowsDoubleCrop = false,
        KnowsTripleCrop = false,
        CityActors = [],
        GarrisonWounded = 0,
        PollTaxRate = 10,
        AgricultureTaxRate = 25,
        CommerceTaxRate = 12,
        TariffTaxRate = 8,
        GovernancePriority = "Military",
        IsHistorical = true,
        Defense = 500,
        DefenseFacilities = [],
        EconomyFacilities = [],
        LuxuryGoods = 0,
        Scale = 12,
        Maintenance = 7200,
        ActiveEffects = [],
    };

    [Fact]
    public void ApplyStrongholdFog_OwnRealmInnerVassal_KeepsFullStats()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            LordName = "织田信长",
            StartOptions = options
        };

        var innerVassal = StrategyTestWorldBuilder.CreateTestForce(3);
        innerVassal.Status = Force.ForceStatus.InnerVassal;
        innerVassal.SuzerainForceId = 1;

        var gameData = new GameData
        {
            GameDate = new SengokuScroll.Domain.Types.GameDate(1, 1, 1),
            Forces = new Dictionary<int, Force>
            {
                [1] = StrategyTestWorldBuilder.CreateTestForce(1),
                [3] = innerVassal
            },
            Strongholds = [],
            Units = [],
            SubUnits = [],
            Characters = [],
            SupplyConvoys = [],
            MessageCarriers = []
        };

        var visibility = new ForceVisibilityState();
        visibility.EnsureCapacity(20, 20);

        var dto = CreateTestStrongholdDto();

        var result = StrategyFogDtoRules.ApplyStrongholdFog(dto, meta, gameData, visibility, 20);

        Assert.NotNull(result);
        Assert.Equal("Visible", result!.VisibilityTier);
        Assert.Equal(36000, result.Population);
        Assert.Equal(1200, result.GarrisonSoldiers);
        Assert.Equal(9000000, result.Money);
    }

    [Fact]
    public void ApplyStrongholdFog_ExploredButNotVisible_KeepsNameAndMasksStats()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Hard, null);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            LordName = "织田信长",
            StartOptions = options
        };

        var visibility = new ForceVisibilityState();
        visibility.EnsureCapacity(20, 20);
        visibility.MarkExplored(4, 4, 20);

        var dto = CreateTestStrongholdDto();

        var result = StrategyFogDtoRules.ApplyStrongholdFog(dto, meta, null!, visibility, 20);

        Assert.NotNull(result);
        Assert.Equal("Known", result!.VisibilityTier);
        Assert.Equal("犬山", result.Name);
        Assert.Equal(0, result.Population);
        Assert.Equal(0, result.GarrisonSoldiers);
    }

    [Fact]
    public void IsMapMobileEntityVisible_CharacterFog_RealmConvoyOutsideVision_IsHidden()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Hard, null) with
        {
            FogMode = StrategyFogMode.Character
        };
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            StartOptions = options
        };
        var visibility = new ForceVisibilityState();
        visibility.EnsureCapacity(20, 20);
        visibility.VisibleCells.Add((2, 8));

        Assert.False(StrategyFogDtoRules.IsMapMobileEntityVisible(
            9, 9, 1, meta, null!, visibility));
    }

    [Fact]
    public void IsMessageCarrierMapVisible_ForceFog_CharacterCarrierOutsideVision_IsHidden()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            StartOptions = options
        };
        var visibility = new ForceVisibilityState();
        visibility.EnsureCapacity(20, 20);
        visibility.VisibleCells.Add((2, 8));

        var carrier = new MessageCarrier
        {
            Id = 1,
            Name = "税令",
            ForceId = 1,
            CarrierKind = MessageCarrierKind.Character,
            Location = new Point3(9, 9),
            SourceStrongholdId = 1,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = new Queue<Point3>(),
            Payload = new MessagePayload { Type = MessagePayloadType.TaxRateChange }
        };

        Assert.False(StrategyFogDtoRules.IsMessageCarrierMapVisible(carrier, meta, visibility));
    }

    [Fact]
    public void AddRealmUnitEscortCarrierVision_ForceFog_ExpandsVisibleAroundCarrier()
    {
        var visible = new HashSet<(int X, int Y)>();
        var gameData = new GameData
        {
            GameDate = new SengokuScroll.Domain.Types.GameDate(1, 1, 1),
            Forces = new Dictionary<int, Force> { [1] = StrategyTestWorldBuilder.CreateTestForce(1) },
            Strongholds = [],
            Units = [],
            SubUnits = [],
            Characters = [],
            SupplyConvoys = [],
            MessageCarriers = new Dictionary<int, MessageCarrier>
            {
                [1] = new()
                {
                    Id = 1,
                    Name = "战报",
                    ForceId = 1,
                    CarrierKind = MessageCarrierKind.UnitEscort,
                    Location = new Point3(9, 9),
                    SourceStrongholdId = 1,
                    Status = MessageCarrierStatus.Moving,
                    RoutePoints = new Queue<Point3>(),
                    Payload = new MessagePayload { Type = MessagePayloadType.BattleReport }
                }
            }
        };

        StrategyVisionRules.AddRealmUnitEscortCarrierVision(
            visible,
            [1],
            playerForceId: 1,
            gameData,
            mapWidth: 20,
            mapHeight: 20);

        Assert.Contains((9, 9), visible);
        Assert.Contains((9, 10), visible);
    }
}
