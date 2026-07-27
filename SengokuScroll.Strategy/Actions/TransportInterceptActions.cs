using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输 Unit 软拦截结果（M4-b/d）。</summary>
public static class TransportInterceptActions
{
    /// <summary>执行拦截后果；返回 true 表示当日不再移动。</summary>
    public static bool ApplySoftIntercept(
        IGameWorldContext context,
        Unit transport,
        GameDate date,
        int threatLevel,
        GameData gameData)
    {
        if (!TransportRules.ShouldIntercept(transport, date, threatLevel))
            return false;

        if (threatLevel >= TransportConstants.SameTileEnemyThreat)
        {
            var plunderForceId = TransportRules.FindPrimaryThreatUnitId(transport, gameData) is int unitId
                                 && gameData.Units.TryGetValue(unitId, out var unit)
                ? unit.ForceId
                : 0;

            if (plunderForceId > 0)
                PlunderEconomyActions.AwardConvoyCargoToForce(transport, plunderForceId, gameData);

            TributeArrearsActions.AccrueUndeliveredConvoy(transport, gameData);

            transport.Food = 0;
            transport.Money = 0;
            TransportUnitActions.DestroyTransport(context, transport);
            return true;
        }

        if (transport.Money > 0)
        {
            var toll = EconomyCalculator.ApplyBasisPointsTax(
                transport.Money,
                TransportConstants.TollCargoMoneyBp);
            transport.Money = Math.Max(0, transport.Money - toll);
        }
        else if (transport.Food > 0)
        {
            var loss = EconomyCalculator.ApplyBasisPointsTax(
                transport.Food,
                TransportConstants.SkirmishCargoFoodBp);
            transport.Food = Math.Max(0, transport.Food - loss);
        }

        TransportUnitActions.ApplyDeceivedHold(transport, holdDays: 1);
        return true;
    }
}
