using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Evaluators;

public class UnitAttackEvaluator(
    IGameContext context,
    CommonRules commonRules,
    UnitRules unitRules,
    DiplomacyRules diplomacyRules)
    : EvaluatorBase
{
    public GameResult Evaluate(Unit unit, Point2 location)
    {
        // 检查边界
        GameResult CheckOutOfBounds()
            => commonRules.CheckOutOfBounds(location);

        // 检查攻击所需行动点
        GameResult CheckAttackAp()
            => unitRules.CheckAttackAp(unit);

        // 检查攻击范围
        GameResult CheckAttackRange()
            => UnitRules.CheckAttackRange(unit, location);

        // 检查攻击目标
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
                // 检查边界
                CheckOutOfBounds,
                // 检查攻击范围
                CheckAttackRange,
                // 检查攻击所需行动点
                CheckAttackAp,
                // 检查攻击目标
                CheckAttackTarget,
            ]);
    }
}
