using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.GameStart;

internal sealed class EnabledInstantEventBehavior : IInstantEventBehavior
{
    public static readonly EnabledInstantEventBehavior Instance = new();

    public bool ShouldPushInstantSummary() => true;
}

internal sealed class DisabledInstantEventBehavior : IInstantEventBehavior
{
    public static readonly DisabledInstantEventBehavior Instance = new();

    public bool ShouldPushInstantSummary() => false;
}

public static class InstantEventBehaviorFactory
{
    public static IInstantEventBehavior Create(StrategyDifficulty difficulty, GameStartOptions options)
    {
        if (difficulty == StrategyDifficulty.Easy)
            return EnabledInstantEventBehavior.Instance;

        return options.InstantEventMessages
            ? EnabledInstantEventBehavior.Instance
            : DisabledInstantEventBehavior.Instance;
    }
}
