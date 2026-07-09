using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

public class UnitRules(IGameContext context)
{
    //public GameResult CheckAttackAlly(Unit source, Unit target)
    //{
    //    var myForce = context.GameWorldContext.GetForce(source);
    //    var isLeaderForce = myForce.LeaderId == source.LeaderId;

    //    // 如果是盟友关系，并且如果不是领导者则不能攻击盟友
    //    if (myForce.IsAlly(target) && (!isLeaderForce || source.LeaderId != myForce.LeaderId))
    //        return DiplomacyError.AllyForce;

    //    return GameResult.Ok();
    //}

    public GameResult CheckAttackAp(Unit unit)
    {
        if (unit.Ap < context.GameRuleConfig.AttackAp)
            return ApNotEnough;

        return GameResult.Ok();
    }

    public static GameResult CheckAttackRange(Unit unit, Point2 p)
    {
        if (!unit.IsAdjacent(p))
            return TargetLocationNotAdjacent;

        return GameResult.Ok();
    }
}
