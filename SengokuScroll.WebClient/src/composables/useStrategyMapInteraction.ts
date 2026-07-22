import { computed, onScopeDispose, ref, type Ref } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";
import type {
  MapHoverCellPayload,
  MapSelectCellEntitiesPayload,
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
import {
  isLordInStrongholdId,
  isLordOnMap,
  isPlayerRealmStronghold,
  isPlayerRealmUnit,
  playerLordMapCharacterAtCell,
  resolvePlayerLordCharacterId,
} from "@/utils/strategyPlayerCharacter";

export interface UseStrategyMapInteractionOptions {
  worldState: Ref<StrategyWorldState | null>;
  selectedUnitId: Ref<number | null>;
  selectedCharacterId: Ref<number | null>;
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
  const cellEntityOptions = ref<MapCellEntityOption[]>([]);
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
    getSelectedCharacterId: () => options.selectedCharacterId.value,
    setSelectedCharacterId: (id) => {
      options.selectedCharacterId.value = id;
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
    resolveCharacterLocation: (characterId) => {
      const world = options.worldState.value;
      if (!world) return null;
      const mapChar = world.mapCharacters?.find((c) => c.id === characterId);
      if (mapChar) return { x: mapChar.x, y: mapChar.y };
      if (world.lord.characterId === characterId) {
        return { x: world.lord.x, y: world.lord.y };
      }
      return null;
    },
    resolvePlayerLordCharacterId: () => {
      const world = options.worldState.value;
      return world ? resolvePlayerLordCharacterId(world) : null;
    },
    isLordInStrongholdAt: (strongholdId) => {
      const world = options.worldState.value;
      return world ? isLordInStrongholdId(world, strongholdId) : false;
    },
    isPlayerCharacterAtCell: (x, y) => {
      const world = options.worldState.value;
      if (!world || !isLordOnMap(world)) return false;
      return playerLordMapCharacterAtCell(world, x, y) != null;
    },
    isValidCharacterMovePathCell: (x, y) => {
      const characterId = options.selectedCharacterId.value;
      const world = options.worldState.value;
      if (!characterId || !world) return false;
      const mapChar = world.mapCharacters?.find((c) => c.id === characterId);
      const loc = mapChar
        ?? (world.lord.characterId === characterId
          ? { x: world.lord.x, y: world.lord.y }
          : null);
      if (!loc) return false;
      return loc.x !== x || loc.y !== y;
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
      return isPlayerRealmUnit(world, unitId);
    },
    isPlayerUnit: (unitId) => {
      const world = options.worldState.value;
      if (!world) return false;
      return isPlayerRealmUnit(world, unitId);
    },
    isPlayerStronghold: (strongholdId) => {
      const world = options.worldState.value;
      if (!world) return false;
      const sh = world.strongholds.find((s) => s.id === strongholdId);
      if (!sh) return false;
      return isPlayerRealmStronghold(world, sh);
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
    getCellEntityOptions: () => cellEntityOptions.value,
    setCellEntityOptions: (options) => {
      cellEntityOptions.value = [...options];
    },
    transitionTo: (state) => machine.transitionTo(state),
  });

  const unsubscribe = machine.subscribe((next) => {
    snapshot.value = next;
    menuAnchor.value = next.menuAnchor;
    moveTarget.value = next.moveTarget;
    cellEntityOptions.value = [...(next.cellEntityOptions ?? [])];
  });

  onScopeDispose(unsubscribe);

  const mapUnitSelectionEnabled = computed(() => snapshot.value.mapUnitSelectionEnabled);
  const mapStrongholdSelectionEnabled = computed(() => snapshot.value.mapStrongholdSelectionEnabled);
  const mapConvoySelectionEnabled = computed(() => snapshot.value.mapConvoySelectionEnabled);
  const mapCharacterSelectionEnabled = computed(() => snapshot.value.id === "navigate");
  const mapCellSelectionEnabled = computed(() => snapshot.value.mapCellSelectionEnabled);
  const mapRightClickEnabled = computed(() => snapshot.value.mapRightClickEnabled);
  const popupMode = computed(() => snapshot.value.popupMode);
  const cellEntityPickerOptions = computed(() => cellEntityOptions.value);
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
    mapCharacterSelectionEnabled,
    mapCellSelectionEnabled,
    mapRightClickEnabled,
    popupMode,
    cellEntityPickerOptions,
    reset: () => machine.reset(),
    onSelectUnit: (payload: MapSelectUnitPayload) => machine.onSelectUnit(payload),
    onSelectStronghold: (payload: MapSelectStrongholdPayload) => machine.onSelectStronghold(payload),
    onSelectConvoy: (payload: MapSelectConvoyPayload) => machine.onSelectConvoy(payload),
    onSelectCharacter: (payload: import("@/strategyMapInteraction/types").MapSelectCharacterPayload) =>
      machine.onSelectCharacter(payload),
    onSelectCellEntities: (payload: MapSelectCellEntitiesPayload, entities: readonly MapCellEntityOption[]) =>
      machine.onSelectCellEntities(payload, entities),
    onPickCellEntity: (entity: MapCellEntityOption) => machine.onPickCellEntity(entity),
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
