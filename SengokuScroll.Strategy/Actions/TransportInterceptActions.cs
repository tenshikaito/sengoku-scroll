using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输队软拦截结果（M4-b/d）。</summary>
public static class TransportInterceptActions
{
    /// <summary>执行拦截后果；返回 true 表示当日不再移动。</summary>
    public static bool ApplySoftIntercept(
        SupplyConvoy convoy,
        GameDate date,
        int threatLevel,
        GameData gameData)
    {
        if (!TransportRules.ShouldIntercept(convoy, date, threatLevel))
            return false;

        // 业务：同格敌军威胁则全灭运输队并缴获/记欠账；否则软拦截扣货并迷惑一日
        if (threatLevel >= TransportConstants.SameTileEnemyThreat)
        {
            var plunderForceId = TransportRules.FindPrimaryThreatUnitId(convoy, gameData) is int unitId
                                 && gameData.Units.TryGetValue(unitId, out var unit)
                ? unit.ForceId
                : 0;

            if (plunderForceId > 0)
                PlunderEconomyActions.AwardConvoyCargoToForce(convoy, plunderForceId, gameData);

            TributeArrearsActions.AccrueUndeliveredConvoy(convoy, gameData);

            convoy.CargoFoodGo = 0;
            convoy.CargoMoney = 0;
            convoy.Status = SupplyConvoyStatus.Destroyed;
            return true;
        }

        if (convoy.CargoMoney > 0)
        {
            var toll = EconomyCalculator.ApplyBasisPointsTax(
                convoy.CargoMoney,
                TransportConstants.TollCargoMoneyBp);
            convoy.CargoMoney = Math.Max(0, convoy.CargoMoney - toll);
        }
        else if (convoy.CargoFoodGo > 0)
        {
            var loss = EconomyCalculator.ApplyBasisPointsTax(
                convoy.CargoFoodGo,
                TransportConstants.SkirmishCargoFoodBp);
            convoy.CargoFoodGo = Math.Max(0, convoy.CargoFoodGo - loss);
        }

        convoy.DeceivedHoldDaysRemaining = 1;
        convoy.Status = SupplyConvoyStatus.Deceived;
        return true;
    }
}
