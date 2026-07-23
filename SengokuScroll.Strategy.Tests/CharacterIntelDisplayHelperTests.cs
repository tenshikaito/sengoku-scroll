using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class CharacterIntelDisplayHelperTests
{
    private static (StrategyLoadedScenario Loaded, StrategyForceLordRegistry Registry) LoadBootstrapped()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var registry = new StrategyForceLordRegistry();
        StrongholdCityActorBootstrapHelper.EnsureCityActors(loaded.World.GameData, registry);
        return (loaded, registry);
    }

    [Fact]
    public void Bootstrap_NoNanbanMerchantDuplicateNames()
    {
        var (loaded, _) = LoadBootstrapped();
        var nanbanForceId = OrganizationForceHelper.KnownIds.Nanban;
        var nanbanNames = loaded.World.GameData.Characters.Values
            .Where(c => !c.IsDead && c.ForceId == nanbanForceId)
            .Select(c => c.Name)
            .ToList();

        Assert.DoesNotContain("南蛮商人", nanbanNames);
        Assert.Contains("柏来图", nanbanNames);
    }

    [Fact]
    public void Bootstrap_PruneRemovesUnreferencedOrganizationCharacters()
    {
        var (loaded, _) = LoadBootstrapped();
        var nanbanForceId = OrganizationForceHelper.KnownIds.Nanban;
        var referenced = OrganizationForceHelper.CollectReferencedCharacterIds(
            loaded.World.GameData,
            nanbanForceId);

        foreach (var character in loaded.World.GameData.Characters.Values
                     .Where(c => !c.IsDead && c.ForceId == nanbanForceId))
        {
            Assert.Contains(character.Id, referenced);
        }
    }

    [Fact]
    public void ResolveHomeStrongholdName_FallsBackToForceLordResidence()
    {
        var (loaded, _) = LoadBootstrapped();
        var shibata = loaded.World.GameData.Characters[2];

        Assert.Equal("清洲", CharacterIntelDisplayHelper.ResolveHomeStrongholdName(
            shibata,
            loaded.World.GameData,
            loaded.Meta));
    }

    [Fact]
    public void ToDto_CharacterStrongholdName_RemainsWhenStrongholdFoggedOut()
    {
        var (loaded, _) = LoadBootstrapped();
        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(loaded.World, loaded.Meta);

        var dto = StrategyWorldStateMapper.ToDto(loaded.World, "mini_kanto", loaded.Meta, ledger);
        var shibata = dto.Characters.First(c => c.Id == 2);
        var tokugawa = dto.Characters.First(c => c.Id == 9);

        Assert.Equal("清洲", shibata.StrongholdName);
        Assert.Equal("滨松", tokugawa.StrongholdName);
    }
}
