using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Rules;

/// <summary>玩家对部队下达直接操作前的控制模式校验。</summary>
public static class PlayerUnitControlRules
{
    /// <summary>校验单位是否属于当前玩家势力；所有会修改单位状态的入口都应先调用。</summary>
    public static GameResult ValidateOwnership(Unit unit, StrategyScenarioMeta meta)
        => unit.ForceId == meta.PlayerForceId
            ? GameResult.Ok()
            : GameError.DiplomacyError.NotSelfForce;

    public static GameResult ValidateDirectUnitCommand(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        var ownership = ValidateOwnership(unit, meta);
        if (!ownership.IsSuccess)
            return ownership.Error!;

        var profile = GameStartOptionsProfile.Create(meta.StartOptions, meta.Difficulty);
        if (!profile.Control.AllowsDirectUnitControl(unit, meta, gameData, meta.PlayerForceId))
            return GameError.UnitError.UnitNotDirectlyControllable;

        return GameResult.Ok();
    }
}
