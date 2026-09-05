using System.Text.Json;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public sealed class AuditRegressionTests
{
    [Fact]
    public void OrganizationHashCollision_DoesNotMergeDifferentNames()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        var seen = new Dictionary<int, string>();
        for (var i = 0; i <= 8900; i++)
        {
            var name = $"organization-{i}";
            var id = OrganizationForceHelper.ResolveForceId(name);
            if (seen.TryGetValue(id, out var other))
            {
                var first = OrganizationForceHelper.GetOrCreate(gameData, other, ForceCategory.Merchant);
                var second = OrganizationForceHelper.GetOrCreate(gameData, name, ForceCategory.Merchant);
                Assert.NotEqual(first.Id, second.Id);
                Assert.Equal(name, second.Name);
                Assert.Same(second, OrganizationForceHelper.GetOrCreate(gameData, name, ForceCategory.Merchant));
                return;
            }
            seen.Add(id, name);
        }
        Assert.Fail("Expected a collision within 8901 names in an 8900-ID bucket.");
    }

    [Theory]
    [InlineData("../mini_kanto")]
    [InlineData("..\\mini_kanto")]
    [InlineData("C:\\Maps\\mini_kanto.json")]
    public void ScenarioLoad_RejectsPathsAndKeepsCurrentWorld(string path)
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
        Assert.False(host.LoadScenario(path).IsSuccess);
        Assert.True(host.GetState().IsSuccess);
        Assert.Equal("mini_kanto", host.LoadedScenarioId);
    }

    [Fact]
    public void MalformedRestore_DoesNotReplaceCurrentWorld()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);
        Assert.True(host.AdvanceDay().IsSuccess);
        var before = StrategySimulationHost.SerializeSave(host.CaptureSave().Value!);
        var invalid = new StrategySaveDocument
        {
            ScenarioId = "mini_kanto",
            PlayerForceId = int.MaxValue,
            Date = new() { Year = 1560, Month = 1, Day = 1 },
            Forces = [],
            Strongholds = [],
            Units = [],
            RuntimeState = JsonDocument.Parse("{\"Forces\":null}").RootElement.Clone()
        };
        Assert.False(host.RestoreSave(invalid).IsSuccess);
        Assert.Equal(before, StrategySimulationHost.SerializeSave(host.CaptureSave().Value!));
    }

    [Fact]
    public void NestedParallelRegions_CompleteInStableOrderAndRecoverAfterException()
    {
        var input = Enumerable.Range(0, 128).ToArray();
        var result = StrategyParallelWork.MapOrdered(input,
            outer => StrategyParallelWork.MapOrdered(input, inner => outer + inner, 1).Sum(), 1);
        Assert.Equal(input.Select(outer => input.Sum(inner => outer + inner)), result);
        Assert.ThrowsAny<Exception>(() => StrategyParallelWork.ForEachIndex(128,
            _ => throw new InvalidOperationException("test"), 1));
        Assert.Equal(input, StrategyParallelWork.MapOrdered(input, value => value, 1));
    }
}
