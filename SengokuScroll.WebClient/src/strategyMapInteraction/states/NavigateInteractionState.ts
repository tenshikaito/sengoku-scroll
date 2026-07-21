import type {
  MapHoverCellPayload,
  MapSelectConvoyPayload,
  MapSelectStrongholdPayload,
  MapSelectUnitPayload,
  MapSelectUnitStrongholdPayload,
  StrategyMapInteractionContext,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { ConvoyCommandInteractionState } from "./ConvoyCommandInteractionState";
import { ForeignStrongholdCommandInteractionState } from "./ForeignStrongholdCommandInteractionState";
import { ForeignUnitCommandInteractionState } from "./ForeignUnitCommandInteractionState";
import { StrongholdCommandInteractionState } from "./StrongholdCommandInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";
import { UnitStrongholdCommandInteractionState } from "./UnitStrongholdCommandInteractionState";

/** 大地图浏览：点击单位/据点打开对应菜单。 */
export class NavigateInteractionState extends StrategyMapInteractionState {
  readonly id = "navigate";
  readonly mapUnitSelectionEnabled = true;
  readonly mapStrongholdSelectionEnabled = true;
  readonly mapConvoySelectionEnabled = true;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = false;
  readonly popupMode = "none" as const;

  override onSelectUnit(ctx: StrategyMapInteractionContext, payload: MapSelectUnitPayload): void {
    const location = ctx.resolveUnitLocation(payload.unitId);
    if (!location) return;

    const strongholdId = ctx.resolveStrongholdAtCell(location.x, location.y);
    if (strongholdId !== null) {
      UnitStrongholdCommandInteractionState.openFromNavigate(ctx, {
        ...payload,
        strongholdId,
      });
      return;
    }

    ctx.setSelectedUnitId(payload.unitId);
    ctx.setSelectedStrongholdId(null);
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

    if (ctx.isPlayerUnit(payload.unitId)) {
      ctx.transitionTo(new UnitCommandInteractionState());
    } else {
      ctx.transitionTo(new ForeignUnitCommandInteractionState());
    }
  }

  override onSelectUnitAndStronghold(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectUnitStrongholdPayload
  ): void {
    UnitStrongholdCommandInteractionState.openFromNavigate(ctx, payload);
  }

  override onSelectStronghold(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectStrongholdPayload
  ): void {
    const location = ctx.resolveStrongholdLocation(payload.strongholdId);
    if (!location) return;

    ctx.setSelectedUnitId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedStrongholdId(payload.strongholdId);
    ctx.setSelectedCell(location);
    ctx.setMoveTarget(null);
    ctx.setMenuAnchor({
      x: location.x,
      y: location.y,
      screenX: payload.screenX,
      screenY: payload.screenY,
    });

    if (ctx.isPlayerStronghold(payload.strongholdId)) {
      ctx.transitionTo(new StrongholdCommandInteractionState());
    } else {
      ctx.transitionTo(new ForeignStrongholdCommandInteractionState());
    }
  }

  override onSelectConvoy(ctx: StrategyMapInteractionContext, payload: MapSelectConvoyPayload): void {
    if (!ctx.isPlayerConvoy(payload.convoyId)) return;

    const location = ctx.resolveConvoyLocation(payload.convoyId);
    if (!location) return;

    ctx.setSelectedUnitId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedConvoyId(payload.convoyId);
    ctx.setSelectedCell(location);
    ctx.setMoveTarget(null);
    ctx.setMenuAnchor({
      x: location.x,
      y: location.y,
      screenX: payload.screenX,
      screenY: payload.screenY,
    });
    ctx.transitionTo(new ConvoyCommandInteractionState());
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }

  override onReset(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedUnitId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.setLockedCommand(null);
    ctx.setHoverCell(null);
  }
}
