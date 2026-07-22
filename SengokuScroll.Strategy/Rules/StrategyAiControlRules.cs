using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>判定势力是否由 AI 接管军事决策（含全势力仿真模式）。</summary>
public static class StrategyAiControlRules
{
    /// <summary>
    /// 势力是否由 AI 控制：全势力 AI 模式下含玩家势力；否则仅非玩家势力。
    /// </summary>
    public static bool IsForceAiControlled(StrategyScenarioMeta meta, int forceId)
        => meta.AllForcesAiControlled || forceId != meta.PlayerForceId;
}
