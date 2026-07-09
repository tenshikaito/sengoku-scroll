import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  MapSelectUnitPayload,
  StrategyMapInteractionContext,
  StrategyMapInteractionStateSnapshot,
  StrategyMapPopupMode,
  StrategyMoveTarget,
} from "./types";

/**
 * 策略地图交互状态基类。
 * 根组件 / 状态机仅调用基类方法，由各状态子类决定行为。
 */
export abstract class StrategyMapInteractionState {
  abstract readonly id: string;

  abstract readonly mapUnitSelectionEnabled: boolean;

  abstract readonly mapStrongholdSelectionEnabled: boolean;

  abstract readonly mapConvoySelectionEnabled: boolean;

  abstract readonly mapCellSelectionEnabled: boolean;

  abstract readonly mapRightClickEnabled: boolean;

  abstract readonly popupMode: StrategyMapPopupMode;

  toSnapshot(ctx: StrategyMapInteractionContext): StrategyMapInteractionStateSnapshot {
    return {
      id: this.id,
      mapUnitSelectionEnabled: this.mapUnitSelectionEnabled,
      mapStrongholdSelectionEnabled: this.mapStrongholdSelectionEnabled,
      mapConvoySelectionEnabled: this.mapConvoySelectionEnabled,
      mapCellSelectionEnabled: this.mapCellSelectionEnabled,
      mapRightClickEnabled: this.mapRightClickEnabled,
      popupMode: this.popupMode,
      menuAnchor: ctx.getMenuAnchor(),
      moveTarget: ctx.getMoveTarget(),
    };
  }

  onSelectUnit(_ctx: StrategyMapInteractionContext, _payload: MapSelectUnitPayload): void {}

  onSelectStronghold(_ctx: StrategyMapInteractionContext, _payload: import("./types").MapSelectStrongholdPayload): void {}

  onSelectConvoy(_ctx: StrategyMapInteractionContext, _payload: import("./types").MapSelectConvoyPayload): void {}

  /** 选中格点；返回非 null 时表示应立刻执行移动 API。 */
  onSelectCell(_ctx: StrategyMapInteractionContext, _payload: MapSelectCellPayload): StrategyMoveTarget | null {
    return null;
  }

  onHoverCell(_ctx: StrategyMapInteractionContext, _cell: MapHoverCellPayload): void {}

  onMapRightClick(_ctx: StrategyMapInteractionContext): void {}

  /** 指令菜单：点击「移动」。 */
  onBeginMove(_ctx: StrategyMapInteractionContext): void {}

  /** 指令菜单：点击「攻击」。 */
  onBeginAttack(_ctx: StrategyMapInteractionContext): void {}

  /** 指令菜单：查看详细情报。 */
  onShowIntel(_ctx: StrategyMapInteractionContext): void {}

  /** 战前确认：执行瞬间战。 */
  onConfirmBattle(_ctx: StrategyMapInteractionContext): void {}

  /** 攻击目标选定且战前预览 API 成功。 */
  onBattlePreviewReady(_ctx: StrategyMapInteractionContext): void {}

  /** Popup / 菜单「取消」或等价退出。 */
  onCancel(_ctx: StrategyMapInteractionContext): void {}

  onMoveSucceeded(_ctx: StrategyMapInteractionContext): void {}

  onMoveFailed(_ctx: StrategyMapInteractionContext, _target: StrategyMoveTarget): void {}

  onBattleSucceeded(_ctx: StrategyMapInteractionContext): void {}

  onBattleFailed(_ctx: StrategyMapInteractionContext, _target: StrategyMoveTarget): void {}

  onReset(_ctx: StrategyMapInteractionContext): void {}
}
