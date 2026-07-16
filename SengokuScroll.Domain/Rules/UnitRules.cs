using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

/// <summary>军事单位行动规则：攻击行动力、攻击距离等。</summary>
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

    /// <summary>校验单位是否具备发动攻击所需行动力。</summary>
    public GameResult CheckAttackAp(Unit unit)
    {
        if (unit.Ap < context.GameRuleConfig.AttackAp)
            return ApNotEnough;

        return GameResult.Ok();
    }

    /// <summary>校验攻击目标是否与单位相邻（近战一格攻击前提）。</summary>
    public static GameResult CheckAttackRange(Unit unit, Point2 p)
    {
        if (!unit.IsAdjacent(p))
            return TargetLocationNotAdjacent;

        return GameResult.Ok();
    }
}
