using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Vision;

/// <summary>谍报登记目标类型。</summary>
public enum EspionageIntelTargetKind : byte
{
    Stronghold = 1,
    Unit = 2
}

/// <summary>谍报情报范围：军事（兵数/士气/城防）或内政（人口/税率/储粮）。</summary>
public enum EspionageIntelScope : byte
{
    Military = 1,
    Domestic = 2,
    Both = 3
}

/// <summary>谍报精度：模糊（高/中/低档位）或精确（具体数值）。</summary>
public enum EspionageIntelPrecision : byte
{
    Fuzzy = 1,
    Exact = 2
}

/// <summary>
/// 玩家对非自势力目标的情报台账（谍报任务登记；约 2 个月过期后恢复「未知」）。
/// 与视野无关：进入 visible 格也不自动暴露具体数值。
/// </summary>
public sealed class StrategyEspionageIntelLedger
{
    /// <summary>谍报有效期：自获得日起 2 个游戏月。</summary>
    private const int ExpiryMonths = 2;

    public sealed record Record(
        int ObserverForceId,
        EspionageIntelTargetKind TargetKind,
        int TargetId,
        EspionageIntelScope Scope,
        EspionageIntelPrecision Precision,
        GameDate AcquiredDate,
        GameDate ExpiresDate);

    private readonly Dictionary<(int ObserverForceId, EspionageIntelTargetKind Kind, int Id), Record> byTarget = [];

    /// <summary>登记或覆盖对某目标的谍报成果（同目标再次谍报以最新为准）。</summary>
    public void RecordMission(
        int observerForceId,
        EspionageIntelTargetKind targetKind,
        int targetId,
        EspionageIntelScope scope,
        EspionageIntelPrecision precision,
        GameDate acquiredDate)
    {
        if (observerForceId <= 0)
            return;

        var expires = AddMonths(acquiredDate, ExpiryMonths);
        byTarget[(observerForceId, targetKind, targetId)] = new Record(
            observerForceId,
            targetKind,
            targetId,
            scope,
            precision,
            acquiredDate,
            expires);
    }

    public Record? TryGet(int observerForceId, EspionageIntelTargetKind targetKind, int targetId)
    {
        if (!byTarget.TryGetValue((observerForceId, targetKind, targetId), out var record))
            return null;

        return record;
    }

    public IReadOnlyList<Record> Snapshot()
        => [.. byTarget.Values
            .OrderBy(r => r.ObserverForceId)
            .ThenBy(r => r.TargetKind)
            .ThenBy(r => r.TargetId)];

    public void Restore(IEnumerable<Record> restored, int legacyObserverForceId = 0)
    {
        byTarget.Clear();
        foreach (var record in restored)
        {
            var observerForceId = record.ObserverForceId > 0
                ? record.ObserverForceId
                : legacyObserverForceId;
            if (observerForceId <= 0)
                continue;

            var normalized = record with { ObserverForceId = observerForceId };
            byTarget[(observerForceId, record.TargetKind, record.TargetId)] = normalized;
        }
    }

    /// <summary>日推进时剔除过期条目，前端 DTO masking 随之恢复为「未知」。</summary>
    public void PruneExpired(GameDate currentDate)
    {
        foreach (var key in byTarget.Keys.ToList())
        {
            if (byTarget[key].ExpiresDate.CompareTo(currentDate) <= 0)
                byTarget.Remove(key);
        }
    }

    public void Clear() => byTarget.Clear();

    private static GameDate AddMonths(GameDate date, int months)
    {
        var month = date.Month + months;
        var year = date.Year;
        while (month > 12)
        {
            month -= 12;
            year += 1;
        }

        var day = Math.Min(date.Day, DaysInMonth(year, month));
        return new GameDate(year, month, day);
    }

    private static int DaysInMonth(int year, int month)
        => month switch
        {
            2 => DateTime.IsLeapYear(year) ? 29 : 28,
            4 or 6 or 9 or 11 => 30,
            _ => 31
        };
}
