using SengokuScroll.Common.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 信使制度相关的业务规则（判定类，不直接修改实体）。
/// </summary>
public static class MessengerRules
{
    /// <summary>
    /// 判断下达方与目标是否不在同一格，从而需要派遣信使。
    /// 同格（含同在据点内）则返回 <c>false</c>。
    /// </summary>
    /// <param name="issuerLocation">指令下达方所在格。</param>
    /// <param name="targetLocation">目标单位/据点所在格。</param>
    public static bool RequiresMessenger(Point3 issuerLocation, Point3 targetLocation)
        => issuerLocation != targetLocation;
}
