using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>玩家微操范围行为。</summary>
public interface IControlModeBehavior
{
    StrategyControlMode Mode { get; }

    bool AllowsDirectUnitControl(
        Unit unit,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId);
}
