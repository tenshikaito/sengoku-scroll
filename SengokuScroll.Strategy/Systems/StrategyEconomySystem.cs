using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式经济系统接口。</summary>
public interface IStrategyEconomySystem : IEconomySystem
{
}

/// <summary>
/// 策略经济系统：每日军事单位粮耗；每月 1 日维持费结算，并汇总上月/上年贡纳到账（M3-d）。
/// </summary>
public class StrategyEconomySystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyTributeLedger tributeLedger) : IStrategyEconomySystem
{
    /// <summary>在气候系统之后、后勤系统之前执行。</summary>
    public int Order { get; } = 10;

    /// <inheritdoc />
    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var gameDate = gameData.GameDate;

        foreach (var unit in context.GameWorldContext.EachUnit())
        {
            if (!EconomyRules.ShouldConsumeDailyFood(unit))
                continue;

            UnitEconomyActions.ApplyDailyFoodConsumption(unit);
        }

        if (!EconomyRules.IsMonthlySettlementDay(gameDate))
            return;

        var (reportYear, reportMonth) = ResolvePreviousMonth(gameDate);
        var playerMaintenance = 0;
        var playerArmyMaintenance = 0;

        foreach (var force in gameData.Forces.Values)
        {
            var result = ForceEconomyActions.ApplyMonthlyMaintenance(force, gameData);
            if (force.Id != scenarioMeta.PlayerForceId)
                continue;

            playerMaintenance = result.ExpenseMoney;
            playerArmyMaintenance = result.ArmyMaintenanceMoney;
        }

        if (!gameData.Forces.TryGetValue(scenarioMeta.PlayerForceId, out var playerForce))
            return;

        EmitMonthlySettlement(
            reportYear,
            reportMonth,
            playerMaintenance,
            playerArmyMaintenance,
            playerForce);

        if (gameDate.Month == 1)
        {
            EmitAnnualSettlement(
                gameDate.Year - 1,
                playerMaintenance,
                playerArmyMaintenance,
                playerForce);
        }
    }

    private void EmitMonthlySettlement(
        int reportYear,
        int reportMonth,
        int playerMaintenance,
        int playerArmyMaintenance,
        Domain.Entities.Force playerForce)
    {
        var tributeSummary = tributeLedger.ConsumeMonthlySettlement(reportYear, reportMonth);
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
            Brief = $"📋 {reportYear}年{reportMonth}月收支结算",
            Message =
                $"📋 月度收支结算（{reportYear}年{reportMonth}月）\n" +
                $"共 {tributeSummary.ConvoyCount} 批运输队抵达当主居城。\n" +
                $"合计收入 🌾{tributeSummary.TotalFood:N0} 💰{tributeSummary.TotalMoney:N0}\n" +
                $"支出 💰{playerMaintenance:N0}（含军队维护 💰{playerArmyMaintenance:N0}）\n" +
                $"库藏 💰{playerForce.Money:N0} 🌾{playerForce.Food:N0}\n" +
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
        var tributeSummary = tributeLedger.ConsumeAnnualSettlement(reportYear);
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
            Brief = $"📋 {reportYear}年年度收支结算",
            Message =
                $"📋 年度收支结算（{reportYear}年）\n" +
                $"共 {tributeSummary.ConvoyCount} 批运输队抵达当主居城。\n" +
                $"合计收入 🌾{tributeSummary.TotalFood:N0} 💰{tributeSummary.TotalMoney:N0}\n" +
                $"支出 💰{playerMaintenance:N0}（含军队维护 💰{playerArmyMaintenance:N0}）\n" +
                $"库藏 💰{playerForce.Money:N0} 🌾{playerForce.Food:N0}\n" +
                detailLines,
            EconomySettlement = settlement
        });
    }

    private static StrategyEconomySettlementDetailDto BuildSettlementDetail(
        string period,
        StrategyTributeLedger.TributeSettlementSummary tributeSummary,
        int playerMaintenance,
        int playerArmyMaintenance,
        Domain.Entities.Force playerForce)
        => new()
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
            TributeLines = tributeSummary.Lines
                .Select(l => new StrategyTributeLineDto
                {
                    OriginName = l.OriginName,
                    Food = l.Food,
                    Money = l.Money
                })
                .ToList()
        };

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
}
