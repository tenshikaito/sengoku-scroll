using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>经济结算纯计算（M4-a 整型 + M3-d 过渡月结）。</summary>
public static class EconomyCalculator
{
    /// <summary>按比例计算：amount × rateBp / 10000，税收向下取整。</summary>
    public static int ApplyBasisPointsTax(int amount, int rateBp)
        => amount <= 0 || rateBp <= 0 ? 0 : amount * rateBp / EconomyConstants.BasisPointsPer100Percent;

    /// <summary>按比例分配：amount × shareBp / 10000，四舍五入。</summary>
    public static int ApplyBasisPointsShare(int amount, int shareBp)
        => amount <= 0 || shareBp <= 0
            ? 0
            : (amount * shareBp + EconomyConstants.BasisPointsPer100Percent / 2)
              / EconomyConstants.BasisPointsPer100Percent;

    /// <summary>中央实际收税比例（万分比）。</summary>
    public static int CalculateCollectionEfficiencyBp(Stronghold stronghold)
        => CalculateCollectionEfficiencyBp(stronghold, gameData: null, meta: null);

    /// <summary>中央实际收税比例（万分比）；传入 world 上下文时计入距居城行政损耗。</summary>
    public static int CalculateCollectionEfficiencyBp(
        Stronghold stronghold,
        GameData? gameData,
        StrategyScenarioMeta? meta)
    {
        if (stronghold.LordId > 0)
            return EconomyConstants.BasisPointsPer100Percent;

        // 业务：权威 50→7500bp 基准，腐败（含距离损耗）每点扣约 67bp，虚构据点打折
        var corruption = gameData is not null && meta is not null
            ? AdministrationCalculator.CalculateEffectiveCorruption(stronghold, gameData, meta)
            : stronghold.Corruption;

        var authorityFactor = 5000 + stronghold.Authority * 50;
        var corruptionFactor = EconomyConstants.BasisPointsPer100Percent
            - corruption * EconomyConstants.BasisPointsPer100Percent / 150;
        // 业务：史实据点全额；虚构据点按开局/剧本配置的收入系数打折
        var historicalFactor = stronghold.IsHistorical
            ? EconomyConstants.BasisPointsPer100Percent
            : ResolveFictionalIncomePenaltyBp(meta);

        var product = (long)authorityFactor * corruptionFactor * historicalFactor;
        var scaled = (int)(product / EconomyConstants.BasisPointsPer100Percent
                           / EconomyConstants.BasisPointsPer100Percent);

        return Math.Clamp(
            scaled,
            EconomyConstants.MinCollectionEfficiencyBp,
            EconomyConstants.BasisPointsPer100Percent);
    }

    /// <summary>解析虚构据点收入系数：剧本 gameOptions，否则固定 80%。</summary>
    public static int ResolveFictionalIncomePenaltyBp(StrategyScenarioMeta? meta)
    {
        var fromScenario = meta?.GameOptions.FictionalIncomePenaltyBp ?? 0;
        if (fromScenario > 0)
            return Math.Clamp(fromScenario, 1, EconomyConstants.BasisPointsPer100Percent);

        return EconomyConstants.FictionalIncomePenaltyBp;
    }

    /// <summary>市民农业日产粮（合/日）。</summary>
    public static int CalculateDailyFoodProduction(Stronghold stronghold)
        => stronghold.CivilianActor.AgricultureProduction / EconomyConstants.DaysPerMonth;

    /// <summary>市民商业日产钱（文/日）。</summary>
    public static int CalculateDailyMoneyProduction(Stronghold stronghold)
        => stronghold.CivilianActor.CommerceProduction / EconomyConstants.DaysPerMonth;

    /// <summary>商业值线性换算最大店铺数。</summary>
    public static int CalculateMaxMerchantShops(Stronghold stronghold)
        => stronghold.CommerceValue / EconomyConstants.CommerceValuePerShopSlot;

    /// <summary>月度人头税（文，从市民征收）。</summary>
    public static int CalculateMonthlyPollTaxMoney(
        Stronghold stronghold,
        GameData? gameData = null,
        StrategyScenarioMeta? meta = null)
    {
        var baseTax = stronghold.Population * stronghold.PollTaxRate / 5;
        return ApplyBasisPointsTax(baseTax, CalculateCollectionEfficiencyBp(stronghold, gameData, meta));
    }

    /// <summary>月度基础商业税（文，按商业值）。</summary>
    public static int CalculateMonthlyCommerceBaseTaxMoney(
        Stronghold stronghold,
        GameData? gameData = null,
        StrategyScenarioMeta? meta = null)
    {
        var baseTax = stronghold.CommerceValue * stronghold.CommerceTaxRate / 20;
        return ApplyBasisPointsTax(baseTax, CalculateCollectionEfficiencyBp(stronghold, gameData, meta));
    }

