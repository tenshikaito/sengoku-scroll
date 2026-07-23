using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>为商业值足够的据点初始化屋号商户（xx屋）。</summary>
public static class MerchantBootstrapHelper
{
    public const int MinCommerceForShop = 20;

    private static readonly string[] PrimaryHouseNames = ["三井屋", "今井屋", "津田屋", "住友屋", "鸿池屋"];

    public static void EnsureMerchantShops(GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values.OrderBy(s => s.Id))
        {
            if (!MarketRules.CanTrade(stronghold))
                continue;

            if (stronghold.CommerceValue < MinCommerceForShop)
                continue;

            if (HasNonNanbanMerchant(stronghold))
                continue;

            var houseName = ResolvePrimaryHouseName(stronghold.Id);
            EnsureMerchantShop(gameData, stronghold, houseName, suffix: 7);
        }
    }

    private static bool HasNonNanbanMerchant(Stronghold stronghold)
        => stronghold.MerchantActors.Any(m => !OrganizationForceHelper.IsNanbanMerchantName(m.Name));

    private static string ResolvePrimaryHouseName(int strongholdId)
        => PrimaryHouseNames[Math.Max(0, strongholdId - 1) % PrimaryHouseNames.Length];

    internal static string ResolveDefaultLeaderName(string houseName)
        => houseName switch
        {
            "三井屋" => "三井高利",
            "今井屋" => "今井宗久",
            "津田屋" => "津田算长",
            "住友屋" => "住友吉次",
            "鸿池屋" => "鸿池新七",
            _ => string.Empty,
        };

    private static bool OrganizationHasShop(GameData gameData, int organizationForceId)
        => OrganizationForceHelper.EnumerateShops(gameData, organizationForceId).Any();

    private static int ResolveShopMoney(Stronghold stronghold, string houseName)
    {
        if (OrganizationForceHelper.IsNanbanMerchantName(houseName))
            return 35_000;

        return 18_000 + Math.Max(0, stronghold.Id) * 2_500;
    }

    public static void EnsureMerchantShop(
        GameData gameData,
        Stronghold stronghold,
        string houseName,
        int suffix,
        int? leaderCharacterId = null,
        string? leaderName = null)
    {
        if (stronghold.MerchantActors.Any(m =>
                string.Equals(m.Name, houseName, StringComparison.Ordinal)
                && m.ForceId == OrganizationForceHelper.ResolveForceId(houseName)))
        {
            return;
        }

        var organization = OrganizationForceHelper.GetOrCreate(
            gameData,
            houseName,
            ForceCategory.Merchant);

        var isBranchShop = OrganizationHasShop(gameData, organization.Id);
        var leaderId = leaderCharacterId ?? 90_000 + stronghold.Id * 100 + suffix;
        OrganizationForceHelper.EnsureShopCharacter(
            gameData,
            stronghold,
            leaderId,
            organization.Id,
            houseName,
            preferFamousLeader: !isBranchShop && string.IsNullOrWhiteSpace(leaderName),
            explicitName: leaderName);

        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = stronghold.Id * 1000 + suffix,
            Name = houseName,
            ForceId = organization.Id,
            StrongholdId = stronghold.Id,
            Type = ActorType.Merchant,
            Food = MarketConstants.MerchantFoodReserveGo * 2,
            Money = ResolveShopMoney(stronghold, houseName),
            LuxuryGoods = 100,
            CommerceProduction = Math.Max(500, stronghold.CommerceValue / 40),
            CharacterIds = [leaderId],
            SubUnitIds = []
        });
    }
}
