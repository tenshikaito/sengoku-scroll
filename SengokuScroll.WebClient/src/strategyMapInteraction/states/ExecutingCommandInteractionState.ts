import type { MapHoverCellPayload, StrategyMapInteractionContext, StrategyMoveTarget } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { BattleConfirmationInteractionState } from "./BattleConfirmationInteractionState";
import { MoveTargetSelectionInteractionState } from "./MoveTargetSelectionInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 移动 / 瞬间战 API 执行中：忽略地图交互。 */
export class ExecutingCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "executingCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = false;
  readonly popupMode = "none" as const;

  override onMoveSucceeded(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedUnitId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.setLockedCommand(null);
    ctx.transitionTo(new NavigateInteractionState());
  }

  override onMoveFailed(ctx: StrategyMapInteractionContext, target: StrategyMoveTarget): void {
    ctx.setLockedCommand(null);
    ctx.setMoveTarget({ x: target.x, y: target.y });
    ctx.setSelectedCell({ x: target.x, y: target.y });
    ctx.transitionTo(new MoveTargetSelectionInteractionState());
  }

  override onBattleSucceeded(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedUnitId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.setLockedCommand(null);
    ctx.transitionTo(new NavigateInteractionState());
  }

  override onBattleFailed(ctx: StrategyMapInteractionContext, _target: StrategyMoveTarget): void {
    ctx.setLockedCommand(null);
    ctx.transitionTo(new BattleConfirmationInteractionState());
  }

  override onHoverCell(_ctx: StrategyMapInteractionContext, _cell: MapHoverCellPayload): void {}
}
