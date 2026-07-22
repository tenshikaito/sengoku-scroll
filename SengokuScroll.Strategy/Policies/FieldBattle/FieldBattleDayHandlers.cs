using SengokuScroll.Strategy.Calculators;

namespace SengokuScroll.Strategy.Policies.FieldBattle;

/// <summary>野战日推进结果的处理策略。</summary>
public interface IFieldBattleDayHandler
{
    FieldBattleAutoResolver.FieldBattleDayKind Kind { get; }

    /// <summary>处理当日接敌结果；返回 true 表示外层循环应 continue。</summary>
    bool Handle(FieldBattleDayContext ctx);
}

public sealed class FieldBattleDayContext
{
    public required FieldBattleAutoResolver.FieldBattleDayResult DayResult { get; init; }
    public required bool BothOrdered { get; init; }
    public required IFieldBattleDayHost Host { get; init; }
}

public interface IFieldBattleDayHost
{
    void HandleStandoff(FieldBattleAutoResolver.FieldBattleDayResult dayResult);

    void HandleSurrender(FieldBattleAutoResolver.FieldBattleDayResult dayResult);

    void HandleDecisive(FieldBattleAutoResolver.FieldBattleDayResult dayResult, bool bothOrdered);
}

internal sealed class StandoffFieldBattleDayHandler : IFieldBattleDayHandler
{
    public static readonly StandoffFieldBattleDayHandler Instance = new();
    public FieldBattleAutoResolver.FieldBattleDayKind Kind => FieldBattleAutoResolver.FieldBattleDayKind.Standoff;

    public bool Handle(FieldBattleDayContext ctx)
    {
        ctx.Host.HandleStandoff(ctx.DayResult);
        return true;
    }
}

internal sealed class SurrenderFieldBattleDayHandler : IFieldBattleDayHandler
{
    public static readonly SurrenderFieldBattleDayHandler Instance = new();
    public FieldBattleAutoResolver.FieldBattleDayKind Kind => FieldBattleAutoResolver.FieldBattleDayKind.Surrender;

    public bool Handle(FieldBattleDayContext ctx)
    {
        ctx.Host.HandleSurrender(ctx.DayResult);
        return false;
    }
}

internal sealed class DecisiveFieldBattleDayHandler : IFieldBattleDayHandler
{
    public static readonly DecisiveFieldBattleDayHandler Instance = new();
    public FieldBattleAutoResolver.FieldBattleDayKind Kind => FieldBattleAutoResolver.FieldBattleDayKind.Decisive;

    public bool Handle(FieldBattleDayContext ctx)
    {
        ctx.Host.HandleDecisive(ctx.DayResult, ctx.BothOrdered);
        return false;
    }
}

public static class FieldBattleDayHandlerRegistry
{
    private static readonly Dictionary<FieldBattleAutoResolver.FieldBattleDayKind, IFieldBattleDayHandler> ByKind =
        new IFieldBattleDayHandler[]
        {
            StandoffFieldBattleDayHandler.Instance,
            SurrenderFieldBattleDayHandler.Instance,
            DecisiveFieldBattleDayHandler.Instance
        }.ToDictionary(h => h.Kind);

    public static bool Handle(FieldBattleDayContext ctx)
    {
        if (!ByKind.TryGetValue(ctx.DayResult.Kind, out var handler))
            return false;

        return handler.Handle(ctx);
    }
}
