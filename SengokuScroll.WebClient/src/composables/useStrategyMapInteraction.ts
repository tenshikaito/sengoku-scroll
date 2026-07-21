import { computed, onScopeDispose, ref, type Ref } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  MapSelectStrongholdPayload,
  MapSelectUnitPayload,
  MapSelectConvoyPayload,
  StrategyMenuAnchor,
  StrategyMoveTarget,
} from "@/strategyMapInteraction/types";
import {
  StrategyMapInteractionMachine,
  type StrategyMapInteractionStateSnapshot,
} from "@/strategyMapInteraction/StrategyMapInteractionMachine";
import { SplitSpawnSelectionInteractionState } from "@/strategyMapInteraction/states/SplitSpawnSelectionInteractionState";

export interface UseStrategyMapInteractionOptions {
  worldState: Ref<StrategyWorldState | null>;
  selectedUnitId: Ref<number | null>;
  selectedStrongholdId: Ref<number | null>;
  selectedConvoyId: Ref<number | null>;
  selectedCell: Ref<{ x: number; y: number } | null>;
  hoverCell: Ref<MapHoverCellPayload>;
  playerForceId?: number;
}

export function useStrategyMapInteraction(options: UseStrategyMapInteractionOptions) {
  const menuAnchor = ref<StrategyMenuAnchor | null>(null);
  const moveTarget = ref<{ x: number; y: number } | null>(null);
  const lockedCommand = ref<StrategyMoveTarget | null>(null);
  const pendingMergeTargetUnitId = ref<number | null>(null);
  const pendingSplitSubUnitIds = ref<number[]>([]);
  const snapshot = ref<StrategyMapInteractionStateSnapshot>({
    id: "navigate",
    mapUnitSelectionEnabled: true,
    mapStrongholdSelectionEnabled: true,
    mapConvoySelectionEnabled: true,
    mapCellSelectionEnabled: false,
    mapRightClickEnabled: false,
    popupMode: "none",
    menuAnchor: null,
    moveTarget: null,
  });

  const playerForceId = options.playerForceId ?? 1;

  let machine!: StrategyMapInteractionMachine;

  machine = new StrategyMapInteractionMachine({
    getSelectedUnitId: () => options.selectedUnitId.value,
    setSelectedUnitId: (id) => {
      options.selectedUnitId.value = id;
    },
    getSelectedStrongholdId: () => options.selectedStrongholdId.value,
    setSelectedStrongholdId: (id) => {
      options.selectedStrongholdId.value = id;
    },
    getSelectedConvoyId: () => options.selectedConvoyId.value,
    setSelectedConvoyId: (id) => {
      options.selectedConvoyId.value = id;
    },
    getSelectedCell: () => options.selectedCell.value,
    setSelectedCell: (cell) => {
      options.selectedCell.value = cell;
    },
    getMenuAnchor: () => menuAnchor.value,
    setMenuAnchor: (anchor) => {
      menuAnchor.value = anchor;
    },
    getMoveTarget: () => moveTarget.value,
    setMoveTarget: (cell) => {
      moveTarget.value = cell;
    },
    getLockedCommand: () => lockedCommand.value,
    setLockedCommand: (command) => {
      lockedCommand.value = command;
    },
    setHoverCell: (cell) => {
      options.hoverCell.value = cell;
    },
    resolveUnitLocation: (unitId) => {
      const world = options.worldState.value;
      if (!world) return null;
      const unit = world.units.find((u) => u.id === unitId);
      if (unit) return { x: unit.x, y: unit.y };
      const roster = world.ownUnitRoster?.find((u) => u.id === unitId);
      return roster ? { x: roster.x, y: roster.y } : null;
    },
    resolveStrongholdLocation: (strongholdId) => {
      const sh = options.worldState.value?.strongholds.find((s) => s.id === strongholdId);
      return sh ? { x: sh.x, y: sh.y } : null;
    },
    resolveStrongholdAtCell: (x, y) => {
      const sh = options.worldState.value?.strongholds.find((s) => s.x === x && s.y === y);
      return sh?.id ?? null;
    },
    resolveConvoyLocation: (convoyId) => {
      const convoy = options.worldState.value?.supplyConvoys.find((c) => c.id === convoyId);
      return convoy ? { x: convoy.x, y: convoy.y } : null;
    },
    isSelectableUnit: (unitId) => {
      const world = options.worldState.value;
      if (!world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (unit?.forceId === playerForceId) return true;
      const roster = world.ownUnitRoster?.find((u) => u.id === unitId);
      return roster?.forceId === playerForceId;
    },
    isPlayerUnit: (unitId) => {
      const world = options.worldState.value;
      if (!world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (unit?.forceId === playerForceId) return true;
      const roster = world.ownUnitRoster?.find((u) => u.id === unitId);
      return roster?.forceId === playerForceId;
    },
    isPlayerStronghold: (strongholdId) => {
      const sh = options.worldState.value?.strongholds.find((s) => s.id === strongholdId);
      return sh?.forceId === playerForceId;
    },
    isPlayerConvoy: (convoyId) => {
      const convoy = options.worldState.value?.supplyConvoys.find((c) => c.id === convoyId);
      return convoy?.forceId === playerForceId;
    },
    isValidMoveTarget: (x, y) => {
      const unitId = options.selectedUnitId.value;
      const world = options.worldState.value;
      if (!unitId || !world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (!unit || unit.forceId !== playerForceId) return false;
      return unit.x !== x || unit.y !== y;
    },
    isValidMovePathCell: (x, y) => {
      const unitId = options.selectedUnitId.value;
      const world = options.worldState.value;
      if (!unitId || !world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (!unit || unit.forceId !== playerForceId) return false;
      return unit.x !== x || unit.y !== y;
    },
    isValidAttackTarget: (x, y) => {
      const unitId = options.selectedUnitId.value;
      const world = options.worldState.value;
      if (!unitId || !world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (!unit || unit.forceId !== playerForceId) return false;
      const dx = Math.abs(unit.x - x);
      const dy = Math.abs(unit.y - y);
      if (dx + dy !== 1) return false;
      const targetUnit = world.units.find((u) => u.x === x && u.y === y);
      return targetUnit !== undefined && targetUnit.forceId !== playerForceId;
    },
    isValidMergeTarget: (targetUnitId) => {
      const sourceUnitId = options.selectedUnitId.value;
      const world = options.worldState.value;
      if (!sourceUnitId || !world || targetUnitId === sourceUnitId) return false;
      const source = world.units.find((u) => u.id === sourceUnitId);
      const target = world.units.find((u) => u.id === targetUnitId);
      if (!source || !target) return false;
      if (source.forceId !== playerForceId || target.forceId !== playerForceId) return false;
      if (source.soldiers <= 0 || target.soldiers <= 0) return false;
      const dx = Math.abs(source.x - target.x);
      const dy = Math.abs(source.y - target.y);
      return dx + dy <= 1;
    },
    isValidSplitSpawnCell: (x, y) => {
      const unitId = options.selectedUnitId.value;
      const world = options.worldState.value;
      if (!unitId || !world) return false;
      const unit = world.units.find((u) => u.id === unitId);
      if (!unit || unit.forceId !== playerForceId) return false;
      const dx = Math.abs(unit.x - x);
      const dy = Math.abs(unit.y - y);
      if (dx + dy !== 1) return false;
      return !world.units.some((u) => u.soldiers > 0 && u.x === x && u.y === y);
    },
    getPendingMergeTargetUnitId: () => pendingMergeTargetUnitId.value,
    setPendingMergeTargetUnitId: (id) => {
      pendingMergeTargetUnitId.value = id;
    },
    getPendingSplitSubUnitIds: () => pendingSplitSubUnitIds.value,
    setPendingSplitSubUnitIds: (ids) => {
      pendingSplitSubUnitIds.value = [...ids];
    },
    clearPendingSplitSubUnitIds: () => {
      pendingSplitSubUnitIds.value = [];
    },
    transitionTo: (state) => machine.transitionTo(state),
  });

  const unsubscribe = machine.subscribe((next) => {
    snapshot.value = next;
    menuAnchor.value = next.menuAnchor;
    moveTarget.value = next.moveTarget;
  });

  onScopeDispose(unsubscribe);

  const mapUnitSelectionEnabled = computed(() => snapshot.value.mapUnitSelectionEnabled);
  const mapStrongholdSelectionEnabled = computed(() => snapshot.value.mapStrongholdSelectionEnabled);
  const mapConvoySelectionEnabled = computed(() => snapshot.value.mapConvoySelectionEnabled);
  const mapCellSelectionEnabled = computed(() => snapshot.value.mapCellSelectionEnabled);
  const mapRightClickEnabled = computed(() => snapshot.value.mapRightClickEnabled);
  const popupMode = computed(() => snapshot.value.popupMode);
  const secondaryPopupMode = computed(() => snapshot.value.secondaryPopupMode ?? null);
  const stateId = computed(() => snapshot.value.id);

  return {
    stateId,
    menuAnchor,
    moveTarget,
    lockedCommand,
    pendingMergeTargetUnitId,
    pendingSplitSubUnitIds,
    snapshot,
    mapUnitSelectionEnabled,
    mapStrongholdSelectionEnabled,
    mapConvoySelectionEnabled,
    mapCellSelectionEnabled,
    mapRightClickEnabled,
    popupMode,
    secondaryPopupMode,
    reset: () => machine.reset(),
    onSelectUnit: (payload: MapSelectUnitPayload) => machine.onSelectUnit(payload),
    onSelectStronghold: (payload: MapSelectStrongholdPayload) => machine.onSelectStronghold(payload),
    onSelectConvoy: (payload: MapSelectConvoyPayload) => machine.onSelectConvoy(payload),
    onSelectCell: (payload: MapSelectCellPayload) => machine.onSelectCell(payload),
    onHoverCell: (cell: MapHoverCellPayload) => machine.onHoverCell(cell),
    onMapRightClick: () => machine.onMapRightClick(),
    onBeginMove: () => machine.onBeginMove(),
    onBeginAttack: () => machine.onBeginAttack(),
    onBeginMerge: () => machine.onBeginMerge(),
    onBeginSplit: () => machine.onBeginSplit(),
    onBeginExpedition: () => machine.onBeginExpedition(),
    enterSplitSpawnSelection: () =>
      machine.transitionTo(new SplitSpawnSelectionInteractionState()),
    onConfirmBattle: () => machine.onConfirmBattle(),
    onBattlePreviewReady: () => machine.onBattlePreviewReady(),
    onCancel: () => machine.onCancel(),
    onMoveSucceeded: () => machine.onMoveSucceeded(),
    onMoveFailed: (target: StrategyMoveTarget) => machine.onMoveFailed(target),
    onBattleSucceeded: () => machine.onBattleSucceeded(),
    onBattleFailed: (target: StrategyMoveTarget) => machine.onBattleFailed(target),
    enterExecutingCommand: () => machine.enterExecutingCommand(),
  };
}
