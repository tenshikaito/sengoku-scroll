using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Rules;

/// <summary>玩家对部队下达直接操作前的控制模式校验。</summary>
public static class PlayerUnitControlRules
{
    public static GameResult ValidateDirectUnitCommand(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (unit.ForceId != meta.PlayerForceId)
            return GameError.DiplomacyError.NotSelfForce;

        var profile = GameStartOptionsProfile.Create(meta.StartOptions, meta.Difficulty);
        if (!profile.Control.AllowsDirectUnitControl(unit, meta, gameData, meta.PlayerForceId))
            return GameError.UnitError.UnitNotDirectlyControllable;

        return GameResult.Ok();
    }
}
