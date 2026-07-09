import type { MapHoverCellPayload, StrategyMapInteractionContext } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 己方据点：情报与取消。 */
export class StrongholdCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "strongholdCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "strongholdCommand" as const;

  override onCancel(ctx: StrategyMapInteractionContext): void {
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
