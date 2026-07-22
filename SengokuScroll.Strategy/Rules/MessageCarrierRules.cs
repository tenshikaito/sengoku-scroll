using SengokuScroll.Common.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 文书载体制度相关的业务规则（判定类，不直接修改实体）。
/// </summary>
public static class MessageCarrierRules
{
    /// <summary>
    /// 判断下达方与目标是否不在同一格，从而需要派遣在途载体。
    /// 同格（含同在据点内）则返回 <c>false</c>。
    /// </summary>
    public static bool RequiresInTransitDelivery(Point3 issuerLocation, Point3 targetLocation)
        => issuerLocation != targetLocation;
}
