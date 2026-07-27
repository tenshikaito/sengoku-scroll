using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>贸易运输 Unit 过境关税（M4-c）。</summary>
public static class TariffEconomyActions
{
    /// <summary>
    /// 贸易队进入据点后尝试计征关税：从载货扣款并记入台账。
    /// </summary>
    /// <returns>本次实际扣款（文）。</returns>
    public static int TryAssessTransitTariff(
        Unit transport,
        Stronghold stronghold,
        TariffTaxLedger ledger,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer = null,
        StrategyScenarioMeta? meta = null)
    {
        if (transport.TransportPurpose != TransportPurpose.Trade)
            return 0;

        if (transport.IsReturningToOrigin)
            return 0;

        if (stronghold.Id == transport.TransportOriginStrongholdId)
            return 0;

        if (transport.ForceId == stronghold.ForceId)
            return 0;

        if (ledger.HasChargedTransit(transport.Id, stronghold.Id))
            return 0;

        if (!MarketRules.CanTrade(stronghold))
            return 0;

        var cargoValue = EconomyCalculator.CalculateConvoyCargoMoneyValue(
            transport.Money,
            transport.Food,
            ResolveFoodPriceMoneyPerGo(stronghold));

        var tariffDue = EconomyCalculator.CalculateConvoyTariffMoney(stronghold, cargoValue);
        ledger.MarkTransitCharged(transport.Id, stronghold.Id);

        if (tariffDue <= 0)
            return 0;

        var paid = Math.Min(tariffDue, transport.Money);
        transport.Money -= paid;
        ledger.Accrue(stronghold.Id, paid);

        if (paid > 0
            && dayOutcomeBuffer is not null
            && meta is not null
            && transport.ForceId == meta.PlayerForceId)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "TariffTransitCharged",
                Brief = $"🛃 {stronghold.Name} 过境关税 {paid:N0} 文",
                Message =
                    $"贸易队「{transport.Name}」途经 {stronghold.Name}，缴纳过境关税 {paid:N0} 文（载货估值 {cargoValue:N0} 文）。"
            });
        }

        return paid;
    }

    private static int ResolveFoodPriceMoneyPerGo(Stronghold stronghold)
        => stronghold.Market.LastClosePriceMoneyPerGo > 0
            ? stronghold.Market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;
}
