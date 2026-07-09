import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  StrategyMapInteractionContext,
  StrategyMoveTarget,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { BattleConfirmationInteractionState } from "./BattleConfirmationInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 攻击目标选择：点击相邻敌军格；右键 / 取消返回指令状态。 */
export class AttackTargetSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "attackTargetSelect";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = true;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "attackSelect" as const;

  override onSelectCell(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCellPayload
  ): StrategyMoveTarget | null {
    if (!ctx.isValidAttackTarget(payload.x, payload.y)) return null;

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

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
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

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
    if (cell && ctx.isValidAttackTarget(cell.x, cell.y)) {
      ctx.setMoveTarget({ x: cell.x, y: cell.y });
    } else {
      ctx.setMoveTarget(null);
    }
  }

  /** 战前预览成功后进入战前确认（居中对话框）。 */
  override onBattlePreviewReady(ctx: StrategyMapInteractionContext): void {
    ctx.transitionTo(new BattleConfirmationInteractionState());
  }
}
