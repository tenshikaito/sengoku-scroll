using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Domain.Systems;

/// <summary>AI 日推进占位接口；策略模式由 <see cref="SengokuScroll.Strategy.Systems.StrategyAISystem"/> 实装。</summary>
public interface IAISystem : IGameSystem
{
}

/// <summary>
/// Domain 层 AI 占位。大战略军事 AI 在 <see cref="SengokuScroll.Strategy.Systems.StrategyAISystem"/>（方针、接敌、寻路）。
/// </summary>
public class AISystem(IGameContext context) : IAISystem
{
    /// <summary>Domain 占位 Order；策略实装为 <see cref="SengokuScroll.Strategy.Systems.StrategyAISystem"/> Order=18。</summary>
    public int Order { get; } = 40;

    /// <inheritdoc />
    public void Update()
    {
        // 业务：Domain 默认无 AI；策略/RPG 子类覆写。
    }
}