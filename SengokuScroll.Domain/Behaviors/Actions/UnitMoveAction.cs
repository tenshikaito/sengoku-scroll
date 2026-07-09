using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Diagnostics;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Events;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Domain.Behaviors.Actions;

public class UnitMoveAction(
    IGameContext context,
    UnitMoveEvaluator moveEvaluator,
    MovementRules movementRules,
    IGameWorldEventDispatcher eventDispatcher,
    IUnitMoveObserver moveObserver)
{
    public void Update(Unit o)
    {
        var routes = o.ActionTarget.RoutePoints;

        if (o.Status != UnitStatus.Moving)
        {
            moveObserver.OnMoveSkipped(o, "status_not_moving");
            return;
        }

        while (true)
        {
            if (o.Status != UnitStatus.Moving)
                break;

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

                eventDispatcher.Publish(new UnitMovedEvent()
                {
                    Id = o.Id,
                    Name = o.Name,
                    From = from,
                    To = p
                });

                moveObserver.OnMoveStepCompleted(o, from, p, o.Ap, routes.Count);

#warning 如果抵达则修改状态为等待
            }
            else
            {
                moveObserver.OnMoveSkipped(o, $"eval_failed:{eval.Error?.Code}");
                break;
            }
        }
    }
}
