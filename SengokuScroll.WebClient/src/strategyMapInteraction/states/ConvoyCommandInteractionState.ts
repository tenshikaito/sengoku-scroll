import type { MapHoverCellPayload, StrategyMapInteractionContext } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 己方运输队指令菜单：移动由系统自动调度，改道须经信使（后续 API）。 */
export class ConvoyCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "convoyCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "convoyCommand" as const;

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedUnitId(null);
    ctx.setSelectedStrongholdId(null);
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
