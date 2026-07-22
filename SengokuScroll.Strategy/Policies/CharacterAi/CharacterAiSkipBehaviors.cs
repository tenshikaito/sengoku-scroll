using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Policies.CharacterAi;

public interface ICharacterAiSkipBehavior
{
    bool AppliesTo(Character character);

    string Reason { get; }
}

internal sealed class DeadCharacterAiSkipBehavior : ICharacterAiSkipBehavior
{
    public static readonly DeadCharacterAiSkipBehavior Instance = new();

    public bool AppliesTo(Character character) => character.IsDead;

    public string Reason => "Dead";
}

internal sealed class PrisonerCharacterAiSkipBehavior : ICharacterAiSkipBehavior
{
    public static readonly PrisonerCharacterAiSkipBehavior Instance = new();

    public bool AppliesTo(Character character) => character.ForceStatus == CharacterForceStatus.Prisoner;

    public string Reason => "Prisoner";
}

internal sealed class UnitActionCharacterAiSkipBehavior : ICharacterAiSkipBehavior
{
    public static readonly UnitActionCharacterAiSkipBehavior Instance = new();

    public bool AppliesTo(Character character) => character.ForceStatus == CharacterForceStatus.UnitAction;

    public string Reason => "UnitAction";
}

internal sealed class MovingCharacterAiSkipBehavior : ICharacterAiSkipBehavior
{
    public static readonly MovingCharacterAiSkipBehavior Instance = new();

    public bool AppliesTo(Character character) =>
        character.ActionStatus is CharacterActionStatus.Moving
            or CharacterActionStatus.Acting
            or CharacterActionStatus.Resting;

    public string Reason => "Busy";
}

internal sealed class UnitLocationCharacterAiSkipBehavior : ICharacterAiSkipBehavior
{
    public static readonly UnitLocationCharacterAiSkipBehavior Instance = new();

    public bool AppliesTo(Character character) => character.LocationType == CharacterLocationType.Unit;

    public string Reason => "InUnit";
}

public static class CharacterAiSkipBehaviorRegistry
{
    private static readonly ICharacterAiSkipBehavior[] Behaviors =
    [
        DeadCharacterAiSkipBehavior.Instance,
        PrisonerCharacterAiSkipBehavior.Instance,
        UnitActionCharacterAiSkipBehavior.Instance,
        MovingCharacterAiSkipBehavior.Instance,
        UnitLocationCharacterAiSkipBehavior.Instance,
    ];

    public static bool ShouldSkipDailyAi(Character character)
        => Behaviors.Any(b => b.AppliesTo(character));

    public static string? DescribeSkipReason(Character character)
        => Behaviors.FirstOrDefault(b => b.AppliesTo(character))?.Reason;
}
