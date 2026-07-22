using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Vision;

/// <summary>按 <see cref="StrategyFogMode"/> 构造可见格计算策略。</summary>
public static class VisionPolicyFactory
{
    public static IVisionPolicy Create(StrategyFogMode mode)
        => FogModeBehaviorFactory.Create(mode).VisionPolicy;
}

/// <summary>按 <see cref="StrategyIntelMode"/> 构造单位 DTO 情报掩码策略（ForceIntel 已为 no-op，谍报见台账）。</summary>
public static class IntelPolicyFactory
{
    public static IIntelPolicy Create(StrategyIntelMode mode)
        => new IntelPolicyAdapter(IntelModeBehaviorFactory.Create(mode));
}

/// <summary>兼容旧 <see cref="IIntelPolicy"/> 调用方。</summary>
internal sealed class IntelPolicyAdapter(IIntelModeBehavior behavior) : IIntelPolicy
{
    public StrategyUnitStateDto ApplyUnitIntelMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells)
        => behavior.ApplyUnitDtoMask(
            unit,
            world,
            meta,
            playerForceId,
            visibleCells,
            espionageLedger: null,
            meta.StartOptions);
}
