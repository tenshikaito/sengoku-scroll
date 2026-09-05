using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>判定势力是否由 AI 接管军事决策（含全势力仿真模式）。</summary>
public static class StrategyAiControlRules
{
    /// <summary>
    /// 势力是否由 AI 控制：全势力 AI 模式下含玩家势力；联机房间排除全部真人势力；
    /// 单机模式仍仅排除 PlayerForceId。
    /// </summary>
    public static bool IsForceAiControlled(StrategyScenarioMeta meta, int forceId)
        => meta.AllForcesAiControlled
           || (meta.HasHumanControlConfiguration || meta.HumanControlledForceIds.Count > 0
               ? !meta.HumanControlledForceIds.Contains(forceId)
               : forceId != meta.PlayerForceId);
}
