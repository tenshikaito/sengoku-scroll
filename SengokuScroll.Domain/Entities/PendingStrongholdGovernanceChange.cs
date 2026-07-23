using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Entities;

/// <summary>信使待投递的据点政务方针变更。</summary>
public sealed class PendingStrongholdGovernanceChange
{
    public StrongholdGovernancePriority Priority { get; set; }
}
