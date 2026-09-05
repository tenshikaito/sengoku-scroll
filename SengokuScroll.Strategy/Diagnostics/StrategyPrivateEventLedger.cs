using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>Per-force delivery sequence; read is non-destructive, acknowledgement is explicit.</summary>
public sealed class StrategyPrivateEventLedger
{
    public const int CapacityPerForce = 4096;
    public sealed record Entry(long Sequence, StrategyEventDto Event);
    public sealed record Mailbox(int ForceId, long LastSequence, long AcknowledgedSequence, IReadOnlyList<Entry> Entries);
    public sealed record Batch(long AcknowledgedSequence, long LastSequence, bool HistoryTruncated, IReadOnlyList<Entry> Entries);
    private sealed class State
    {
        public long Last;
        public long Acknowledged;
        public Queue<Entry> Entries = new();
    }
    private readonly Dictionary<int, State> mailboxes = [];

    public void Add(int forceId, StrategyEventDto evt)
    {
        if (forceId <= 0) throw new ArgumentOutOfRangeException(nameof(forceId));
        if (!mailboxes.TryGetValue(forceId, out var box)) mailboxes[forceId] = box = new();
        box.Entries.Enqueue(new(checked(++box.Last), evt with { RecipientForceId = forceId }));
        if (box.Entries.Count > CapacityPerForce) box.Entries.Dequeue();
    }

    public Batch Read(int forceId)
    {
        if (!mailboxes.TryGetValue(forceId, out var box)) return new(0, 0, false, []);
        return new(box.Acknowledged, box.Last,
            box.Entries.TryPeek(out var first) && first.Sequence > box.Acknowledged + 1,
            box.Entries.Take(100).ToArray());
    }

    public bool Acknowledge(int forceId, long sequence)
    {
        if (!mailboxes.TryGetValue(forceId, out var box)) return sequence == 0;
        if (sequence < 0 || sequence > box.Last) return false;
        if (sequence <= box.Acknowledged) return true;
        box.Acknowledged = sequence;
        while (box.Entries.TryPeek(out var first) && first.Sequence <= sequence) box.Entries.Dequeue();
        return true;
    }

    public IReadOnlyList<Mailbox> Snapshot() => mailboxes.OrderBy(p => p.Key).Select(p =>
        new Mailbox(p.Key, p.Value.Last, p.Value.Acknowledged, p.Value.Entries.ToArray())).ToArray();
    public void Restore(IReadOnlyList<Mailbox> snapshot)
    {
        if (snapshot.Any(m => m is null || m.ForceId <= 0 || m.Entries is null || m.LastSequence < m.AcknowledgedSequence
            || m.AcknowledgedSequence < 0 || m.Entries.Count > CapacityPerForce
            || m.Entries.Any(e => e is null || e.Event is null || e.Event.RecipientForceId != m.ForceId
                || e.Sequence <= m.AcknowledgedSequence || e.Sequence > m.LastSequence)
            || !m.Entries.Select(e => e.Sequence).SequenceEqual(m.Entries.Select(e => e.Sequence).Distinct().Order()))
            || snapshot.Select(m => m.ForceId).Distinct().Count() != snapshot.Count)
            throw new InvalidOperationException("Invalid private mailbox snapshot");
        mailboxes.Clear();
        foreach (var box in snapshot) mailboxes.Add(box.ForceId, new State
        { Last = box.LastSequence, Acknowledged = box.AcknowledgedSequence, Entries = new(box.Entries) });
    }
}
