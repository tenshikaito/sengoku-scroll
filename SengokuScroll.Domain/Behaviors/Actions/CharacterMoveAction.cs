using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Events;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Domain.Behaviors.Actions;

public class CharacterMoveAction(
    IGameContext context,
    CharacterMoveEvaluator moveEvaluator,
    MovementRules movementRules,
    IGameWorldEventDispatcher eventDispatcher)
{
    public void Update(Character o)
    {
        var routes = o.ActionTarget.RoutePoints;

        while (true)
        {
            if (o.ActionStatus != CharacterActionStatus.Moving)
                break;

            if (!routes.TryPeek(out var p))
                break;

            if (moveEvaluator.Evaluate(o, p))
            {
                var from = o.Location;

                MapLocationActions.SetCharacterLocation(context.GameWorldContext, o, p);
                routes.Dequeue();

                o.Ap -= movementRules.GetTileMovementApCost(o, p);

                eventDispatcher.Publish(new CharacterMovedEvent()
                {
                    CharacterId = o.Id,
                    CharacterName = o.Name,
                    From = from,
                    To = p
                });

#warning 如果抵达则修改状态为等待
            }
            else
            {
                break;
            }
        }
    }
}
