import type { MapHoverCellPayload, StrategyMapInteractionContext, StrategyMoveTarget } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { ExecutingCommandInteractionState } from "./ExecutingCommandInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 战前确认：展示胜率预览；确认后由父组件调用瞬间战 API。 */
export class BattleConfirmationInteractionState extends StrategyMapInteractionState {
  readonly id = "battleConfirm";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  /** 战前确认由屏幕居中对话框承担，地图 popup 关闭。 */
  readonly popupMode = "none" as const;

  override onConfirmBattle(ctx: StrategyMapInteractionContext): void {
    ctx.transitionTo(new ExecutingCommandInteractionState());
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    ctx.setLockedCommand(null);
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

  override onBattleFailed(ctx: StrategyMapInteractionContext, target: StrategyMoveTarget): void {
    ctx.setMoveTarget({ x: target.x, y: target.y });
    ctx.setSelectedCell({ x: target.x, y: target.y });
    ctx.transitionTo(new BattleConfirmationInteractionState());
  }

  override onHoverCell(_ctx: StrategyMapInteractionContext, _cell: MapHoverCellPayload): void {}
}
