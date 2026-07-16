using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Evaluators;

/// <summary>军事单位攻击合法性评估：边界、距离、行动力、敌对目标（单位或据点）。</summary>
public class UnitAttackEvaluator(
    IGameContext context,
    CommonRules commonRules,
    UnitRules unitRules,
    DiplomacyRules diplomacyRules)
    : EvaluatorBase
{
    /// <summary>评估单位能否攻击目标格（须为相邻敌方单位或敌方据点）。</summary>
    public GameResult Evaluate(Unit unit, Point2 location)
    {
        GameResult CheckOutOfBounds()
            => commonRules.CheckOutOfBounds(location);

        GameResult CheckAttackAp()
            => unitRules.CheckAttackAp(unit);

        GameResult CheckAttackRange()
            => UnitRules.CheckAttackRange(unit, location);

        GameResult CheckAttackTarget()
        {
            GameResult CheckAttackUnit()
            {
                var tu = context.GameWorldContext.GetUnitOrDefault(location);

                if (tu is null)
                    return UnitError.UnitNotFound;

                return diplomacyRules.IsEnemy(unit, tu);
            }

            GameResult CheckAttackStronghold()
            {
                var ts = context.GameWorldContext.GetStrongholdOrDefault(location);

                if (ts is null)
                    return StrongholdError.StrongholdNotFound;

                return diplomacyRules.IsEnemy(unit, ts);
            }

            // 业务：优先尝试攻击格内军事单位，无单位时再尝试攻城
            var r = CheckAttackUnit();

            if (r)
                return r;

            r = CheckAttackStronghold();

            if (r)
                return r;

            return UnitError.AttackTargetNotFound;
        }

        return Evaluate(
            [
                CheckOutOfBounds,
                CheckAttackRange,
                CheckAttackAp,
                CheckAttackTarget,
            ]);
    }
}
