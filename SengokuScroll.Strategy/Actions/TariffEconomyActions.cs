using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>贸易运输队过境关税（M4-c）。</summary>
public static class TariffEconomyActions
{
    /// <summary>
    /// 贸易队进入据点后尝试计征关税：从载货扣款并记入台账。
    /// </summary>
    /// <returns>本次实际扣款（文）。</returns>
    public static int TryAssessTransitTariff(
        SupplyConvoy convoy,
        Stronghold stronghold,
        TariffTaxLedger ledger)
    {
        // 业务：仅贸易去程、异势力据点、且未重复计征时征收过境关税
        if (convoy.Purpose != TransportPurpose.Trade)
            return 0;

        if (convoy.IsReturningToOrigin)
            return 0;

        if (stronghold.Id == convoy.OriginStrongholdId)
            return 0;

        if (convoy.ForceId == stronghold.ForceId)
            return 0;

        if (ledger.HasChargedTransit(convoy.Id, stronghold.Id))
            return 0;

        if (!MarketRules.CanTrade(stronghold))
            return 0;

        var cargoValue = EconomyCalculator.CalculateConvoyCargoMoneyValue(
            convoy.CargoMoney,
            convoy.CargoFoodGo,
            ResolveFoodPriceMoneyPerGo(stronghold));

        var tariffDue = EconomyCalculator.CalculateConvoyTariffMoney(stronghold, cargoValue);
        ledger.MarkTransitCharged(convoy.Id, stronghold.Id);

        if (tariffDue <= 0)
            return 0;

        // 业务：按目的地市价折算粮货价值后计征关税，不足则从载货现款扣
        var paid = Math.Min(tariffDue, convoy.CargoMoney);
        convoy.CargoMoney -= paid;
        ledger.Accrue(stronghold.Id, paid);
        return paid;
    }

    private static int ResolveFoodPriceMoneyPerGo(Stronghold stronghold)
        => stronghold.Market.LastClosePriceMoneyPerGo > 0
            ? stronghold.Market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;
}
