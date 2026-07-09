using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

public class DiplomacyRules(IGameContext context)
{
    public static GameResult HasTargetForce(Force? force)
    {
        if (force is null)
            return DiplomacyError.InvalidForce;

        return GameResult.Ok();
    }

    public GameResult IsEnemy(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsEnemy(sf, tf);
    }

    public static GameResult IsEnemy(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        if (source == target)
            return DiplomacyError.SelfForce;

        if (!source.IsEnemy(target))
            return DiplomacyError.NotEnemyForce;

        return GameResult.Ok();
    }

    public GameResult IsAlly(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsAlly(sf, tf);
    }

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

    public GameResult IsSelf(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsSelf(sf, tf);
    }

    public static GameResult IsSelf(Force? source, Force? target)
    {
        if (source is null || target is null)
            return DiplomacyError.InvalidForce;

        if (source.Id != target.Id)
            return DiplomacyError.NotSelfForce;

        return GameResult.Ok();
    }

    public GameResult IsTruce(IHasForce source, IHasForce target)
    {
        var (sf, tf) = GetForces(source, target);

        return IsTruce(sf, tf);
    }

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

    public (Force? sourceForce, Force? targetForce) GetForces(IHasForce source, IHasForce target)
    {
        var sf = context.GameWorldContext.GetForce(source);
        var tf = context.GameWorldContext.GetForce(target);

        return (sf, tf);
    }
}
