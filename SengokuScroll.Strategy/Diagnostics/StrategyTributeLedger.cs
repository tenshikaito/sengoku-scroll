namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 记录运输队抵达玩家当主居城的贡纳，供每月/每年 1 日汇总到账。
/// </summary>
public sealed class StrategyTributeLedger
{
    private readonly List<TributeArrivalRecord> monthlyArrivals = [];
    private readonly List<YearlyArrivalRecord> yearlyArrivals = [];

    public sealed record TributeArrivalRecord(
        int OriginStrongholdId,
        string OriginName,
        int Food,
        int Money);

    public sealed record YearlyArrivalRecord(int Year, TributeArrivalRecord Record);

    public sealed record State(
        IReadOnlyList<TributeArrivalRecord> MonthlyArrivals,
        IReadOnlyList<YearlyArrivalRecord> YearlyArrivals);

    public sealed record TributeSettlementSummary(
        int ReportingYear,
        int ReportingMonth,
        int TotalFood,
        int TotalMoney,
        int ConvoyCount,
        IReadOnlyList<TributeArrivalRecord> Lines);

    public void RecordArrival(
        int calendarYear,
        int originStrongholdId,
        string originName,
        int food,
        int money)
    {
        if (food <= 0 && money <= 0)
            return;

        var record = new TributeArrivalRecord(originStrongholdId, originName, food, money);
        monthlyArrivals.Add(record);
        yearlyArrivals.Add(new YearlyArrivalRecord(calendarYear, record));
    }

    public TributeSettlementSummary ConsumeMonthlySettlement(int reportingYear, int reportingMonth)
    {
        var lines = monthlyArrivals.ToList();
        monthlyArrivals.Clear();

        return BuildSummary(reportingYear, reportingMonth, lines);
    }

    public TributeSettlementSummary ConsumeAnnualSettlement(int reportingYear)
    {
        var lines = yearlyArrivals
            .Where(a => a.Year == reportingYear)
            .Select(a => a.Record)
            .ToList();

        yearlyArrivals.RemoveAll(a => a.Year == reportingYear);

        return BuildSummary(reportingYear, 0, lines);
    }

    public State Snapshot()
        => new(monthlyArrivals.ToList(), yearlyArrivals.ToList());

    public void Restore(State restored)
    {
        monthlyArrivals.Clear();
        monthlyArrivals.AddRange(restored.MonthlyArrivals);
        yearlyArrivals.Clear();
        yearlyArrivals.AddRange(restored.YearlyArrivals);
    }

    private static TributeSettlementSummary BuildSummary(
        int reportingYear,
        int reportingMonth,
        IReadOnlyList<TributeArrivalRecord> lines)
    {
        var aggregated = lines
            .GroupBy(l => l.OriginStrongholdId)
            .Select(g => new TributeArrivalRecord(
                g.Key,
                g.First().OriginName,
                g.Sum(x => x.Food),
                g.Sum(x => x.Money)))
            .OrderBy(l => l.OriginName)
            .ToList();

        return new TributeSettlementSummary(
            reportingYear,
            reportingMonth,
            aggregated.Sum(l => l.Food),
            aggregated.Sum(l => l.Money),
            lines.Count,
            aggregated);
    }
}
