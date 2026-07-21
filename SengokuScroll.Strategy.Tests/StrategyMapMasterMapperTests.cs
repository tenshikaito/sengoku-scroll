using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary><see cref="StrategyWorldStateMapper.ToMapMasterDto"/> 单元测试。</summary>
public class StrategyMapMasterMapperTests
{
    [Fact]
    public void ToMapMasterDto_MiniKanto_IncludesTerrainRegionRoadsAndLandmarks()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var dto = StrategyWorldStateMapper.ToMapMasterDto(loaded.World, "mini_kanto");

        Assert.Equal("mini_kanto", dto.ScenarioId);
        Assert.Equal(20, dto.Width);
        Assert.Equal(20, dto.Height);
        Assert.Equal(400, dto.TerrainIds.Count);
        Assert.Equal(400, dto.RegionIds.Count);
        Assert.Contains(dto.Terrains, t => t.Key == "forest" && t.Name == "森林");
        Assert.Contains(dto.Terrains, t => t.Key == "water" && t.Name == "水域");
        Assert.Contains(dto.Terrains, t => t.Key == "mountain" && t.Name == "山地");
        Assert.True(dto.TerrainIds.Any(id => id == dto.Terrains.First(t => t.Key == "forest").Id));
        Assert.Equal(1, dto.RegionIds[0]);
        Assert.Equal(2, dto.RegionIds[10]);
        Assert.True(dto.RoadCells.Count >= 12);
        Assert.Equal(3, dto.Landmarks.Count);
        Assert.Contains(dto.Landmarks, lm => lm.Name == "热田神宫" && lm.X == 6 && lm.Y == 12);
    }
}
