import type {
  MapHoverCellPayload,
  MapSelectUnitStrongholdPayload,
  StrategyMapInteractionContext,
  StrategyMapInteractionStateSnapshot,
  StrategyMapPopupMode,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { AttackTargetSelectionInteractionState } from "./AttackTargetSelectionInteractionState";
import { MergeTargetSelectionInteractionState } from "./MergeTargetSelectionInteractionState";
import { MoveTargetSelectionInteractionState } from "./MoveTargetSelectionInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 同格单位 + 据点：同时展示两套悬浮命令菜单。 */
export class UnitStrongholdCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "unitStrongholdCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;

  constructor(
    readonly unitPopupMode: Exclude<StrategyMapPopupMode, "none">,
    readonly strongholdPopupMode: Exclude<StrategyMapPopupMode, "none">
  ) {
    super();
  }

  get popupMode(): StrategyMapPopupMode {
    return this.unitPopupMode;
  }

  override toSnapshot(ctx: StrategyMapInteractionContext): StrategyMapInteractionStateSnapshot {
    return {
      ...super.toSnapshot(ctx),
      secondaryPopupMode: this.strongholdPopupMode,
    };
  }

  static openFromNavigate(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectUnitStrongholdPayload
  ): void {
    const location = ctx.resolveUnitLocation(payload.unitId);
    if (!location) return;

    ctx.setSelectedUnitId(payload.unitId);
    ctx.setSelectedStrongholdId(payload.strongholdId);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedCell(location);
    ctx.setMoveTarget(null);
    ctx.setMenuAnchor({
      x: location.x,
      y: location.y,
      screenX: payload.screenX,
      screenY: payload.screenY,
      panelAnchorRect: payload.panelAnchorRect,
      anchorSide: payload.anchorSide,
    });

    const unitMode: Exclude<StrategyMapPopupMode, "none"> = ctx.isPlayerUnit(payload.unitId)
      ? "command"
      : "foreignCommand";
    const strongholdMode: Exclude<StrategyMapPopupMode, "none"> = ctx.isPlayerStronghold(
      payload.strongholdId
    )
      ? "strongholdCommand"
      : "foreignStrongholdCommand";

    ctx.transitionTo(new UnitStrongholdCommandInteractionState(unitMode, strongholdMode));
  }

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
