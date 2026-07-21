using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class StrategyFogDtoRulesTests
{
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

        var dto = new StrategyStrongholdStateDto
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
            PopularFeelings = 65,
            IsLordResidence = false,
            LordId = 6,
            IsDirectRule = false,
            LordName = "酒井忠次",
            Morale = 78,
            Training = 62,
            CultureName = "日本",
            ReligionName = "神道教",
            Money = 9000000,
            GarrisonSoldiers = 1200,
            GarrisonWounded = 0,
            PollTaxRate = 10,
            AgricultureTaxRate = 25,
            CommerceTaxRate = 12,
            TariffTaxRate = 8,
            IsHistorical = true,
            Defense = 500,
            DefenseFacilities = [],
            EconomyFacilities = [],
            LuxuryGoods = 0
        };

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
}
