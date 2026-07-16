using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>将领与部队绑定。</summary>
public static class UnitCommanderHelper
{
    public static void AttachToUnit(Character commander, Unit unit)
    {
        commander.ForceId = unit.ForceId;
        commander.Location = unit.Location;
        commander.LocationType = CharacterLocationType.Unit;
        commander.ForceStatus = CharacterForceStatus.UnitAction;
    }

    public static bool IsAvailableForDeployment(Character commander, int strongholdId)
        => !commander.IsDead
           && commander.ForceStatus is CharacterForceStatus.Idle or CharacterForceStatus.Task
           && commander.LocationType == CharacterLocationType.Stronghold
           && commander.StrongholdId == strongholdId;
}
