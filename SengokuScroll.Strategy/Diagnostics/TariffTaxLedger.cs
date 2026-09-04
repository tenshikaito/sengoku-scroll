namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>关税待入库台账（贸易队过境记账，月 1 日征收入府库）。</summary>
public sealed class TariffTaxLedger
{
    private readonly Dictionary<int, int> accruedByStronghold = [];
    private readonly HashSet<(int ConvoyId, int StrongholdId)> chargedTransit = [];

    public sealed record Accrual(int StrongholdId, int Money);

    public sealed record TransitCharge(int ConvoyId, int StrongholdId);

    public bool HasChargedTransit(int convoyId, int strongholdId)
        => chargedTransit.Contains((convoyId, strongholdId));

    public void Accrue(int strongholdId, int tariffMoney)
    {
        if (tariffMoney <= 0)
            return;

        accruedByStronghold[strongholdId] = accruedByStronghold.GetValueOrDefault(strongholdId) + tariffMoney;
    }

    public void MarkTransitCharged(int convoyId, int strongholdId)
        => chargedTransit.Add((convoyId, strongholdId));

    /// <summary>征收指定据点全部待缴关税，返回总额。</summary>
    public int CollectForStronghold(int strongholdId)
    {
        if (!accruedByStronghold.Remove(strongholdId, out var total))
            return 0;

        return total;
    }

    public int GetAccrued(int strongholdId)
        => accruedByStronghold.GetValueOrDefault(strongholdId);

    public IReadOnlyList<Accrual> SnapshotAccruals()
        => [.. accruedByStronghold.Select(x => new Accrual(x.Key, x.Value))];

    public IReadOnlyList<TransitCharge> SnapshotTransitCharges()
        => [.. chargedTransit.Select(x => new TransitCharge(x.ConvoyId, x.StrongholdId))];

    public void Restore(IEnumerable<Accrual> accruals, IEnumerable<TransitCharge> transitCharges)
    {
        accruedByStronghold.Clear();
        chargedTransit.Clear();

        foreach (var entry in accruals.Where(x => x.Money > 0))
            accruedByStronghold[entry.StrongholdId] = entry.Money;
        foreach (var entry in transitCharges)
            chargedTransit.Add((entry.ConvoyId, entry.StrongholdId));
    }
}
