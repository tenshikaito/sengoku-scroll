using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点商店创立（无需势力许可；商业值与每组织每城 1 店限制）。</summary>
public static class StrongholdShopActions
{
    public static GameResult CreateShop(
        GameData gameData,
        Stronghold stronghold,
        int playerForceId,
        string? houseName = null)
    {
        if (stronghold.ForceId != playerForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (!MarketRules.CanTrade(stronghold))
            return GameError.StrongholdError.StrongholdNotFound;

        if (stronghold.CommerceValue < MerchantBootstrapHelper.MinCommerceForShop)
            return GameError.DataNotFound;

        var resolvedName = string.IsNullOrWhiteSpace(houseName)
            ? MerchantBootstrapHelper.ResolvePrimaryHouseName(stronghold.Id)
            : houseName.Trim();

        var organizationId = OrganizationForceHelper.ResolveForceId(resolvedName);
        if (stronghold.MerchantActors.Any(m => m.ForceId == organizationId))
            return GameError.DataNotFound;

        MerchantBootstrapHelper.EnsureMerchantShop(
            gameData,
            stronghold,
            resolvedName,
            suffix: 70 + stronghold.MerchantActors.Count,
            leaderName: MerchantBootstrapHelper.ResolveDefaultLeaderName(resolvedName));

        return GameResult.Ok();
    }
}
