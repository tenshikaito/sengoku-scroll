import type { AnchorSide, PanelRect } from "@/utils/mapCellAnchor";

import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";

/** Popup 锚点（格点 + 屏幕坐标）。 */
export interface StrategyMenuAnchor {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
  /** 相对地图面板的锚定矩形（如部队列表项）；优先于格块定位。 */
  panelAnchorRect?: PanelRect;
  /** 与 panelAnchorRect 联用时固定弹出方向。 */
  anchorSide?: AnchorSide;
}

/** 移动目标格（含屏幕坐标，供 API 与失败恢复）。 */
export interface StrategyMoveTarget extends StrategyMenuAnchor {}

export interface MapSelectUnitPayload {
  unitId: number;
  screenX: number;
  screenY: number;
  panelAnchorRect?: PanelRect;
  anchorSide?: AnchorSide;
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

export interface MapSelectUnitStrongholdPayload extends MapSelectUnitPayload {
  strongholdId: number;
}

export interface MapSelectConvoyPayload {
  convoyId: number;
  screenX: number;
  screenY: number;
}

export interface MapSelectCharacterPayload {
  characterId: number;
  screenX: number;
  screenY: number;
}

export interface MapSelectCharacterStrongholdPayload extends MapSelectCharacterPayload {
  strongholdId: number;
}

export interface MapSelectCellEntitiesPayload {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}

export type MapHoverCellPayload = { x: number; y: number; screenX: number; screenY: number } | null;

export type StrategyMapPopupMode =
  | "none"
  | "entityPicker"
  | "command"
  | "foreignCommand"
  | "characterCommand"
  | "strongholdCommand"
  | "foreignStrongholdCommand"
  | "convoyCommand"
  | "moveSelect"
  | "attackSelect"
  | "mergeSelect"
  | "splitSelect"
  | "diplomacyForceSelect";

/** 状态类可读写的 UI 上下文；状态切换通过 transitionTo 完成。 */
export interface StrategyMapInteractionContext {
  getSelectedUnitId(): number | null;
  setSelectedUnitId(unitId: number | null): void;

  getSelectedStrongholdId(): number | null;
  setSelectedStrongholdId(strongholdId: number | null): void;

  getSelectedConvoyId(): number | null;
  setSelectedConvoyId(convoyId: number | null): void;

  getSelectedCharacterId(): number | null;
  setSelectedCharacterId(characterId: number | null): void;

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

  resolveCharacterLocation(characterId: number): { x: number; y: number } | null;

  isSelectableUnit(unitId: number): boolean;

  isValidMoveTarget(x: number, y: number): boolean;

  /** 移动路径选点：允许再次点击同一格确认终点。 */
  isValidMovePathCell(x: number, y: number): boolean;

  isValidAttackTarget(x: number, y: number): boolean;

  isValidMergeTarget(unitId: number): boolean;

  isValidSplitSpawnCell(x: number, y: number): boolean;

  getPendingMergeTargetUnitId(): number | null;

  setPendingMergeTargetUnitId(unitId: number | null): void;

  getPendingSplitSubUnitIds(): readonly number[];

  setPendingSplitSubUnitIds(subUnitIds: readonly number[]): void;

  clearPendingSplitSubUnitIds(): void;

  isPlayerUnit(unitId: number): boolean;

  isPlayerStronghold(strongholdId: number): boolean;

  isPlayerConvoy(convoyId: number): boolean;

  resolvePlayerLordCharacterId(): number | null;

  isPlayerCharacterAtCell(x: number, y: number): boolean;

  isLordInStrongholdAt(strongholdId: number): boolean;

  isValidCharacterMovePathCell(x: number, y: number): boolean;

  resolveConvoyLocation(convoyId: number): { x: number; y: number } | null;

  resolveStrongholdLocation(strongholdId: number): { x: number; y: number } | null;

  resolveStrongholdAtCell(x: number, y: number): number | null;

  getCellEntityOptions(): readonly MapCellEntityOption[];

  setCellEntityOptions(options: readonly MapCellEntityOption[]): void;

  /**
   * 外交地图选点：据点选中后做校验；返回 true 表示接受并退出选点态。
   * 未注入时视为拒绝。
   */
  onDiplomacyForceStrongholdPicked?(strongholdId: number): boolean;

  /** 外交地图选点取消（右键 / Esc）。 */
  onDiplomacyForcePickCancelled?(): void;

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
  /** 同格多实体选择列表。 */
  cellEntityOptions?: readonly MapCellEntityOption[];
  menuAnchor: StrategyMenuAnchor | null;
  moveTarget: { x: number; y: number } | null;
}

/** @deprecated 兼容旧名 */
export type StrategyCellCommand = StrategyMoveTarget;
