using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式经济系统接口。</summary>
public interface IStrategyEconomySystem : IEconomySystem
{
}

/// <summary>
/// 策略经济系统：每日生产/口粮、收粮税、月钱税；每月维持费与贡纳报告（M4-b）。
/// </summary>
public class StrategyEconomySystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyTributeLedger tributeLedger,
    MerchantTaxLedger merchantTaxLedger,
    TariffTaxLedger tariffTaxLedger,
    MonthlyTaxCollectionLedger monthlyTaxCollectionLedger,
    SupplyConvoyDispatchHelper dispatchHelper,
    BattleReportDeliveryHelper battleReportDeliveryHelper) : IStrategyEconomySystem
{
    /// <summary>在市场系统之后、后勤系统之前执行。</summary>
    public int Order { get; } = 10;

    /// <inheritdoc />
    public void Update()
    {
        var world = context.GameWorldContext.GameWorld;
        var gameData = world.GameData;
        var gameDate = gameData.GameDate;

        // 阶段1：逐据点日更——日产、收粮税、市民口粮
        foreach (var stronghold in context.GameWorldContext.EachStronghold())
        {
            var regionId = RegionLocationHelper.ResolveRegionId(world, stronghold.Location);
            // 业务：收粮日跳过农业日产，避免与收粮结算重复计粮
            var skipFoodProduction = HarvestRules.ShouldSkipDailyFoodProduction(
                stronghold,
                gameDate,
                scenarioMeta.RegionHarvestProfiles,
                regionId);

            if (EconomyRules.ShouldApplyDailyProduction(stronghold))
            {
                StrongholdEconomyActions.ApplyDailyProduction(
                    stronghold,
                    gameData,
                    skipFoodProduction);
            }

            if (HarvestRules.ResolveTodayEvent(gameDate, scenarioMeta.RegionHarvestProfiles, regionId)
                is { } harvestEvent)
            {
                var settlement = HarvestEconomyActions.ApplyHarvestSettlement(
                    stronghold,
                    harvestEvent,
                    HarvestConstants.DefaultInternalTributeFoodBp,
                    gameData,
                    scenarioMeta.RegionHarvestProfiles,
                    regionId);

                if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                    ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

                if (settlement.TributeObligationGo > 0)
                    dispatchHelper.DispatchHarvestFoodTribute(stronghold, settlement.TributeObligationGo);
            }

            AgricultureProgressActions.AdvanceDailyProgress(
                stronghold,
                gameData,
                gameDate,
                scenarioMeta.RegionHarvestProfiles,
                regionId);

            if (EconomyRules.ShouldConsumeDailyCivilianFood(stronghold))
                StrongholdEconomyActions.ApplyDailyCivilianFoodConsumption(stronghold);

            if (EconomyRules.ShouldConsumeDailyGarrisonFood(stronghold))
            {
                StrongholdEconomyActions.ApplyDailyGarrisonFoodConsumption(stronghold);
                if (gameData.Forces.TryGetValue(stronghold.ForceId, out var garrisonForce))
                    ForceEconomyActions.SyncForceTreasuryFromStrongholds(garrisonForce, gameData);
            }
        }

        // 阶段2：月初逐据点征收钱税、店铺维持费
        if (EconomyRules.IsMonthlySettlementDay(gameDate))
        {
            foreach (var stronghold in context.GameWorldContext.EachStronghold())
            {
                var (poll, commerce, trade, tariff) = StrongholdEconomyActions.ApplyMonthlyMoneyTaxes(
                    stronghold,
                    merchantTaxLedger,
                    tariffTaxLedger,
                    gameData,
                    scenarioMeta);

                monthlyTaxCollectionLedger.RecordMonthlyMoneyTaxes(
                    stronghold.Id,
                    poll,
                    commerce,
                    trade,
                    tariff);

                StrongholdEconomyActions.ApplyMerchantShopMaintenance(stronghold);

                if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
                    ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
            }
        }

        // 强制解散：需连续断粮多日且士气归零（见 Unit.SupplyCollapseDays）
        foreach (var unit in context.GameWorldContext.EachUnit())
        {
            if (!EconomyRules.ShouldConsumeDailyFood(unit))
                continue;

            UnitEconomyActions.ApplyDailyFoodConsumption(unit);
            UnitMoraleRules.ApplyDailySupplyCollapseTracking(unit, gameData);
        }

        UnitMoraleRules.ProcessForcedDisbands(
            context.GameWorldContext,
            gameData,
            scenarioMeta,
            dayOutcomeBuffer,
            battleReportDeliveryHelper);

        // 阶段4：月初势力维持费与玩家收支报告；1 月追加年度人口与年报
        if (!EconomyRules.IsMonthlySettlementDay(gameDate))
            return;

        var (reportYear, reportMonth) = ResolvePreviousMonth(gameDate);
        var settlements = gameData.Forces.Values.OrderBy(f => f.Id)
            .ToDictionary(f => f.Id, f => ForceEconomyActions.ApplyMonthlyMaintenance(f, gameData));

        // World population growth must not depend on a surviving observer/player.
        if (gameDate.Month == 1)
            ApplyAnnualPopulationChange(gameData);

        foreach (var playerForce in gameData.Forces.Values.OrderBy(f => f.Id))
        {
            if (!StrategyForcePerspective.ReceivesReports(scenarioMeta, playerForce.Id)) continue;
            var settlement = settlements[playerForce.Id];
            EmitMonthlySettlement(reportYear, reportMonth, settlement.ExpenseMoney,
                settlement.ArmyMaintenanceMoney, playerForce);
            if (gameDate.Month == 1)
                EmitAnnualSettlement(gameDate.Year - 1, settlement.ExpenseMoney,
                    settlement.ArmyMaintenanceMoney, playerForce);
        }
    }

    private void EmitMonthlySettlement(
        int reportYear,
        int reportMonth,
        int playerMaintenance,
        int playerArmyMaintenance,
        Domain.Entities.Force playerForce)
    {
        var tributeSummary = tributeLedger.ConsumeMonthlySettlement(reportYear, reportMonth, playerForce.Id);
        var settlement = BuildSettlementDetail(
            "Monthly",
            tributeSummary,
            playerMaintenance,
            playerArmyMaintenance,
            playerForce);

        var detailLines = FormatDetailLines(tributeSummary, "上月无运输队抵达当主居城。");

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "EconomyMonthly",
            RecipientForceId = playerForce.Id,
            Brief = $"📋 {reportYear}年{reportMonth}月收支结算",
            Message =
                $"📋 月度收支结算（{reportYear}年{reportMonth}月）\n" +
                $"共 {tributeSummary.ConvoyCount} 批运输队抵达当主居城。\n" +
                $"上月贡纳收入 🌾{tributeSummary.TotalFood:N0}合 💰{tributeSummary.TotalMoney:N0}文\n" +
                $"本月维持费支出 💰{playerMaintenance:N0}文（含军队维护 💰{playerArmyMaintenance:N0}文）\n" +
                $"结算后库藏 💰{playerForce.Money:N0}文 🌾{playerForce.Food:N0}合\n" +
                detailLines,
            EconomySettlement = settlement
        });
    }

    private void EmitAnnualSettlement(
        int reportYear,
        int playerMaintenance,
        int playerArmyMaintenance,
        Domain.Entities.Force playerForce)
    {
        var tributeSummary = tributeLedger.ConsumeAnnualSettlement(reportYear, playerForce.Id);
        var settlement = BuildSettlementDetail(
            "Annual",
            tributeSummary,
            playerMaintenance,
            playerArmyMaintenance,
            playerForce);

        var detailLines = FormatDetailLines(tributeSummary, "上年无运输队抵达当主居城。");

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "EconomyAnnual",
            RecipientForceId = playerForce.Id,
            Brief = $"📋 {reportYear}年年度收支结算",
            Message =
                $"📋 年度收支结算（{reportYear}年）\n" +
                $"共 {tributeSummary.ConvoyCount} 批运输队抵达当主居城。\n" +
                $"年度贡纳收入 🌾{tributeSummary.TotalFood:N0}合 💰{tributeSummary.TotalMoney:N0}文\n" +
                $"本月维持费支出 💰{playerMaintenance:N0}文（含军队维护 💰{playerArmyMaintenance:N0}文）\n" +
                $"结算后库藏 💰{playerForce.Money:N0}文 🌾{playerForce.Food:N0}合\n" +
                detailLines,
            EconomySettlement = settlement
        });
    }

    private StrategyEconomySettlementDetailDto BuildSettlementDetail(
        string period,
        StrategyTributeLedger.TributeSettlementSummary tributeSummary,
        int playerMaintenance,
        int playerArmyMaintenance,
        Domain.Entities.Force playerForce)
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        return new StrategyEconomySettlementDetailDto
        {
            Period = period,
            ReportingYear = tributeSummary.ReportingYear,
            ReportingMonth = tributeSummary.ReportingMonth,
            TotalFood = tributeSummary.TotalFood,
            TotalMoney = tributeSummary.TotalMoney,
            ExpenseMoney = playerMaintenance,
            ArmyMaintenanceMoney = playerArmyMaintenance,
            TreasuryMoney = playerForce.Money,
            TreasuryFood = playerForce.Food,
            ConvoyCount = tributeSummary.ConvoyCount,
            TributeLines = [.. tributeSummary.Lines.Select(l => MapTributeLine(l, gameData))]
        };
    }

    private StrategyTributeLineDto MapTributeLine(
        StrategyTributeLedger.TributeArrivalRecord line,
        Domain.GameData gameData)
    {
        if (!gameData.Strongholds.TryGetValue(line.OriginStrongholdId, out var stronghold))
        {
            return new StrategyTributeLineDto
            {
                OriginName = line.OriginName,
                ForceName = "—",
                LordName = "—",
                Food = line.Food,
                Money = line.Money
            };
        }

        var forceName = gameData.Forces.TryGetValue(stronghold.ForceId, out var force)
            ? force.Name
            : "未知势力";
        var lordName = StrategyStrongholdLordHelper.ResolveStrongholdLordName(
            stronghold,
            scenarioMeta,
            gameData);

        return new StrategyTributeLineDto
        {
            OriginName = stronghold.Name,
            ForceName = forceName,
            LordName = lordName,
            Food = line.Food,
            Money = line.Money
        };
    }

    private static string FormatDetailLines(
        StrategyTributeLedger.TributeSettlementSummary tributeSummary,
        string emptyText)
        => tributeSummary.Lines.Count == 0
            ? emptyText
            : string.Join("\n", tributeSummary.Lines.Select(l =>
                $"· {l.OriginName}：🌾{l.Food:N0} 💰{l.Money:N0}"));

    private static (int Year, int Month) ResolvePreviousMonth(GameDate gameDate)
    {
        if (gameDate.Month <= 1)
            return (gameDate.Year - 1, 12);

        return (gameDate.Year, gameDate.Month - 1);
    }

    private static void ApplyAnnualPopulationChange(Domain.GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            var delta = EconomyCalculator.CalculateAnnualPopulationGrowth(stronghold);
            if (delta == 0)
                continue;

            stronghold.Population = Math.Max(100, stronghold.Population + delta);
        }
    }
}
