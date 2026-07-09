import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  StrategyMapInteractionContext,
  StrategyMoveTarget,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 移动目标选择：点击地图设目标；右键 / 取消返回指令状态。 */
export class MoveTargetSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "moveTargetSelect";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = true;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "moveSelect" as const;

  override onSelectCell(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCellPayload
  ): StrategyMoveTarget | null {
    if (!ctx.isValidMovePathCell(payload.x, payload.y)) return null;

    ctx.setSelectedCell({ x: payload.x, y: payload.y });
    ctx.setMoveTarget({ x: payload.x, y: payload.y });
    return null;
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    this.returnToCommand(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
    if (cell && ctx.isValidMovePathCell(cell.x, cell.y)) {
      ctx.setMoveTarget({ x: cell.x, y: cell.y });
    } else {
      ctx.setMoveTarget(null);
    }
  }

  private returnToCommand(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    const unitId = ctx.getSelectedUnitId();
    const anchor = ctx.getMenuAnchor();
    if (unitId && anchor) {
      const loc = ctx.resolveUnitLocation(unitId);
      if (loc) ctx.setSelectedCell(loc);
    }
    ctx.transitionTo(new UnitCommandInteractionState());
  }
}
