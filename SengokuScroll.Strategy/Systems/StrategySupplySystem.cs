using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Helpers;

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
    SupplyConvoyDispatchHelper dispatchHelper) : IStrategySupplySystem
{
    /// <summary>在经济系统之后、军事单位系统之前执行。</summary>
    public int Order { get; } = 15;

    /// <inheritdoc />
    public void Update()
    {
        dispatchHelper.DispatchMonthlyLordTributes();
        dispatchHelper.DispatchNeededConvoys();

        var convoys = context.GameWorldContext.GameWorld.GameData.SupplyConvoys;

        foreach (var convoy in convoys.Values.ToList())
        {
            if (convoy.Status is not (SupplyConvoyStatus.Moving or SupplyConvoyStatus.Deceived))
                continue;

            SupplyConvoyActions.ApplyDailyTransitConsumption(convoy);

            // 返程空载不扣粮尽；Outbound 载粮/载钱均为 0 则视为溃散
            if (convoy.CargoFoodGo <= 0 && convoy.CargoMoney <= 0 && !convoy.IsReturningToOrigin)
            {
                convoy.Status = SupplyConvoyStatus.Destroyed;
                continue;
            }

            if (convoy.IsDeceived && convoy.DeceivedHoldDaysRemaining > 0)
            {
                convoy.DeceivedHoldDaysRemaining--;
                continue;
            }

            if (convoy.Ap <= 0)
                continue;

            convoy.Ap--;
            SupplyConvoyActions.AdvanceOneStep(convoy);
        }

        dispatchHelper.CompleteArrivedConvoys();
    }
}
