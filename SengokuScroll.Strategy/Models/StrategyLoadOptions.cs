using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Models;

/// <summary>加载剧本时的开局覆盖（UI 确认后传入）。</summary>
public sealed record StrategyLoadOptions
{
    /// <summary>覆盖剧本 JSON 难度；省略则沿用剧本默认。</summary>
    public StrategyDifficulty? Difficulty { get; init; }

    /// <summary>Custom 难度下的完整选项；非 Custom 时忽略。</summary>
    public GameStartOptions? CustomStartOptions { get; init; }

    /// <summary>全势力由 AI 控制（仿真/观战模式）。</summary>
    public bool AllForcesAiControlled { get; init; }
}

public static class GameStartOptionsMapper
{
    public static GameStartOptionsDto ToDto(GameStartOptions options)
        => new()
        {
            FogMode = options.FogMode.ToString(),
            IntelMode = options.IntelMode.ToString(),
            ControlMode = options.ControlMode.ToString(),
            AllySharedVision = options.AllySharedVision,
            CharacterSharedVision = options.CharacterSharedVision,
            ShowAllyIntel = options.ShowAllyIntel,
            InstantEventMessages = options.InstantEventMessages
        };

    public static GameStartOptions FromDto(GameStartOptionsDto dto)
    {
        var fog = Enum.TryParse<StrategyFogMode>(dto.FogMode, ignoreCase: true, out var fogMode)
            ? fogMode
            : StrategyFogMode.Force;

        var options = new GameStartOptions
        {
            FogMode = fog,
            IntelMode = Enum.TryParse<StrategyIntelMode>(dto.IntelMode, ignoreCase: true, out var intel)
                ? intel
                : StrategyIntelMode.ForceIntel,
            ControlMode = Enum.TryParse<StrategyControlMode>(dto.ControlMode, ignoreCase: true, out var control)
                ? control
                : StrategyControlMode.DirectiveOnly,
            AllySharedVision = dto.AllySharedVision,
            CharacterSharedVision = dto.CharacterSharedVision,
            ShowAllyIntel = dto.ShowAllyIntel,
            InstantEventMessages = dto.InstantEventMessages
        };

        return GameStartOptionsProfile.ApplyAllConstraints(options);
    }
}
