using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Models;

/// <summary>开局战争迷雾模式。</summary>
public enum StrategyFogMode : byte
{
    /// <summary>无迷雾：全图 visible + explored。</summary>
    None = 0,

    /// <summary>势力迷雾：本势力可控单位/角色/据点聚合视野；内藩共享开图。</summary>
    Force = 1,

    /// <summary>角色视野：仅玩家当主 Character 提供 sight；控制规则与势力模式相同。</summary>
    Character = 2
}

/// <summary>情报展示档位。</summary>
public enum StrategyIntelMode : byte
{
    /// <summary>全信息（调试/无迷雾）。</summary>
    Full = 0,

    /// <summary>非自势力兵数模糊、士气/训练分档；高度优势可见兵数首位。</summary>
    ForceIntel = 1
}

/// <summary>玩家微操范围。</summary>
public enum StrategyControlMode : byte
{
    /// <summary>默认：友军仅方针，不可直控（不含内藩）。</summary>
    DirectiveOnly = 0,

    /// <summary>全控：可直控友军（仍不含内藩）。</summary>
    FullDirect = 1
}

/// <summary>本局开局选项（难度预设或 Custom 快照）。</summary>
public sealed record GameStartOptions
{
    public required StrategyFogMode FogMode { get; init; }

    public required StrategyIntelMode IntelMode { get; init; }

    public required StrategyControlMode ControlMode { get; init; }

    /// <summary>同盟共享视野（势力模式有效）。</summary>
    public required bool AllySharedVision { get; init; }

    /// <summary>
    /// 势力迷雾下，非当主地图角色（AI avatar 等）是否作为视野源。
    /// 玩家当主角色在任何迷雾模式下恒提供视野，不受此项影响。
    /// </summary>
    public required bool CharacterSharedVision { get; init; }

    /// <summary>显示同盟情报：同盟势力及内藩可见具体数值；外藩仍须谍报（ForceIntel 有效）。</summary>
    public required bool ShowAllyIntel { get; init; }

    /// <summary>
    /// 即时事件摘要：当日在消息区显示摘要；信使/Message 权威通道照常。
    /// </summary>
    public required bool InstantEventMessages { get; init; }

    public static GameStartOptions ForDifficulty(StrategyDifficulty difficulty)
        => GameStartOptionsPresets.Resolve(difficulty, customOverride: null);
}

/// <summary>Easy / Normal / Hard 固定模板；Custom 由调用方传入 override。</summary>
public static class GameStartOptionsPresets
{
    public static GameStartOptions Resolve(StrategyDifficulty difficulty, GameStartOptions? customOverride)
        => Policies.GameStart.DifficultyPresetRegistry.Resolve(difficulty, customOverride);

    private static GameStartOptions Sanitize(GameStartOptions options)
        => GameStartOptionsProfile.ApplyAllConstraints(options);

    /// <summary>角色视野下强制关闭同盟共享视野与角色共享视野。</summary>
    public static GameStartOptions SanitizeCharacterFogOptions(GameStartOptions options)
        => Sanitize(options);
}
