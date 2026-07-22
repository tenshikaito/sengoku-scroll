using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>为商业值足够的据点初始化商户店铺。</summary>
public static class MerchantBootstrapHelper
{
    public const int MinCommerceForShop = 20;

    public static void EnsureMerchantShops(GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (!MarketRules.CanTrade(stronghold))
                continue;

            if (stronghold.CommerceValue < MinCommerceForShop)
                continue;

            if (stronghold.MerchantActors.Count > 0)
                continue;

            var merchantId = stronghold.Id * 1000 + 7;
            stronghold.MerchantActors.Add(new StrongholdActor
            {
                Id = merchantId,
                Name = $"{stronghold.Name}商会",
                ForceId = stronghold.ForceId,
                StrongholdId = stronghold.Id,
                Type = ActorType.Merchant,
                Food = MarketConstants.MerchantFoodReserveGo * 2,
                Money = 20_000,
                LuxuryGoods = 100,
                CharacterIds = [],
                SubUnitIds = []
            });
        }
    }
}
