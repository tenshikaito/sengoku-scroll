using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点城内势力：商户、南蛮商、寺社等 Actor 初始化。</summary>
public static class StrongholdCityActorBootstrapHelper
{
    private const int MinPopulationForTemple = 40_000;
    private const int MinCommerceForNanban = 80_000;

    public static void EnsureCityActors(GameData gameData, StrategyForceLordRegistry registry)
    {
        MerchantBootstrapHelper.EnsureMerchantShops(gameData);
        MarketBootstrapHelper.EnsureDemoMarketData(gameData);

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            EnsureReligionActor(gameData, stronghold);
            EnsureNanbanMerchant(gameData, stronghold);
        }

        CityActorDemoRosterHelper.EnsureDemoRoster(gameData);
        OrganizationForceHelper.SyncOrganizationLordRegistries(gameData, registry);
    }

    private static void EnsureReligionActor(GameData gameData, Stronghold stronghold)
    {
        if (stronghold.Population < MinPopulationForTemple)
            return;

        if (stronghold.ReligionActors.Count > 0)
            return;

        var templeName = ResolveTempleName(stronghold);
        var organization = OrganizationForceHelper.GetOrCreate(
            gameData,
            templeName,
            ForceCategory.Religion);

        var leaderId = 90_000 + stronghold.Id * 100 + 8;
        PaperDollCharacterHelper.EnsurePaperDollCharacter(
            gameData,
            stronghold,
            leaderId,
            organization.Id,
            templeName,
            religionId: ResolveTempleReligionId(templeName));

        stronghold.ReligionActors.Add(new StrongholdActor
        {
            Id = stronghold.Id * 1000 + 8,
            Name = templeName,
            ForceId = organization.Id,
            StrongholdId = stronghold.Id,
            Type = ActorType.Regligion,
            Money = 12_000,
            Food = MarketConstants.MerchantFoodReserveGo,
            CommerceProduction = 0,
            AgricultureProduction = Math.Max(120_000, stronghold.Population * 3),
            CharacterIds = [leaderId],
            SubUnitIds = []
        });
    }

    private static int ResolveTempleReligionId(string templeName)
    {
        if (templeName.Contains('神', StringComparison.Ordinal)
            || templeName.Contains('社', StringComparison.Ordinal)
            || templeName.Contains('宫', StringComparison.Ordinal))
        {
            return 1;
        }

        if (templeName.Contains('寺', StringComparison.Ordinal))
            return 4;

        return 1;
    }

    private static void EnsureNanbanMerchant(GameData gameData, Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        if (stronghold.CommerceValue < MinCommerceForNanban)
            return;

        if (stronghold.MerchantActors.Any(m => OrganizationForceHelper.IsNanbanMerchantName(m.Name)))
            return;

        const string houseName = "南蛮商会";
        var organization = OrganizationForceHelper.GetOrCreate(
            gameData,
            houseName,
            ForceCategory.Merchant);

        var hasExistingShop = OrganizationForceHelper.EnumerateShops(gameData, organization.Id).Any();
        var leaderId = stronghold.Id == 1 ? 90_030 : 90_000 + stronghold.Id * 100 + 9;
        var useNamedLeader = stronghold.Id == 1 && !hasExistingShop;
        OrganizationForceHelper.EnsureShopCharacter(
            gameData,
            stronghold,
            leaderId,
            organization.Id,
            houseName,
            preferFamousLeader: false,
            explicitName: useNamedLeader ? "柏来图" : null,
            religionId: useNamedLeader ? 6 : null);

        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = stronghold.Id * 1000 + 9,
            Name = houseName,
            ForceId = organization.Id,
            StrongholdId = stronghold.Id,
            Type = ActorType.Merchant,
            Food = MarketConstants.MerchantFoodReserveGo,
            Money = 35_000,
            CommerceProduction = Math.Max(500, stronghold.CommerceValue / 40),
            CharacterIds = [leaderId],
            SubUnitIds = []
        });
    }

    private static string ResolveTempleName(Stronghold stronghold)
        => stronghold.Name switch
        {
            "清洲" => "热田神宫",
            "小田原" => "早云寺",
            "冈崎" => "八幡宫",
            "骏府" => "久能山浅间神社",
            _ => $"{stronghold.Name}寺"
        };
}
