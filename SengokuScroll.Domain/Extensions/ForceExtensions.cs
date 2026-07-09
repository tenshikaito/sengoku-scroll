using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Diplomacy;

namespace SengokuScroll.Domain.Extensions;

public static class ForceExtensions
{
    public static bool HasDiplomacy(this Force source, int targetId)
        => source.Diplomacies.Any(o => o.TargetForceId == targetId);

    public static bool IsDiplomacy(this Force source, int targetId, DiplomacyRelation status)
        => source.Diplomacies.Any(o => o.TargetForceId == targetId && o.Relation == status);

    public static bool IsEnemy(this Force source, int targetId) => source.IsDiplomacy(targetId, DiplomacyRelation.Enemy);

    public static bool IsAlly(this Force source, int targetId) => source.IsDiplomacy(targetId, DiplomacyRelation.Allied);

    public static bool IsTruce(this Force source, int targetId)
        => source.Diplomacies.Any(o => o.TargetForceId == targetId && o.IsTruce);
}
