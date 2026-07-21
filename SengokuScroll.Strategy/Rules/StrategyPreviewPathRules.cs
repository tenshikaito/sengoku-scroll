using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Rules;

/// <summary>战争迷雾下路径预览：玩家不可见的战场格不作为寻路阻挡。</summary>
public static class StrategyPreviewPathRules
{
    public static Func<Point2, bool>? BuildFogAwarePathBlockCheck(
        MovementRules movementRules,
        Unit unit,
        GameData gameData,
        StrategyScenarioMeta meta,
        ForceVisibilityState? visibility)
    {
        if (visibility is null || meta.StartOptions.FogMode == Models.StrategyFogMode.None)
            return null;

        return location =>
        {
            if (!movementRules.IsPathTileBlockedByMilitary(unit, location))
                return false;

            if (StrategyFogDtoRules.IsMapEntityVisible(location.X, location.Y, meta, visibility))
                return true;

            var hasBattlefield = gameData.Battlefields.Values.Any(b =>
                !b.IsClosed
                && b.Location.X == location.X
                && b.Location.Y == location.Y);

            // 业务：迷雾外看不见的战场不应让预览路线无故绕路
            return !hasBattlefield;
        };
    }
}
