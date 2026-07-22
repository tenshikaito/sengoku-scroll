using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.GameStart;

internal sealed class FullDirectControlModeBehavior : IControlModeBehavior
{
    public static readonly FullDirectControlModeBehavior Instance = new();

    public StrategyControlMode Mode => StrategyControlMode.FullDirect;

    public bool AllowsDirectUnitControl(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId)
        => unit.ForceId == playerForceId;
}

internal sealed class DirectiveOnlyControlModeBehavior : IControlModeBehavior
{
    public static readonly DirectiveOnlyControlModeBehavior Instance = new();

    public StrategyControlMode Mode => StrategyControlMode.DirectiveOnly;

    public bool AllowsDirectUnitControl(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId)
    {
        if (unit.ForceId != playerForceId)
            return false;

        if (meta.LordUnitId == unit.Id)
            return true;

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, meta);
        return lordLocation.X == unit.Location.X && lordLocation.Y == unit.Location.Y;
    }
}

public static class ControlModeBehaviorFactory
{
    public static IControlModeBehavior Create(StrategyControlMode mode)
        => mode switch
        {
            StrategyControlMode.FullDirect => FullDirectControlModeBehavior.Instance,
            StrategyControlMode.DirectiveOnly => DirectiveOnlyControlModeBehavior.Instance,
            _ => DirectiveOnlyControlModeBehavior.Instance
        };
}
