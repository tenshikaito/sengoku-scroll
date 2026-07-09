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

    private sealed record YearlyArrivalRecord(int Year, TributeArrivalRecord Record);

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

    private static TributeSettlementSummary BuildSummary(
        int reportingYear,
        int reportingMonth,
        IReadOnlyList<TributeArrivalRecord> lines)
    {
        return new TributeSettlementSummary(
            reportingYear,
            reportingMonth,
            lines.Sum(l => l.Food),
            lines.Sum(l => l.Money),
            lines.Count,
            lines);
    }
}
