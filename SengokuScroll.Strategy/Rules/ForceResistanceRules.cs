using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Rules;

/// <summary>势力是否仍有城或部队愿意继续抵抗。</summary>
public static class ForceResistanceRules
{
    /// <summary>势力是否仍有据点或尚有士气、兵力的军事单位可继续作战。</summary>
    public static bool HasActiveResistance(int forceId, GameData gameData)
    {
        // 业务：仍控制至少一座据点，视为势力尚未覆灭
        if (gameData.Strongholds.Values.Any(s => s.ForceId == forceId))
            return true;

        // 业务：无城时，尚有士气&gt;0 的军事单位则仍可继续抵抗
        return gameData.Units.Values.Any(u =>
            u.ForceId == forceId
            && u.IsMilitary
            && u.Soldier > 0
            && u.Morale > 0);
    }
}
