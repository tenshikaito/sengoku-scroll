import type {
  MapHoverCellPayload,
  MapSelectStrongholdPayload,
  StrategyMapInteractionContext,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/**
 * 外交目标势力点选：仅可选据点；右上角提示；选中后由 Context 回调校验并恢复对话框。
 */
export class DiplomacyForceSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "diplomacyForceSelect";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = true;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "diplomacyForceSelect" as const;

  override onSelectStronghold(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectStrongholdPayload
  ): void {
    ctx.setSelectedStrongholdId(payload.strongholdId);
    const loc = ctx.resolveStrongholdLocation(payload.strongholdId);
    if (loc) ctx.setSelectedCell(loc);

    const accepted = ctx.onDiplomacyForceStrongholdPicked?.(payload.strongholdId) ?? false;
    if (!accepted) return;

    ctx.setMenuAnchor(null);
    ctx.transitionTo(new NavigateInteractionState());
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.onDiplomacyForcePickCancelled?.();
    ctx.setMenuAnchor(null);
    ctx.setSelectedStrongholdId(null);
    ctx.transitionTo(new NavigateInteractionState());
  }
}
