using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>当月钱税征收汇总，供月 1 日钱纳运输队计算义务（M4-c）。</summary>
public sealed class MonthlyTaxCollectionLedger
{
    private readonly Dictionary<int, int> moneyTributeObligationByStronghold = [];

    public sealed record Obligation(int StrongholdId, int Money);

    /// <summary>记录本据点上月征收的钱税合计，并换算钱纳义务。</summary>
    public void RecordMonthlyMoneyTaxes(
        int strongholdId,
        int pollTaxPaid,
        int commerceTaxPaid,
        int tradeTaxPaid,
        int tariffTaxPaid)
    {
        var total = pollTaxPaid + commerceTaxPaid + tradeTaxPaid + tariffTaxPaid;
        if (total <= 0)
        {
            moneyTributeObligationByStronghold.Remove(strongholdId);
            return;
        }

        moneyTributeObligationByStronghold[strongholdId] = EconomyCalculator.ApplyBasisPointsShare(
            total,
            HarvestConstants.DefaultInternalTributeMoneyBp);
    }

    /// <summary>读取并清除钱纳义务（月 1 日运输队派遣前调用）。</summary>
    public int ConsumeMoneyTributeObligation(int strongholdId)
    {
        if (!moneyTributeObligationByStronghold.Remove(strongholdId, out var obligation))
            return 0;

        return obligation;
    }

    public IReadOnlyList<Obligation> Snapshot()
        => [.. moneyTributeObligationByStronghold.OrderBy(x => x.Key).Select(x => new Obligation(x.Key, x.Value))];

    public void Restore(IEnumerable<Obligation> restored)
    {
        moneyTributeObligationByStronghold.Clear();
        foreach (var entry in restored.Where(x => x.Money > 0))
            moneyTributeObligationByStronghold[entry.StrongholdId] = entry.Money;
    }
}