    /// <summary>运输队载货折钱（文）：现金 + 粮×当地粮价。</summary>
    public static int CalculateConvoyCargoMoneyValue(int cargoMoney, int cargoFoodGo, int foodPriceMoneyPerGo)
    {
        if (foodPriceMoneyPerGo <= 0)
            foodPriceMoneyPerGo = 1;

        return cargoMoney + cargoFoodGo * foodPriceMoneyPerGo;
    }

    /// <summary>贸易队途经关税（文）。</summary>
    public static int CalculateConvoyTariffMoney(Stronghold stronghold, int cargoMoneyValue)
    {
        if (cargoMoneyValue <= 0 || stronghold.TariffTaxRate <= 0)
            return 0;

        var rateBp = stronghold.TariffTaxRate * 100;
        return ApplyBasisPointsTax(cargoMoneyValue, rateBp);
    }

    /// <summary>据点月度金钱税收（人头税 + 商业税 + 关税；M3-d 过渡公式，保底 1 万文）。</summary>
    public static int CalculateStrongholdMonthlyTaxMoney(Stronghold stronghold)
    {
        var poll = stronghold.Population * stronghold.PollTaxRate / 5;
        var commerce = stronghold.Population * stronghold.CommerceTaxRate / 20;
        var tariff = stronghold.Population * stronghold.TariffTaxRate / 40;
        return Math.Max(10_000, poll + commerce + tariff);
    }

    /// <summary>据点月度粮草税收（农业税，合；M3-d 过渡公式）。</summary>
    public static int CalculateStrongholdMonthlyTaxFood(Stronghold stronghold)
    {
        var agriculture = stronghold.Population * stronghold.AgricultureTaxRate / 4;
        return Math.Max(10_000, agriculture);
    }

    /// <summary>军事单位月度维护费（金钱，最小单位/文）。</summary>
    public static int CalculateUnitMonthlyMaintenanceMoney(Unit unit)
        => unit.IsMilitary && unit.Soldier > 0 ? Math.Max(100, unit.Soldier / 2) : 0;

    /// <summary>据点月度维持费（金钱，最小单位/文）。</summary>
    public static int CalculateStrongholdMonthlyMaintenanceMoney(Stronghold stronghold)
        => Math.Max(800, stronghold.Population / 5);

    /// <summary>驻城专业队（UnitId=0 非足轻）月度维护费。</summary>
    public static int CalculateGarrisonProfessionalMaintenanceMoney(Stronghold stronghold, GameData gameData)
    {
        var total = 0;

        foreach (var subId in stronghold.ForceActor.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub)
                || sub.UnitId != 0
                || sub.Soldier <= 0
                || sub.TypeId == StrategyTroopTypes.Ashigaru)
                continue;

            total += CalculateGarrisonSubUnitMaintenanceMoney(sub);
        }

        return total;
    }

    /// <summary>单个驻城 SubUnit 月维持费（文）。</summary>
    public static int CalculateGarrisonSubUnitMaintenanceMoney(SubUnit sub)
    {
        if (sub.Soldier <= 0)
            return 0;

        if (sub.TypeId == StrategyTroopTypes.Ashigaru)
            return 0;

        var baseCost = Math.Max(
            50,
            sub.Soldier * GarrisonConstants.ProfessionalMaintenanceMoneyPerSoldier);

        var mountedMultiplier = sub.IsMounted && sub.TypeId != StrategyTroopTypes.Cavalry ? 15_000 : 10_000;

        return sub.TypeId switch
        {
            StrategyTroopTypes.Cavalry => baseCost * GarrisonConstants.CavalryMaintenanceMultiplier,
            StrategyTroopTypes.Matchlock => baseCost * GarrisonConstants.MatchlockMaintenanceMultiplier
                                          * (sub.IsMounted ? mountedMultiplier : 10_000)
                                          / EconomyConstants.BasisPointsPer100Percent,
            StrategyTroopTypes.Archer => baseCost * GarrisonConstants.ArcherMaintenanceMultiplierBp
                                         * (sub.IsMounted ? mountedMultiplier : 10_000)
                                         / EconomyConstants.BasisPointsPer100Percent,
            _ => baseCost
        };
    }

    /// <summary>势力月度俸禄支出（文）。</summary>
    public static int CalculateForceMonthlySalaryExpense(Force force, GameData gameData)
        => gameData.Characters
            .Values
            .Where(c => c.ForceId == force.Id && c.Salary > 0)
            .Sum(c => c.Salary);

    /// <summary>年度人口增长（人；民心 &lt;40 流失，≥70 增长）。</summary>
    public static int CalculateAnnualPopulationGrowth(Stronghold stronghold)
    {
        // 业务：民心低迷每年流失约 0.5% 人口
        if (stronghold.CivilianActor.PopularFeelings < 40)
            return -Math.Max(1, stronghold.Population / 200);

        // 业务：民心旺盛每年增长约 1% 人口
        if (stronghold.CivilianActor.PopularFeelings >= 70)
            return Math.Max(1, stronghold.Population / 100);

        return 0;
    }
}
