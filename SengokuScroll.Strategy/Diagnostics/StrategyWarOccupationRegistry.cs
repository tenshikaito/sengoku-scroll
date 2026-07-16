using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>战时据点占领登记（非持久化；供战史与将来和谈条款参考）。</summary>
public sealed class StrategyWarOccupationRegistry
{
    private readonly List<WarOccupationEntry> entries = [];

    public sealed record WarOccupationEntry(
        int StrongholdId,
        int OriginalForceId,
        int OccupierForceId,
        GameDate OccupiedDate);

    public void RecordOccupation(
        Stronghold stronghold,
        int originalForceId,
        int occupierForceId,
        GameDate occupiedDate)
    {
        entries.RemoveAll(e => e.StrongholdId == stronghold.Id);

        entries.Add(new WarOccupationEntry(
            stronghold.Id,
            originalForceId,
            occupierForceId,
            occupiedDate));
    }

    public IReadOnlyList<WarOccupationEntry> GetEntriesForStronghold(int strongholdId)
        => entries.Where(e => e.StrongholdId == strongholdId).ToList();

    public void RemoveEntry(int strongholdId)
        => entries.RemoveAll(e => e.StrongholdId == strongholdId);
}
