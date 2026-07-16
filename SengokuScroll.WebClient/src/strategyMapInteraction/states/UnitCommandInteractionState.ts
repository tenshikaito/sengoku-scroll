import type { MapHoverCellPayload, StrategyMapInteractionContext } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { AttackTargetSelectionInteractionState } from "./AttackTargetSelectionInteractionState";
import { MergeTargetSelectionInteractionState } from "./MergeTargetSelectionInteractionState";
import { MoveTargetSelectionInteractionState } from "./MoveTargetSelectionInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 己方单位指令菜单：移动 / 攻击 / 情报等。 */
export class UnitCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "unitCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "command" as const;

  override onBeginMove(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    ctx.transitionTo(new MoveTargetSelectionInteractionState());
  }

  override onBeginAttack(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    ctx.transitionTo(new AttackTargetSelectionInteractionState());
  }

  override onBeginMerge(ctx: StrategyMapInteractionContext): void {
    ctx.setPendingMergeTargetUnitId(null);
    ctx.setMoveTarget(null);
    ctx.transitionTo(new MergeTargetSelectionInteractionState());
  }

  override onBeginSplit(ctx: StrategyMapInteractionContext): void {
    ctx.clearPendingSplitSubUnitIds();
    ctx.setMoveTarget(null);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedUnitId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.transitionTo(new NavigateInteractionState());
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }
}
