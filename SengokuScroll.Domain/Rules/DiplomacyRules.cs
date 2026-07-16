using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

/// <summary>外交关系判定：敌我、同盟、停战、己方势力等，供移动/攻击/外交系统复用。</summary>
public class DiplomacyRules(IGameContext context)
{
    /// <summary>校验势力对象是否存在（非 null）。</summary>
    public static GameResult HasTargetForce(Force? force)
    {
        if (force is null)
            return DiplomacyError.InvalidForce;

        return GameResult.Ok();
    }

    /// <summary>判定双方所属势力是否处于敌对关系。</summary>
    public GameResult IsEnemy(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsEnemy(sf, tf);
    }

    /// <summary>判定两势力是否敌对（<see cref="Diplomacy.DiplomacyRelation.Enemy"/>）。</summary>
    public static GameResult IsEnemy(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        // 业务：同势力不可互为攻击/阻挡目标
        if (source == target)
            return DiplomacyError.SelfForce;

        if (!source.IsEnemy(target))
            return DiplomacyError.NotEnemyForce;

        return GameResult.Ok();
    }

    /// <summary>判定双方所属势力是否处于同盟关系。</summary>
    public GameResult IsAlly(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsAlly(sf, tf);
    }

    /// <summary>判定两势力是否同盟（<see cref="Diplomacy.DiplomacyRelation.Allied"/>）。</summary>
    public static GameResult IsAlly(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        if (source == target)
            return DiplomacyError.SelfForce;

        if (!source.IsAlly(target))
            return DiplomacyError.NotAllyForce;

        return GameResult.Ok();
    }

    /// <summary>判定双方是否属于同一势力。</summary>
    public GameResult IsSelf(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsSelf(sf, tf);
    }

    /// <summary>判定两势力是否为同一势力（按 Id 比较）。</summary>
    public static GameResult IsSelf(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        if (source.Id != target.Id)
            return DiplomacyError.NotSelfForce;

        return GameResult.Ok();
    }

    /// <summary>判定双方所属势力是否处于停战期。</summary>
    public GameResult IsTruce(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsTruce(sf, tf);
    }

    /// <summary>判定两势力是否停战中（<see cref="Diplomacy.IsTruce"/>）。</summary>
    public static GameResult IsTruce(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        if (source == target)
            return DiplomacyError.SelfForce;

        if (!source.IsTruce(target))
            return DiplomacyError.NotTruceForce;

        return GameResult.Ok();
    }

    /// <summary>从游戏世界解析双方实体对应的势力对象。</summary>
    public (Force? sourceForce, Force? targetForce) GetForces(IHasForce source, IHasForce target)
    {
        var sf = context.GameWorldContext.GetForce(source);
        var tf = context.GameWorldContext.GetForce(target);

        return (sf, tf);
    }
}
