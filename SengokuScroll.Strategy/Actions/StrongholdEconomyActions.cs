using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点级经济单步变更（日产、市民口粮等）。</summary>
public static class StrongholdEconomyActions
{
    /// <summary>将农业/商业日产计入市民 Actor；skipFood 用于收粮日。</summary>
    public static (int FoodProduced, int MoneyProduced) ApplyDailyProduction(
        Stronghold stronghold,
        GameData gameData,
        bool skipFood = false)
    {
        var food = skipFood ? 0 : EconomyCalculator.CalculateDailyFoodProduction(stronghold);
        var money = EconomyCalculator.CalculateDailyMoneyProduction(stronghold);

        if (food > 0)
            stronghold.CivilianActor.Food += food;

        if (money > 0)
            stronghold.CivilianActor.Money += money;

        var luxury = MarketCalculator.CalculateDailyLuxuryProduction(stronghold);
        if (luxury > 0)
            stronghold.ForceActor.LuxuryGoods += luxury;

        if (food > 0 || money > 0 || luxury > 0)
        {
            if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
        }

        return (food, money);
    }

    /// <summary>每月 1 日：从市民/商户征收钱税入府库。</summary>
    public static (int PollTax, int CommerceTax, int TradeTax, int TariffTax) ApplyMonthlyMoneyTaxes(
        Stronghold stronghold,
        MerchantTaxLedger merchantTaxLedger,
        TariffTaxLedger tariffTaxLedger,
        Domain.GameData? gameData = null,
        Data.Models.StrategyScenarioMeta? meta = null)
    {
        var poll = EconomyCalculator.CalculateMonthlyPollTaxMoney(stronghold, gameData, meta);
        var commerce = EconomyCalculator.CalculateMonthlyCommerceBaseTaxMoney(stronghold, gameData, meta);
        var trade = merchantTaxLedger.CollectForStronghold(stronghold.Id);
        var tariff = tariffTaxLedger.CollectForStronghold(stronghold.Id);

        var pollPaid = TransferMoney(stronghold.CivilianActor, stronghold.ForceActor, poll);
        var commercePaid = TransferMoney(stronghold.CivilianActor, stronghold.ForceActor, commerce);

        stronghold.ForceActor.Money += trade;
        stronghold.ForceActor.Money += tariff;

        return (pollPaid, commercePaid, trade, tariff);
    }

    /// <summary>每月店铺维持费；不足则关店（自列表移除）。</summary>
    public static int ApplyMerchantShopMaintenance(Stronghold stronghold)
    {
        var closed = 0;

        foreach (var merchant in stronghold.MerchantActors.ToList())
        {
        // 业务：资金不足则强制关店并从据点商户列表移除
            if (merchant.Money >= MarketConstants.MerchantShopMonthlyMaintenance)
            {
                merchant.Money -= MarketConstants.MerchantShopMonthlyMaintenance;
                continue;
            }

            stronghold.MerchantActors.Remove(merchant);
            closed++;
        }

        return closed;
    }

    /// <summary>按人口扣除市民聚合口粮；不足时扣至 0。</summary>
    /// <returns>本日实际扣除的合数。</returns>
    public static int ApplyDailyCivilianFoodConsumption(Stronghold stronghold)
    {
        var consumption = LogisticsCalculator.CalculateCivilianDailyFoodConsumption(stronghold.Population);
        if (consumption <= 0)
            return 0;

        var deducted = Math.Min(stronghold.CivilianActor.Food, consumption);
        stronghold.CivilianActor.Food -= deducted;

        ApplyPopularFeelingsFromFood(stronghold, consumption, deducted);
        return deducted;
    }

    /// <summary>按城内驻军扣除府库携行粮（与地图军事单位同口径：约 3 合/人/日）。</summary>
    /// <returns>本日实际扣除的合数。</returns>
    public static int ApplyDailyGarrisonFoodConsumption(Stronghold stronghold)
    {
        var soldiers = StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold);
        if (soldiers <= 0)
            return 0;

        var consumption = LogisticsCalculator.CalculateUnitDailyFoodConsumption(soldiers);
        if (consumption <= 0)
            return 0;

        var deducted = Math.Min(stronghold.ForceActor.Food, consumption);
        stronghold.ForceActor.Food -= deducted;
        return deducted;
    }

    /// <summary>缺粮降民心，充足缓慢恢复（M4-d）。</summary>
    public static void ApplyPopularFeelingsFromFood(
        Stronghold stronghold,
        int requiredConsumption,
        int actualDeducted)
    {
        var feelings = stronghold.CivilianActor.PopularFeelings;

        // 业务：缺粮扣民心，充足则缓慢恢复至上限
        if (requiredConsumption > 0 && actualDeducted < requiredConsumption)
        {
            feelings = (byte)Math.Max(0, feelings - MarketConstants.PopularFeelingsFoodShortagePenalty);
        }
        else if (feelings < MarketConstants.PopularFeelingsRecoveryCap)
        {
            feelings = (byte)Math.Min(
                MarketConstants.PopularFeelingsRecoveryCap,
                feelings + 1);
        }

        stronghold.CivilianActor.PopularFeelings = feelings;
    }

    private static int TransferMoney(StrongholdActor from, StrongholdActor to, int amount)
    {
        if (amount <= 0)
            return 0;

        var paid = Math.Min(from.Money, amount);
        from.Money -= paid;
        to.Money += paid;
        return paid;
    }
}
