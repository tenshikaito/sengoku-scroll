using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Rules;

/// <summary>经济设施判定（Market 设施绑定；M4-d）。</summary>
public static class EconomyFacilityRules
{
    /// <summary>据点是否已建成指定类型的经济设施。</summary>
    public static bool HasFacility(Stronghold stronghold, int facilityTypeId)
        => stronghold.EconomyFacilityIds.Contains(facilityTypeId);

    /// <summary>据点是否拥有市场设施（可开展贸易撮合）。</summary>
    public static bool HasMarket(Stronghold stronghold)
        => HasFacility(stronghold, EconomyFacilityConstants.MarketFacilityTypeId);

    /// <summary>剧本未配置时按商业值推断默认设施。</summary>
    public static IReadOnlyList<int> ResolveDefaultFacilityIds(Stronghold stronghold)
    {
        var ids = new List<int>();

        // 业务：商业值达到每铺位门槛时，视为拥有市场
        if (stronghold.CommerceValue >= EconomyConstants.CommerceValuePerShopSlot)
            ids.Add(EconomyFacilityConstants.MarketFacilityTypeId);

        return ids;
    }

    /// <summary>将设施类型 Id 解析为玩家可读名称。</summary>
    public static string ResolveFacilityName(int typeId)
        => typeId switch
        {
            EconomyFacilityConstants.MarketFacilityTypeId => "市场",
            EconomyFacilityConstants.SpecialtyWorkshopTypeId => "特产工坊",
            _ => $"设施#{typeId}"
        };
}
