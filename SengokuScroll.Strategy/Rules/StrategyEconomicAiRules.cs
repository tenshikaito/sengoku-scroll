using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Rules;

/// <summary>AI 据点的月度税制选择：先保粮与民心，再补足财政缓冲。</summary>
public static class StrategyEconomicAiRules
{
    public sealed record Decision(
        string Policy,
        string Reason,
        PendingStrongholdTaxChange Change);

    public static Decision? Evaluate(Stronghold stronghold, Force force, GameData gameData)
    {
        var dailyFood = Math.Max(
            1,
            LogisticsCalculator.CalculateCivilianDailyFoodConsumption(stronghold.Population));
        var foodReserveDays = stronghold.CivilianActor.Food / dailyFood;
        var feelings = stronghold.CivilianActor.PopularFeelings;

        if (feelings < 45 || foodReserveDays < 10)
        {
            return BuildDecision(
                stronghold,
                "Relief",
                $"民心={feelings} 粮储={foodReserveDays}日",
                pollTarget: 8,
                agricultureTarget: 20,
                commerceTarget: 10,
                tariffTarget: 6,
                maxStep: 3,
                onlyDecrease: true);
        }

        var reserveTarget = CalculateTreasuryReserve(force.Id, gameData);
        if (force.Money < reserveTarget && feelings >= 65 && foodReserveDays >= 30)
        {
            return BuildDecision(
                stronghold,
                "Revenue",
                $"府库={force.Money} 目标={reserveTarget}",
                pollTarget: 14,
                agricultureTarget: 30,
                commerceTarget: 18,
                tariffTarget: 12,
                maxStep: 1,
                onlyIncrease: true);
        }

        return BuildDecision(
            stronghold,
            "Balanced",
            $"民心={feelings} 粮储={foodReserveDays}日",
            pollTarget: 12,
            agricultureTarget: 26,
            commerceTarget: 14,
            tariffTarget: 9,
            maxStep: 1);
    }

    private static int CalculateTreasuryReserve(int forceId, GameData gameData)
    {
        var strongholdMaintenance = gameData.Strongholds.Values
            .Where(x => x.ForceId == forceId)
            .Sum(EconomyCalculator.CalculateStrongholdMonthlyMaintenanceMoney);
        var unitMaintenance = gameData.Units.Values
            .Where(x => x.ForceId == forceId)
            .Sum(EconomyCalculator.CalculateUnitMonthlyMaintenanceMoney);
        return Math.Max(20_000, (strongholdMaintenance + unitMaintenance) * 3);
    }

    private static Decision? BuildDecision(
        Stronghold stronghold,
        string policy,
        string reason,
        int pollTarget,
        int agricultureTarget,
        int commerceTarget,
        int tariffTarget,
        int maxStep,
        bool onlyDecrease = false,
        bool onlyIncrease = false)
    {
        var poll = Step(stronghold.PollTaxRate, pollTarget, maxStep, onlyDecrease, onlyIncrease);
        var agriculture = Step(stronghold.AgricultureTaxRate, agricultureTarget, maxStep, onlyDecrease, onlyIncrease);
        var commerce = Step(stronghold.CommerceTaxRate, commerceTarget, maxStep, onlyDecrease, onlyIncrease);
        var tariff = Step(stronghold.TariffTaxRate, tariffTarget, maxStep, onlyDecrease, onlyIncrease);

        if (poll == stronghold.PollTaxRate
            && agriculture == stronghold.AgricultureTaxRate
            && commerce == stronghold.CommerceTaxRate
            && tariff == stronghold.TariffTaxRate)
        {
            return null;
        }

        return new Decision(policy, reason, new PendingStrongholdTaxChange
        {
            PollTaxRate = poll,
            AgricultureTaxRate = agriculture,
            CommerceTaxRate = commerce,
            TariffTaxRate = tariff
        });
    }

    private static byte Step(
        byte current,
        int target,
        int maxStep,
        bool onlyDecrease,
        bool onlyIncrease)
    {
        if ((onlyDecrease && current <= target) || (onlyIncrease && current >= target))
            return current;

        var delta = Math.Clamp(target - current, -maxStep, maxStep);
        return (byte)Math.Clamp(current + delta, 0, 100);
    }
}
