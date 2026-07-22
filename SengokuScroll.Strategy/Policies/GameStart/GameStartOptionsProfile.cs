using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>
/// 本局开局选项的统一策略入口：各维度行为由 class 实现，避免散落 if/switch 遗漏分支。
/// </summary>
public sealed class GameStartOptionsProfile
{
    private GameStartOptionsProfile(
        GameStartOptions options,
        StrategyDifficulty? difficulty,
        IFogModeBehavior fog,
        IIntelModeBehavior intel,
        IControlModeBehavior control,
        IInstantEventBehavior instantEvents)
    {
        Options = options;
        Difficulty = difficulty;
        Fog = fog;
        Intel = intel;
        Control = control;
        InstantEvents = instantEvents;
    }

    public GameStartOptions Options { get; }

    public StrategyDifficulty? Difficulty { get; }

    public IFogModeBehavior Fog { get; }

    public IIntelModeBehavior Intel { get; }

    public IControlModeBehavior Control { get; }

    public IInstantEventBehavior InstantEvents { get; }

    public static GameStartOptionsProfile Create(
        GameStartOptions options,
        StrategyDifficulty? difficulty = null)
    {
        var sanitized = ApplyAllConstraints(options);
        return new GameStartOptionsProfile(
            sanitized,
            difficulty,
            FogModeBehaviorFactory.Create(sanitized.FogMode),
            IntelModeBehaviorFactory.Create(sanitized.IntelMode),
            ControlModeBehaviorFactory.Create(sanitized.ControlMode),
            InstantEventBehaviorFactory.Create(difficulty ?? StrategyDifficulty.Normal, sanitized));
    }

    public static GameStartOptions ApplyAllConstraints(GameStartOptions options)
        => FogModeBehaviorFactory.Create(options.FogMode).ApplyConstraints(options);
}
