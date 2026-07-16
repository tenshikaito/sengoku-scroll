using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 战败重整：设置撤退方针与 AP 补给，不强制移动。
/// 实际撤离由次日 AI（<c>StrategyUnitAIRules.MarchRetreat</c>）或玩家指令完成。
/// </summary>
public static class BattleRetreatRules
{
    /// <summary>根据败军士气与主将能力计算战后额外 AP，用于次日撤离。</summary>
    public static int CalculateRetreatApBonus(Unit loser, Unit winner, Character? commander)
    {
        var bonus = BattleConstants.RetreatApBonusBase;

        // 业务：士气未崩（≥阈值）时多获 1 AP，便于有序后撤
        if (loser.Morale >= BattleConstants.RetreatMoraleBonusThreshold)
            bonus += 1;

        if (commander is not null)
        {
            // 业务：行动性高的主将战后重整更快
            if (commander.Personality.Action >= 65)
                bonus += 1;

            var loserPower = commander.Power + commander.Leadership;
            var winnerPower = DefaultCommanderStat(winner) * 2;
            // 业务：主将能力远逊于胜方时，溃败更散，需额外 AP 才能脱离
            if (loserPower + 10 < winnerPower)
                bonus += 2;
        }

        return bonus;
    }

    /// <summary>
    /// 败方当日重整：方针=Retreat、进入 Routing、清攻击令、补给 AP。
    /// 撤离优先沿入场方向，由 AI/移动执行；追击失败后再离场。
    /// </summary>
    public static void ApplyDefeatRetreat(
        Unit loser,
        Unit winner,
        Character? commander)
    {
        loser.Directive = UnitDirective.Retreat;
        loser.Stance = UnitStance.Normal;
        loser.ActionTarget.UnitId = 0;
        loser.ActionTarget.RoutePoints.Clear();

        // 业务：被包围败军解除包围态，进入溃逃
        if (loser.Status == UnitStatus.BeingSurround)
            loser.Status = UnitStatus.Routing;

        var bonus = CalculateRetreatApBonus(loser, winner, commander);
        loser.Ap = Math.Min(loser.Movement, loser.Ap + bonus);

        if (loser.Soldier > 0 && loser.Status != UnitStatus.Chaos && loser.Status != UnitStatus.Fearful)
        {
            loser.Status = UnitStatus.Routing;
            // 业务：溃逃日数约 1–3，统率高者偏短
            var days = 2;
            if (commander is not null)
                days = commander.Leadership >= 70 ? 1 : commander.Leadership <= 40 ? 3 : 2;
            loser.RoutingDaysRemaining = days;
        }
    }

    /// <summary>无将领数据时的默认能力值，用于与胜方主将对比。</summary>
    private static int DefaultCommanderStat(Unit unit) => unit.LeaderId > 0 ? 70 : 50;
}
