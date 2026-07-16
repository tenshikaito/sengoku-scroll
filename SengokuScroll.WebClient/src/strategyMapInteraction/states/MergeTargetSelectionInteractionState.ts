import type {
  MapHoverCellPayload,
  MapSelectUnitPayload,
  StrategyMapInteractionContext,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 合并目标选择：点击同格或邻格友军部队。 */
export class MergeTargetSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "mergeTargetSelect";
  readonly mapUnitSelectionEnabled = true;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "mergeSelect" as const;

  override onSelectUnit(ctx: StrategyMapInteractionContext, payload: MapSelectUnitPayload): void {
    if (!ctx.isValidMergeTarget(payload.unitId)) return;
    ctx.setPendingMergeTargetUnitId(payload.unitId);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setLockedCommand(null);
    ctx.transitionTo(new UnitCommandInteractionState());
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }
}
