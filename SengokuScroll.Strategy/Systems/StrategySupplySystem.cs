using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式后勤系统接口。</summary>
public interface IStrategySupplySystem : IGameSystem
{
}

/// <summary>
/// 后勤系统：自动派遣运输队、每日推进在途队列、卸粮后返程、抵达据点后移除以便再次派遣。
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
        // 阶段1：自动派遣——月初贡纳/钱纳、贸易队、缺粮补给队
        dispatchHelper.DispatchMonthlyLordTributes();
        dispatchHelper.DispatchTradeConvoys();
        dispatchHelper.DispatchNeededConvoys();

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var gameDate = gameData.GameDate;
        var convoys = gameData.SupplyConvoys;

        // 阶段2：逐队在途推进——日耗、拦截、移动、过境关税
        foreach (var convoy in convoys.Values.ToList())
        {
            if (convoy.Status is not (SupplyConvoyStatus.Moving or SupplyConvoyStatus.Deceived))
                continue;

            SupplyConvoyActions.ApplyDailyTransitConsumption(convoy);

            // 返程空载不扣粮尽；Outbound 载粮/载钱均为 0 则视为溃散（移民队携带人口除外）
            if (convoy.Purpose != TransportPurpose.Migrant
                && convoy.CargoFoodGo <= 0
                && convoy.CargoMoney <= 0
                && !convoy.IsReturningToOrigin)
            {
                convoy.Status = SupplyConvoyStatus.Destroyed;
                continue;
            }

            // 业务：迷惑状态暂停移动，逐日递减迷惑剩余天数
            if (convoy.IsDeceived && convoy.DeceivedHoldDaysRemaining > 0)
            {
                convoy.DeceivedHoldDaysRemaining--;
                continue;
            }

            var threat = TransportRules.EvaluateThreatLevel(convoy, gameData);
            if (TransportInterceptActions.ApplySoftIntercept(convoy, gameDate, threat, gameData))
                continue;

            if (convoy.Ap <= 0)
                continue;

            if (!convoy.IsReturningToOrigin
                && gameData.Strongholds.TryGetValue(convoy.OriginStrongholdId, out var origin)
                && GarrisonBehaviorRules.IsStrongholdBlockaded(origin, gameData)
                && convoy.Location.IsSameTile(origin.Location))
                continue;

            convoy.Ap--;
            SupplyConvoyActions.AdvanceOneStep(convoy);

            if (convoy.Purpose == TransportPurpose.Trade)
            {
                var location = new Point2(convoy.Location.X, convoy.Location.Y);
                var stronghold = context.GameWorldContext.GetStrongholdOrDefault(location);
                if (stronghold is not null)
                    TariffEconomyActions.TryAssessTransitTariff(
                        convoy, stronghold, tariffTaxLedger, dayOutcomeBuffer, scenarioMeta);
            }
        }

        // 阶段3：处理抵达运输队——卸货、贸易交割、安排返程
        dispatchHelper.CompleteArrivedConvoys();
    }
}
