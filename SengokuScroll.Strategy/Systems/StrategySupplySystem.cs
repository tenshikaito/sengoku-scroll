using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式后勤系统接口。</summary>
public interface IStrategySupplySystem : IGameSystem
{
}

/// <summary>
/// 后勤系统：自动派遣运输 Unit、每日推进在途队列、卸粮后返程、抵达据点后移除以便再次派遣。
/// 玩家可查看运输队指令菜单；改道须经信使（后续 API）。
/// </summary>
public class StrategySupplySystem(
    IGameContext context,
    SupplyConvoyDispatchHelper dispatchHelper,
    TariffTaxLedger tariffTaxLedger,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer) : IStrategySupplySystem
{
    /// <summary>在经济系统之后、军事单位系统之前执行。</summary>
    public int Order { get; } = 15;

    /// <inheritdoc />
    public void Update()
    {
        dispatchHelper.DispatchMonthlyLordTributes();
        dispatchHelper.DispatchTradeConvoys();
        dispatchHelper.DispatchNeededConvoys();

        var worldContext = context.GameWorldContext;
        var gameData = worldContext.GameWorld.GameData;
        var gameDate = gameData.GameDate;

        foreach (var transport in TransportUnitRules.EnumerateActiveTransportUnits(gameData).ToList())
        {
            if (transport.Status != UnitStatus.Moving && !TransportUnitRules.IsDeceivedTransport(transport))
                continue;

            if (TransportUnitRules.HasArrived(transport))
                continue;

            TransportUnitActions.ApplyDailyTransitConsumption(transport);

            if (transport.TransportPurpose != TransportPurpose.Migrant
                && transport.Food <= 0
                && transport.Money <= 0
                && transport.CargoPopulation <= 0
                && !transport.IsReturningToOrigin)
            {
                TransportUnitActions.DestroyTransport(worldContext, transport);
                continue;
            }

            if (transport.IsDeceived && transport.DeceivedHoldDaysRemaining > 0)
            {
                transport.DeceivedHoldDaysRemaining--;
                continue;
            }

            var threat = TransportRules.EvaluateThreatLevel(transport, gameData);
            if (TransportInterceptActions.ApplySoftIntercept(worldContext, transport, gameDate, threat, gameData))
                continue;

            if (transport.Ap <= 0)
                continue;

            if (!transport.IsReturningToOrigin
                && gameData.Strongholds.TryGetValue(transport.TransportOriginStrongholdId, out var origin)
                && GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData)
                && transport.Location.IsSameTile(origin.Location))
                continue;

            transport.Ap--;
            TransportUnitActions.AdvanceOneStep(worldContext, transport);

            if (transport.TransportPurpose == TransportPurpose.Trade)
            {
                var location = new Point2(transport.Location.X, transport.Location.Y);
                var stronghold = worldContext.GetStrongholdOrDefault(location);
                if (stronghold is not null)
                    TariffEconomyActions.TryAssessTransitTariff(
                        transport, stronghold, tariffTaxLedger, dayOutcomeBuffer, scenarioMeta);
            }
        }

        dispatchHelper.CompleteArrivedConvoys();
    }
}
