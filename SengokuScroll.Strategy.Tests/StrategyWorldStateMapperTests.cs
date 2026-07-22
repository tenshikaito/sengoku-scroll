using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary><see cref="StrategyWorldStateMapper"/> 单元测试（M2-a）。</summary>
public class StrategyWorldStateMapperTests
{
    [Fact]
    public void ToDto_MiniKanto_IncludesMapForcesStrongholdsAndUnits()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta);

        Assert.Equal(20, dto.Map.Width);
        Assert.Equal(20, dto.Map.Height);
        Assert.Equal(1560, dto.Date.Year);
        Assert.Equal(6, dto.Forces.Count);
        Assert.Equal(12, dto.Strongholds.Count);
        Assert.Equal(3, dto.Units.Count);
        Assert.Equal(3, dto.Diplomacies.Count);
        Assert.Contains(dto.Diplomacies, d => d.TargetForceId == 2 && d.Relation == "Enemy");
        Assert.Contains(dto.Diplomacies, d => d.TargetForceId == 4 && d.Relation == "Enemy");
        Assert.All(dto.Units, u => Assert.True(u.Soldiers > 0));

        var odaUnit = dto.Units.First(u => u.Id == 1);
        Assert.Equal("柴田胜家", odaUnit.CommanderName);
        Assert.Equal(2, odaUnit.CommanderId);
        Assert.Equal(82, odaUnit.Morale);

        var ozu = dto.Strongholds.First(s => s.Id == 1);
        Assert.Equal(0, ozu.LordId);
        Assert.True(ozu.IsDirectRule);
        Assert.Equal("织田信长", ozu.LordName);
        Assert.True(ozu.IsLordResidence);
        Assert.Equal("林秀贞", ozu.MayorName);

        var inuyama = dto.Strongholds.First(s => s.Id == 2);
        Assert.Equal(6, inuyama.LordId);
        Assert.False(inuyama.IsDirectRule);
        Assert.True(inuyama.IsLordResidence);
        Assert.Equal("酒井忠次", inuyama.LordName);
        Assert.Equal(3, inuyama.ForceId);

        var okazaki = dto.Strongholds.First(s => s.Id == 3);
        Assert.Equal(0, okazaki.LordId);
        Assert.False(okazaki.IsLordResidence);
        Assert.Equal("织田信长", okazaki.LordName);

        var odawara = dto.Strongholds.First(s => s.Id == 4);
        Assert.Equal(5, odawara.LordId);
        Assert.True(odawara.IsLordResidence);
        Assert.Equal("北条氏康", odawara.LordName);

        var kakegawa = dto.Strongholds.First(s => s.Id == 6);
        Assert.Equal(0, kakegawa.LordId);
        Assert.True(kakegawa.IsLordResidence);

        var imagawa = dto.Forces.First(f => f.Id == 2);
        Assert.Equal(6, imagawa.LordResidenceStrongholdId);

        Assert.Equal(11, dto.Characters.Count);

        var oda = dto.Forces.First(f => f.Id == 1);
        Assert.Equal(5, oda.StrongholdCount);
        Assert.Equal(6, oda.CharacterCount);
        Assert.Equal(1, oda.LordResidenceStrongholdId);
        Assert.Equal("清洲", dto.Lord.ResidenceStrongholdName);

        Assert.Equal(2, loaded.World.GameData.Units[1].LeaderId);
        Assert.Equal(4, loaded.World.GameData.Units[1].SubUnitIds.Count);
        Assert.Equal(3000, loaded.World.GameData.Units[1].Soldier);
        Assert.Equal(6, loaded.World.GameData.Strongholds[2].LordId);
        Assert.Equal(2, loaded.World.GameData.Characters[6].StrongholdId);

        var odaComposition = odaUnit.Composition;
        Assert.Equal(4, odaComposition.Count);
        Assert.Equal("足轻", odaComposition[0].TypeName);
        Assert.Equal(1890, odaComposition[0].Soldiers);
        Assert.Equal(63, odaComposition[0].RatioPercent);

        Assert.Equal(20, dto.Map.Width);
        Assert.Equal(20, dto.Map.Height);
        Assert.Equal("迷你关东试玩", dto.Map.Name);
    }

    [Fact]
    public void ToDto_ClearedMayor_DoesNotFallBackToScenarioOverlay()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        loaded.World.GameData.Strongholds[1].LeaderId = 0;

        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta);
        var kiyosu = dto.Strongholds.First(s => s.Id == 1);

        Assert.Null(kiyosu.MayorName);
    }
}
