import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  StrategyMapInteractionContext,
  StrategyMoveTarget,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 分兵落点选择：点击邻格空位生成新部队。 */
export class SplitSpawnSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "splitSpawnSelect";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = true;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "splitSelect" as const;

  override onSelectCell(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCellPayload
  ): StrategyMoveTarget | null {
    if (!ctx.isValidSplitSpawnCell(payload.x, payload.y)) return null;

    const target: StrategyMoveTarget = {
      x: payload.x,
      y: payload.y,
      screenX: payload.screenX,
      screenY: payload.screenY,
    };
    ctx.setSelectedCell({ x: payload.x, y: payload.y });
    ctx.setMoveTarget({ x: payload.x, y: payload.y });
    ctx.setLockedCommand(target);
    return target;
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    ctx.setLockedCommand(null);
    ctx.clearPendingSplitSubUnitIds();
    const unitId = ctx.getSelectedUnitId();
    const anchor = ctx.getMenuAnchor();
    if (unitId && anchor) {
      const loc = ctx.resolveUnitLocation(unitId);
      if (loc) ctx.setSelectedCell(loc);
    }
    ctx.transitionTo(new UnitCommandInteractionState());
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
    if (cell && ctx.isValidSplitSpawnCell(cell.x, cell.y)) {
      ctx.setMoveTarget({ x: cell.x, y: cell.y });
    } else {
      ctx.setMoveTarget(null);
    }
  }
}
