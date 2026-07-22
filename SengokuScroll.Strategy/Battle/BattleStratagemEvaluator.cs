using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Battle;

/// <summary>计略 / 迷惑状态对当次决战的修正 hook。</summary>
public static class BattleStratagemEvaluator
{
    /// <summary>计略迷惑状态快照：中计与粮道情报迷惑对胜率的影响。</summary>
    public readonly record struct StratagemSnapshot(
        bool AttackerDeceived,
        bool DefenderDeceived,
        bool AttackerSupplyDeceived,
        bool DefenderSupplyDeceived,
        int AttackerWinRateDelta,
        int DefenderWinRateDelta);

    /// <summary>检测双方是否中计迷惑或粮道情报被误导，计算胜率惩罚。</summary>
    public static StratagemSnapshot Evaluate(BattleEvaluationContext ctx)
    {
        var atkDeceived = IsUnitConfused(ctx.Attacker, ctx.GameData);
        var defDeceived = IsUnitConfused(ctx.Defender, ctx.GameData);
        var atkSupply = HasDeceivedInboundSupply(ctx.Attacker, ctx.GameData);
        var defSupply = HasDeceivedInboundSupply(ctx.Defender, ctx.GameData);

        var atkDelta = 0;
        var defDelta = 0;
        // 业务：中计迷惑 -10% 胜率；粮道情报迷惑 -6% 胜率
        if (atkDeceived) atkDelta -= 10;
        if (defDeceived) defDelta -= 10;
        if (atkSupply) atkDelta -= 6;
        if (defSupply) defDelta -= 6;

        return new StratagemSnapshot(atkDeceived, defDeceived, atkSupply, defSupply, atkDelta, defDelta);
    }

    /// <summary>将计略快照写入因素明细与胜率修正。</summary>
    public static void ApplyToBreakdown(StratagemSnapshot snapshot, BattleFactorBreakdown b)
    {
        b.AttackerWinRateDelta += snapshot.AttackerWinRateDelta;
        b.DefenderWinRateDelta += snapshot.DefenderWinRateDelta;

        if (snapshot.AttackerDeceived)
            b.Add("stratagem.confused", "中计迷惑", -10, detail: "攻方");
        if (snapshot.DefenderDeceived)
            b.Add("stratagem.confused", "中计迷惑", 0, -10, "守方");
        if (snapshot.AttackerSupplyDeceived)
            b.Add("stratagem.false_supply", "粮道情报迷惑", -6, detail: "攻方");
        if (snapshot.DefenderSupplyDeceived)
            b.Add("stratagem.false_supply", "粮道情报迷惑", 0, -6, "守方");
    }

    private static bool IsUnitConfused(Unit unit, GameData gameData)
        => gameData.MessageCarriers.Values.Any(m =>
            m.Payload.TargetUnitId == unit.Id
            && m.Payload.Type == MessagePayloadType.FalseIntelligence
            && m.Status == MessageCarrierStatus.Arrived);

    private static bool HasDeceivedInboundSupply(Unit unit, GameData gameData)
        => gameData.SupplyConvoys.Values.Any(c =>
            c.TargetUnitId == unit.Id
            && c is { Status: SupplyConvoyStatus.Deceived } or { IsDeceived: true }
            && !c.IsReturningToOrigin);
}
