import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";
import type {
  MapHoverCellPayload,
  StrategyMapInteractionContext,
  StrategyMapInteractionStateSnapshot,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 同格多实体：先选对象，再打开对应指令菜单。 */
export class CellEntityPickerInteractionState extends StrategyMapInteractionState {
  readonly id = "cellEntityPicker";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "entityPicker" as const;

  override toSnapshot(ctx: StrategyMapInteractionContext): StrategyMapInteractionStateSnapshot {
    return {
      ...super.toSnapshot(ctx),
      cellEntityOptions: ctx.getCellEntityOptions(),
    };
  }

  override onPickCellEntity(
    ctx: StrategyMapInteractionContext,
    entity: MapCellEntityOption,
  ): void {
    NavigateInteractionState.openCellEntity(ctx, entity);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    NavigateInteractionState.resetToNavigate(ctx);
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }
}
