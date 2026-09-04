using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Diagnostics;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Events;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Behaviors.Actions;

/// <summary>
/// 军事单位逐日沿路径移动：校验邻格/AP/占格，同步地图索引并发布 <see cref="UnitMovedEvent"/>。
/// 单日最多前进 <see cref="GameRuleConfig.MaxTilesMovedPerDay"/> 格（默认 2）。
/// </summary>
public class UnitMoveAction(
    IGameContext context,
    UnitMoveEvaluator moveEvaluator,
    MovementRules movementRules,
    IGameWorldEventDispatcher eventDispatcher,
    IUnitMoveObserver moveObserver)
{
    /// <summary>推进单个移动中单位，直至路径空、AP/日格数上限或校验失败。</summary>
    public void Update(Unit o)
    {
        var routes = o.ActionTarget.RoutePoints;

        if (o.Status != UnitStatus.Moving)
        {
            moveObserver.OnMoveSkipped(o, "status_not_moving");
            return;
        }

        var tilesMovedToday = 0;
        var maxTilesPerDay = Math.Max(1, context.GameRuleConfig.MaxTilesMovedPerDay);

        while (true)
        {
            if (o.Status != UnitStatus.Moving)
                break;

            if (tilesMovedToday >= maxTilesPerDay)
            {
                // 业务：与 CharacterMoveAction 一致，道路富余 AP 仍受单日格数上限约束
                moveObserver.OnMoveSkipped(o, "daily_tile_cap");
                break;
            }

            if (!routes.TryPeek(out var p))
            {
                moveObserver.OnMoveSkipped(o, "route_empty");
                break;
            }

            var eval = moveEvaluator.Evaluate(o, p);
            moveObserver.OnMoveStepEvaluated(o, p, eval);

            if (eval.IsSuccess)
            {
                var from = o.Location;

                MapLocationActions.SetUnitLocation(context.GameWorldContext, o, p);
                routes.Dequeue();

                o.Ap -= movementRules.GetTileMovementApCost(o, p);
                tilesMovedToday++;

                eventDispatcher.Publish(new UnitMovedEvent()
                {
                    Id = o.Id,
                    Name = o.Name,
                    From = from,
                    To = p
                });

                moveObserver.OnMoveStepCompleted(o, from, p, o.Ap, routes.Count);

                if (!routes.TryPeek(out _))
                {
                    o.Status = UnitStatus.Waiting;
                    break;
                }
            }
            else
            {
                moveObserver.OnMoveSkipped(o, $"eval_failed:{eval.Error?.Code}");
                break;
            }
        }
    }
}
