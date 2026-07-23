using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Actions;

/// <summary>势力级经济变更（月结等）。</summary>
public static class ForceEconomyActions
{
    /// <summary>单据点月度收支明细行。</summary>
    public sealed record StrongholdSettlementLine(
        int StrongholdId,
        string StrongholdName,
        int IncomeMoney,
        int IncomeFood,
        int ExpenseMoney);

    /// <summary>势力月度维持费结算汇总。</summary>
    public sealed record MonthlySettlementResult(
        int IncomeMoney,
        int IncomeFood,
        int ExpenseMoney,
        int ArmyMaintenanceMoney,
        IReadOnlyList<StrongholdSettlementLine> StrongholdLines);

    /// <summary>
    /// 月度维持费：据点维持费自据点扣；军队维护费自势力金库扣；同步势力汇总。
    /// 税收改由月初贡纳运输队送达当主居城后入账，不在此即时结算。
    /// </summary>
    public static MonthlySettlementResult ApplyMonthlyMaintenance(Force force, GameData gameData)
    {
        var lines = new List<StrongholdSettlementLine>();
        var expenseMoney = 0;

        foreach (var stronghold in gameData.Strongholds.Values.Where(s => s.ForceId == force.Id))
        {
            var maintenance = EconomyCalculator.CalculateStrongholdMonthlyMaintenanceMoney(stronghold);

            stronghold.ForceActor.Money = Math.Max(0, stronghold.ForceActor.Money - maintenance);
            expenseMoney += maintenance;

            lines.Add(new StrongholdSettlementLine(
                stronghold.Id,
                stronghold.Name,
                0,
                0,
                maintenance));
        }

        var armyMaintenance = 0;
        foreach (var unit in gameData.Units.Values.Where(u => u.ForceId == force.Id))
            armyMaintenance += EconomyCalculator.CalculateUnitMonthlyMaintenanceMoney(unit);

        var salaryExpense = PayForceMonthlySalaries(force, gameData);

        SyncForceTreasuryFromStrongholds(force, gameData);
        force.Money = Math.Max(0, force.Money - armyMaintenance);
        expenseMoney += armyMaintenance + salaryExpense;

        return new MonthlySettlementResult(
            0,
            0,
            expenseMoney,
            armyMaintenance,
            lines);
    }

    /// <summary>势力金库/粮库 = 旗下据点的官府库存合计。</summary>
    public static void SyncForceTreasuryFromStrongholds(Force force, GameData gameData)
    {
        var money = 0;
        var food = 0;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != force.Id)
                continue;

            money += stronghold.ForceActor.Money;
            food += stronghold.ForceActor.Food;
        }

        force.Money = money;
        force.Food = food;
    }

    private static int PayForceMonthlySalaries(Force force, GameData gameData)
    {
        var paidTotal = 0;

        foreach (var character in gameData.Characters.Values
                     .Where(c => c.ForceId == force.Id && !c.IsDead && c.Salary > 0)
                     .OrderByDescending(c => c.Salary))
        {
            var paid = DeductForceMoney(force, gameData, character.Salary);
            if (paid <= 0)
                continue;

            character.Money += paid;
            paidTotal += paid;
        }

        return paidTotal;
    }

    /// <summary>从势力据点府库扣款，返回实际扣除额。</summary>
    private static int DeductForceMoney(Force force, GameData gameData, int amount)
    {
        if (amount <= 0)
            return 0;

        // 业务：俸禄从旗下据点府库按余额从高到低依次扣款
        var remaining = amount;
        foreach (var stronghold in gameData.Strongholds.Values
                     .Where(s => s.ForceId == force.Id)
                     .OrderByDescending(s => s.ForceActor.Money))
        {
            if (remaining <= 0)
                break;

            var pay = Math.Min(remaining, stronghold.ForceActor.Money);
            stronghold.ForceActor.Money -= pay;
            remaining -= pay;
        }

        return amount - remaining;
    }
}
