namespace SengokuScroll.Strategy.Policies.GameStart;

/// <summary>即时事件摘要通道行为。</summary>
public interface IInstantEventBehavior
{
    bool ShouldPushInstantSummary();
}
