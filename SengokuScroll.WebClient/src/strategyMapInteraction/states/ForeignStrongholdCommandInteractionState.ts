import type { MapHoverCellPayload, StrategyMapInteractionContext } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 敌方/中立据点：情报与取消。 */
export class ForeignStrongholdCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "foreignStrongholdCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "foreignStrongholdCommand" as const;

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
