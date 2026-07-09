/** Popup 锚点（格点 + 屏幕坐标）。 */
export interface StrategyMenuAnchor {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}

/** 移动目标格（含屏幕坐标，供 API 与失败恢复）。 */
export interface StrategyMoveTarget extends StrategyMenuAnchor {}

export interface MapSelectUnitPayload {
  unitId: number;
  screenX: number;
  screenY: number;
}

export interface MapSelectCellPayload {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}

export interface MapSelectStrongholdPayload {
  strongholdId: number;
  screenX: number;
  screenY: number;
}

export interface MapSelectConvoyPayload {
  convoyId: number;
  screenX: number;
  screenY: number;
}

export type MapHoverCellPayload = { x: number; y: number; screenX: number; screenY: number } | null;

export type StrategyMapPopupMode =
  | "none"
  | "command"
  | "foreignCommand"
  | "strongholdCommand"
  | "foreignStrongholdCommand"
  | "convoyCommand"
  | "moveSelect"
  | "attackSelect";

/** 状态类可读写的 UI 上下文；状态切换通过 transitionTo 完成。 */
export interface StrategyMapInteractionContext {
  getSelectedUnitId(): number | null;
  setSelectedUnitId(unitId: number | null): void;

  getSelectedStrongholdId(): number | null;
  setSelectedStrongholdId(strongholdId: number | null): void;

  getSelectedConvoyId(): number | null;
  setSelectedConvoyId(convoyId: number | null): void;

  getSelectedCell(): { x: number; y: number } | null;
  setSelectedCell(cell: { x: number; y: number } | null): void;

  getMenuAnchor(): StrategyMenuAnchor | null;
  setMenuAnchor(anchor: StrategyMenuAnchor | null): void;

  getMoveTarget(): { x: number; y: number } | null;
  setMoveTarget(cell: { x: number; y: number } | null): void;

  getLockedCommand(): StrategyMoveTarget | null;
  setLockedCommand(command: StrategyMoveTarget | null): void;

  setHoverCell(cell: MapHoverCellPayload): void;

  resolveUnitLocation(unitId: number): { x: number; y: number } | null;

  isSelectableUnit(unitId: number): boolean;

  isValidMoveTarget(x: number, y: number): boolean;

  /** 移动路径选点：允许再次点击同一格确认终点。 */
  isValidMovePathCell(x: number, y: number): boolean;

  isValidAttackTarget(x: number, y: number): boolean;

  isPlayerUnit(unitId: number): boolean;

  isPlayerStronghold(strongholdId: number): boolean;

  isPlayerConvoy(convoyId: number): boolean;

  resolveConvoyLocation(convoyId: number): { x: number; y: number } | null;

  resolveStrongholdLocation(strongholdId: number): { x: number; y: number } | null;

  transitionTo(state: import("./StrategyMapInteractionState").StrategyMapInteractionState): void;
}

export interface StrategyMapInteractionStateSnapshot {
  id: string;
  mapUnitSelectionEnabled: boolean;
  mapStrongholdSelectionEnabled: boolean;
  mapConvoySelectionEnabled: boolean;
  mapCellSelectionEnabled: boolean;
  mapRightClickEnabled: boolean;
  popupMode: StrategyMapPopupMode;
  menuAnchor: StrategyMenuAnchor | null;
  moveTarget: { x: number; y: number } | null;
}

/** @deprecated 兼容旧名 */
export type StrategyCellCommand = StrategyMoveTarget;
