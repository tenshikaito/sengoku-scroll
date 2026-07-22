using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Events;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Domain.Behaviors.Actions;

/// <summary>
/// 地图上独立行动的角色（溃逃将领、NPC 等）逐日移动。
/// 与 <see cref="UnitMoveAction"/> 相同受 <see cref="GameRuleConfig.MaxTilesMovedPerDay"/> 限制，避免单日走完整条路径。
/// </summary>
public class CharacterMoveAction(
    IGameContext context,
    CharacterMoveEvaluator moveEvaluator,
    MovementRules movementRules,
    IGameWorldEventDispatcher eventDispatcher)
{
    public void Update(Character o)
    {
        var routes = o.ActionTarget.RoutePoints;

        if (o.ActionStatus != CharacterActionStatus.Moving)
            return;

        var tilesMovedToday = 0;
        // 业务：与军事单位一致，单日最多 2 格（默认），溃逃回城需多日可见
        var maxTilesPerDay = Math.Max(1, context.GameRuleConfig.MaxTilesMovedPerDay);

        while (true)
        {
            if (o.ActionStatus != CharacterActionStatus.Moving)
                break;

            if (tilesMovedToday >= maxTilesPerDay)
                break;

            if (!routes.TryPeek(out var p))
            {
                CompleteMovement(context, o);
                break;
            }

            if (moveEvaluator.Evaluate(o, p))
            {
                var from = o.Location;

                MapLocationActions.SetCharacterLocation(context.GameWorldContext, o, p);
                routes.Dequeue();

                o.Ap -= movementRules.GetTileMovementApCost(o, p);
                tilesMovedToday++;

                eventDispatcher.Publish(new CharacterMovedEvent()
                {
                    CharacterId = o.Id,
                    CharacterName = o.Name,
                    From = from,
                    To = p
                });

                if (!routes.TryPeek(out _))
                    CompleteMovement(context, o);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>路径耗尽或 AP 不足时：若在己方/目标据点格则入城，否则留在地图格等待。</summary>
    private static void CompleteMovement(IGameContext context, Character character)
    {
        character.ActionStatus = CharacterActionStatus.Waiting;

        if (character.LocationType != CharacterLocationType.Map)
            return;

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var targetStrongholdId = character.ActionTarget.StrongholdId;
        Stronghold? target = null;

        if (targetStrongholdId > 0)
            gameData.Strongholds.TryGetValue(targetStrongholdId, out target);

        // 业务：未指定居城时，踏入本势力据点格即视为回城
        target ??= gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == character.ForceId
            && s.Location.X == character.Location.X
            && s.Location.Y == character.Location.Y);

        if (target is null)
            return;

        character.ForceId = target.ForceId;
        character.StrongholdId = target.Id;
        character.Location = target.Location;
        character.LocationType = CharacterLocationType.Stronghold;
        character.LocationStrongholdId = target.Id;
    }
}
