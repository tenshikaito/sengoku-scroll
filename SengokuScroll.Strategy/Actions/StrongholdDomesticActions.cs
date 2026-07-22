using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点内政指令（税率等）。</summary>
public static class StrongholdDomesticActions
{
    public const byte MaxTaxRate = 100;

    /// <summary>调整税率并即时影响民心。</summary>
    public static bool TrySetTaxRates(
        Stronghold stronghold,
        byte? pollTaxRate,
        byte? agricultureTaxRate,
        byte? commerceTaxRate,
        byte? tariffTaxRate,
        out string? error)
    {
        var change = new PendingStrongholdTaxChange
        {
            PollTaxRate = pollTaxRate,
            AgricultureTaxRate = agricultureTaxRate,
            CommerceTaxRate = commerceTaxRate,
            TariffTaxRate = tariffTaxRate
        };

        if (!TryValidateTaxRates(change, out error))
            return false;

        return ApplyTaxRateChange(stronghold, change);
    }

    /// <summary>校验税率变更请求（不修改实体）。</summary>
    public static bool TryValidateTaxRates(PendingStrongholdTaxChange change, out string? error)
    {
        error = null;

        if (change.PollTaxRate is byte poll && poll > MaxTaxRate)
        {
            error = "PollTaxRateOutOfRange";
            return false;
        }

        if (change.AgricultureTaxRate is byte agri && agri > MaxTaxRate)
        {
            error = "AgricultureTaxRateOutOfRange";
            return false;
        }

        if (change.CommerceTaxRate is byte commerce && commerce > MaxTaxRate)
        {
            error = "CommerceTaxRateOutOfRange";
            return false;
        }

        if (change.TariffTaxRate is byte tariff && tariff > MaxTaxRate)
        {
            error = "TariffTaxRateOutOfRange";
            return false;
        }

        if (change.PollTaxRate is null
            && change.AgricultureTaxRate is null
            && change.CommerceTaxRate is null
            && change.TariffTaxRate is null)
        {
            error = "NoTaxRateChange";
            return false;
        }

        return true;
    }

    /// <summary>写入税率并调整民心。</summary>
    public static bool ApplyTaxRateChange(Stronghold stronghold, PendingStrongholdTaxChange change)
    {
        var oldBurden = CalculateTaxBurdenScore(stronghold);

        if (change.PollTaxRate is byte newPoll)
            stronghold.PollTaxRate = newPoll;
        if (change.AgricultureTaxRate is byte newAgri)
            stronghold.AgricultureTaxRate = newAgri;
        if (change.CommerceTaxRate is byte newCommerce)
            stronghold.CommerceTaxRate = newCommerce;
        if (change.TariffTaxRate is byte newTariff)
            stronghold.TariffTaxRate = newTariff;

        ApplyPopularFeelingsFromTaxChange(stronghold, oldBurden, CalculateTaxBurdenScore(stronghold));
        return true;
    }

    /// <summary>税负综合分（人头+商业为主；农税/关税权重较低）。</summary>
    public static int CalculateTaxBurdenScore(Stronghold stronghold)
        => stronghold.PollTaxRate * 2
           + stronghold.CommerceTaxRate * 2
           + stronghold.AgricultureTaxRate
           + stronghold.TariffTaxRate;

    public static void ApplyPopularFeelingsFromTaxChange(
        Stronghold stronghold,
        int oldBurdenScore,
        int newBurdenScore)
    {
        if (newBurdenScore == oldBurdenScore)
            return;

        var feelings = stronghold.CivilianActor.PopularFeelings;
        var delta = newBurdenScore - oldBurdenScore;

        if (delta > 0)
        {
            var penalty = Math.Min(
                MarketConstants.PopularFeelingsTaxIncreaseMaxPenalty,
                delta * MarketConstants.PopularFeelingsTaxIncreasePenaltyPerPoint);
            feelings = (byte)Math.Max(0, feelings - penalty);
        }
        else
        {
            var bonus = Math.Min(
                MarketConstants.PopularFeelingsTaxDecreaseMaxBonus,
                -delta * MarketConstants.PopularFeelingsTaxDecreaseBonusPerPoint);
            feelings = (byte)Math.Min(100, feelings + bonus);
        }

        stronghold.CivilianActor.PopularFeelings = feelings;
    }
}
