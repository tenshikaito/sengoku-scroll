using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Domain.Diagnostics;

/// <summary>单位逐步移动观察者（策略诊断 / 测试）。</summary>
public interface IUnitMoveObserver
{
    void OnMoveStepEvaluated(Unit unit, Point2 target, GameResult result);

    void OnMoveStepCompleted(Unit unit, Point2 from, Point2 to, int apRemaining, int routeRemaining);

    void OnMoveSkipped(Unit unit, string reason);
}

/// <summary>空实现，Domain 默认注册。</summary>
public sealed class NullUnitMoveObserver : IUnitMoveObserver
{
    public static NullUnitMoveObserver Instance { get; } = new();

    public void OnMoveStepEvaluated(Unit unit, Point2 target, GameResult result) { }

    public void OnMoveStepCompleted(Unit unit, Point2 from, Point2 to, int apRemaining, int routeRemaining) { }

    public void OnMoveSkipped(Unit unit, string reason) { }
}
