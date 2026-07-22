using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.GameStart;

public interface IDifficultyPresetTemplate
{
    StrategyDifficulty Difficulty { get; }

    GameStartOptions CreateOptions();
}

internal sealed class EasyDifficultyPreset : IDifficultyPresetTemplate
{
    public static readonly EasyDifficultyPreset Instance = new();
    public StrategyDifficulty Difficulty => StrategyDifficulty.Easy;

    public GameStartOptions CreateOptions()
        => new()
        {
            FogMode = StrategyFogMode.None,
            IntelMode = StrategyIntelMode.Full,
            ControlMode = StrategyControlMode.FullDirect,
            AllySharedVision = true,
            CharacterSharedVision = true,
            ShowAllyIntel = false,
            InstantEventMessages = true
        };
}

internal sealed class NormalDifficultyPreset : IDifficultyPresetTemplate
{
    public static readonly NormalDifficultyPreset Instance = new();
    public StrategyDifficulty Difficulty => StrategyDifficulty.Normal;

    public GameStartOptions CreateOptions()
        => new()
        {
            FogMode = StrategyFogMode.Force,
            IntelMode = StrategyIntelMode.ForceIntel,
            ControlMode = StrategyControlMode.DirectiveOnly,
            AllySharedVision = false,
            CharacterSharedVision = false,
            ShowAllyIntel = false,
            InstantEventMessages = false
        };
}

internal sealed class HardDifficultyPreset : IDifficultyPresetTemplate
{
    public static readonly HardDifficultyPreset Instance = new();
    public StrategyDifficulty Difficulty => StrategyDifficulty.Hard;

    public GameStartOptions CreateOptions()
        => new()
        {
            FogMode = StrategyFogMode.Character,
            IntelMode = StrategyIntelMode.ForceIntel,
            ControlMode = StrategyControlMode.DirectiveOnly,
            AllySharedVision = false,
            CharacterSharedVision = false,
            ShowAllyIntel = false,
            InstantEventMessages = false
        };
}

public static class DifficultyPresetRegistry
{
    private static readonly Dictionary<StrategyDifficulty, IDifficultyPresetTemplate> Presets =
        new IDifficultyPresetTemplate[]
        {
            EasyDifficultyPreset.Instance,
            NormalDifficultyPreset.Instance,
            HardDifficultyPreset.Instance
        }.ToDictionary(p => p.Difficulty);

    public static GameStartOptions Resolve(StrategyDifficulty difficulty, GameStartOptions? customOverride)
    {
        if (difficulty == StrategyDifficulty.Custom)
            return GameStartOptionsProfile.ApplyAllConstraints(
                customOverride ?? NormalDifficultyPreset.Instance.CreateOptions());

        if (Presets.TryGetValue(difficulty, out var preset))
            return preset.CreateOptions();

        return NormalDifficultyPreset.Instance.CreateOptions();
    }
}
