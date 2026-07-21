using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>按 <see cref="StrategyFogMode"/> 构造可见格计算策略。</summary>
public static class VisionPolicyFactory
{
    public static IVisionPolicy Create(StrategyFogMode mode)
        => mode switch
        {
            StrategyFogMode.None => new NoFogVisionPolicy(),
            StrategyFogMode.Force => new ForceVisionPolicy(),
            StrategyFogMode.Character => new CharacterVisionPolicy(),
            _ => new ForceVisionPolicy()
        };
}

/// <summary>按 <see cref="StrategyIntelMode"/> 构造单位 DTO 情报掩码策略（ForceIntel 已为 no-op，谍报见台账）。</summary>
public static class IntelPolicyFactory
{
    public static IIntelPolicy Create(StrategyIntelMode mode)
        => mode switch
        {
            StrategyIntelMode.Full => new FullIntelPolicy(),
            StrategyIntelMode.ForceIntel => new ForceIntelPolicy(),
            _ => new ForceIntelPolicy()
        };
}
