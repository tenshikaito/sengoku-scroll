using SengokuScroll.Strategy.Battle;

namespace SengokuScroll.Strategy.Policies.Battle;

/// <summary>将领意图对目标评分的修正。</summary>
public interface ICommanderActionScoringModifier
{
    BattleCommanderActionKind Action { get; }

    int ModifyScore(
        int score,
        BattleFormationSlot enemySlot,
        bool enemyIsCommanderParent,
        BattleFormationSlot actorSlot,
        int slotDistance);
}

internal sealed class FlankCommanderScoringModifier : ICommanderActionScoringModifier
{
    public static readonly FlankCommanderScoringModifier Instance = new();
    public BattleCommanderActionKind Action => BattleCommanderActionKind.Flank;

    public int ModifyScore(int score, BattleFormationSlot enemySlot, bool enemyIsCommanderParent, BattleFormationSlot actorSlot, int slotDistance)
    {
        if (BattleFormationSlotRules.IsFlank(enemySlot) || enemySlot == BattleFormationSlot.Rear)
            score += 20;
        return score;
    }
}

internal sealed class AssaultCommanderScoringModifier : ICommanderActionScoringModifier
{
    public static readonly AssaultCommanderScoringModifier Instance = new();
    public BattleCommanderActionKind Action => BattleCommanderActionKind.Assault;

    public int ModifyScore(int score, BattleFormationSlot enemySlot, bool enemyIsCommanderParent, BattleFormationSlot actorSlot, int slotDistance)
    {
        if (enemySlot == BattleFormationSlot.Front || enemyIsCommanderParent)
            score += 16;
        return score;
    }
}

internal sealed class HoldCommanderScoringModifier : ICommanderActionScoringModifier
{
    public static readonly HoldCommanderScoringModifier Instance = new();
    public BattleCommanderActionKind Action => BattleCommanderActionKind.Hold;

    public int ModifyScore(int score, BattleFormationSlot enemySlot, bool enemyIsCommanderParent, BattleFormationSlot actorSlot, int slotDistance)
    {
        if (enemySlot == BattleFormationSlot.Front && slotDistance <= 1)
            score += 8;
        return score;
    }
}

internal sealed class WithdrawCommanderScoringModifier : ICommanderActionScoringModifier
{
    public static readonly WithdrawCommanderScoringModifier Instance = new();
    public BattleCommanderActionKind Action => BattleCommanderActionKind.Withdraw;

    public int ModifyScore(int score, BattleFormationSlot enemySlot, bool enemyIsCommanderParent, BattleFormationSlot actorSlot, int slotDistance)
        => score - 10;
}

public static class CommanderActionScoringRegistry
{
    private static readonly Dictionary<BattleCommanderActionKind, ICommanderActionScoringModifier> ByAction =
        new ICommanderActionScoringModifier[]
        {
            FlankCommanderScoringModifier.Instance,
            AssaultCommanderScoringModifier.Instance,
            HoldCommanderScoringModifier.Instance,
            WithdrawCommanderScoringModifier.Instance
        }.ToDictionary(m => m.Action);

    public static int Apply(
        BattleCommanderActionKind action,
        int score,
        BattleFormationSlot enemySlot,
        bool enemyIsCommanderParent,
        BattleFormationSlot actorSlot,
        int slotDistance)
    {
        if (!ByAction.TryGetValue(action, out var modifier))
            return score;

        return modifier.ModifyScore(score, enemySlot, enemyIsCommanderParent, actorSlot, slotDistance);
    }
}
