namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>商户贸易税待缴台账（成交时记账，月 1 日征收）。</summary>
public sealed class MerchantTaxLedger
{
    private readonly Dictionary<(int StrongholdId, int MerchantActorId), int> accrued = [];

    public void Accrue(int strongholdId, int merchantActorId, int tradeTaxMoney)
    {
        if (tradeTaxMoney <= 0)
            return;

        var key = (strongholdId, merchantActorId);
        accrued[key] = accrued.GetValueOrDefault(key) + tradeTaxMoney;
    }

    /// <summary>征收指定据点全部待缴贸易税，返回总额。</summary>
    public int CollectForStronghold(int strongholdId)
    {
        var total = 0;
        var keys = accrued.Keys.Where(k => k.StrongholdId == strongholdId).ToList();

        foreach (var key in keys)
        {
            total += accrued[key];
            accrued.Remove(key);
        }

        return total;
    }

    public int GetAccrued(int strongholdId, int merchantActorId)
        => accrued.GetValueOrDefault((strongholdId, merchantActorId));
}
