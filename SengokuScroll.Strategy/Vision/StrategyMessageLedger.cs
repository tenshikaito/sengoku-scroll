namespace SengokuScroll.Strategy.Vision;

/// <summary>广义 Message 去重（UDP 语义骨架）。</summary>
public sealed class StrategyMessageLedger
{
    private readonly HashSet<string> seenKeys = new(StringComparer.Ordinal);

    public bool TryAccept(string messageKey)
        => seenKeys.Add(messageKey);

    public IReadOnlyList<string> Snapshot()
        => [.. seenKeys.Order(StringComparer.Ordinal)];

    public void Restore(IEnumerable<string> restored)
    {
        seenKeys.Clear();
        foreach (var key in restored)
            seenKeys.Add(key);
    }

    public void Clear() => seenKeys.Clear();
}
