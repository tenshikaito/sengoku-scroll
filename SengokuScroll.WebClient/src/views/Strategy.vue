<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { ElMessageBox } from "element-plus";
import {
  advanceDay,
  exportStrategySave,
  restoreStrategySave,
  hasLocalStrategySave,
  getMovementTrace,
  getStrategyState,
  loadScenario,
  orderUnitAttack,
  orderUnitSiege,
  mergeUnits,
  splitUnit,
  deployFromStronghold,
  moveUnit,
  previewBattle,
  previewUnitPath,
  setUnitDirective,
  setApiMode,
  strategyApiDiagnostics,
  type StrategyApiMode,
  type StrategyBattlePreview,
  type StrategyBattleResult,
  type StrategyEvent,
  type StrategyEconomySettlementDetail,
  type StrategyMovementTraceEntry,
  type StrategyWorldState,
  type MapPoint,
} from "@/api/strategy";
import StrategyMapCanvas from "@/components/strategy/StrategyMapCanvas.vue";
import StrategyIntelBar from "@/components/strategy/StrategyIntelBar.vue";
import StrategyMapPopup from "@/components/strategy/StrategyMapPopup.vue";
import StrategyEntityIntelDialog, {
  type EntityIntelTarget,
} from "@/components/strategy/StrategyEntityIntelDialog.vue";
import StrategyBattleConfirmDialog, {
  type BattleConfirmPayload,
} from "@/components/strategy/StrategyBattleConfirmDialog.vue";
import StrategyBattleResultDialog from "@/components/strategy/StrategyBattleResultDialog.vue";
import StrategyDirectiveDialog from "@/components/strategy/StrategyDirectiveDialog.vue";
import StrategySplitDialog from "@/components/strategy/StrategySplitDialog.vue";
import StrategyExpeditionDialog from "@/components/strategy/StrategyExpeditionDialog.vue";
import StrategyEventFeed from "@/components/strategy/StrategyEventFeed.vue";
import StrategyNotificationTray, {
  type StrategyPendingNotification,
} from "@/components/strategy/StrategyNotificationTray.vue";
import StrategyMessageFeedToolbar from "@/components/strategy/StrategyMessageFeedToolbar.vue";
import StrategyMessageDialog from "@/components/strategy/StrategyMessageDialog.vue";
import {
  filterEventsByMessageScope,
} from "@/utils/strategyMessageScope";
import StrategyEconomySettlementDialog from "@/components/strategy/StrategyEconomySettlementDialog.vue";
import StrategyIntelSystemDialog from "@/components/strategy/StrategyIntelSystemDialog.vue";
import StrategySystemMenuDialog from "@/components/strategy/StrategySystemMenuDialog.vue";
import StrategyForceCommandPopup from "@/components/strategy/StrategyForceCommandPopup.vue";
import StrategyCellIntelHover from "@/components/strategy/StrategyCellIntelHover.vue";
import StrategyMapViewControls from "@/components/strategy/StrategyMapViewControls.vue";
import type { MapRouteOverlay, MapMoveRelayMarker } from "@/components/strategy/mapRouteStyles";
import { getForceColorCss } from "@/components/strategy/forceColors";
import type { StrategyMapColorMode } from "@/utils/mapEntityColors";
import {
  countOwnCharacters,
  countOwnStrongholds,
  countRealmCharacters,
  countRealmStrongholds,
} from "@/utils/strategyRealmStats";
import { formatFoodKoku, formatMoneyKan, formatSoldiers } from "@/utils/strategyDisplayUnits";
import { useStrategyMapInteraction } from "@/composables/useStrategyMapInteraction";
import type { StrategyMoveTarget } from "@/strategyMapInteraction/types";
import {
  DEFAULT_ROUTE_VISIBILITY_POLICY,
  filterUnitsForRouteDisplay,
} from "@/strategyMapInteraction/routeVisibilityPolicy";
import { resolveAnchoredPanelPlacement, type AnchorSide, type AnchorVerticalAlign } from "@/utils/mapCellAnchor";
import { parseEconomySettlementFromEvent } from "@/utils/normalizeStrategyEvent";
import {
  findPointOnPath,
  truncateMovePathAtCell,
} from "@/utils/movePathPlanning";
import {
  clearMovePathDebug,
  formatPathPoints,
  logMovePath,
  movePathDebugEntries,
} from "@/utils/movePathDebug";
import type { UnitDirectiveValue } from "@/utils/unitDirective";
import { landmarkAtCell, mapTileInfo } from "@/utils/mapTileLookup";
import { logStrategyMapCoords, logHoverIntelLayoutDebug, rectToScreenDebug } from "@/utils/strategyMapDebug";
import { attackApBlockReason, parseApiErrorCode, siegeApBlockReason } from "@/utils/strategyActionRules";
import {
  notificationFromEvent,
  strategicReportDetailText,
} from "@/utils/strategyNotifications";
import { messageCategoryLabel } from "@/utils/messageCategories";

const HOVER_INTEL_W = 280;
const HOVER_INTEL_H = 360;
const HOVER_INTEL_DUAL_GAP = 8;
const MENU_POPUP_W = 200;
const MENU_POPUP_H = 180;

const state = ref<StrategyWorldState | null>(null);
const loading = ref(false);
const error = ref("");
const info = ref("");
const selectedUnitId = ref<number | null>(null);
const selectedStrongholdId = ref<number | null>(null);
const selectedConvoyId = ref<number | null>(null);
const selectedCell = ref<{ x: number; y: number } | null>(null);
const hoverCell = ref<{ x: number; y: number; screenX: number; screenY: number } | null>(null);
const mapPanelRef = ref<HTMLElement | null>(null);
const mapCanvasRef = ref<InstanceType<typeof StrategyMapCanvas> | null>(null);
const menuPopupRef = ref<InstanceType<typeof StrategyMapPopup> | null>(null);
const cornerPopupRef = ref<InstanceType<typeof StrategyMapPopup> | null>(null);
const hoverIntelLayerRef = ref<HTMLElement | null>(null);
const intelPinnedCell = ref<{ x: number; y: number } | null>(null);
const intelLayerHovered = ref(false);
const movementTrace = ref<StrategyMovementTraceEntry[]>([]);
const moveCommittedWaypoints = ref<MapPoint[]>([]);
const movePendingRelay = ref<MapPoint | null>(null);

const mapInteraction = useStrategyMapInteraction({
  worldState: state,
  selectedUnitId,
  selectedStrongholdId,
  selectedConvoyId,
  selectedCell,
  hoverCell,
});

const {
  menuAnchor,
  lockedCommand,
  stateId,
  mapUnitSelectionEnabled,
  mapStrongholdSelectionEnabled,
  mapConvoySelectionEnabled,
  mapCellSelectionEnabled,
  mapRightClickEnabled,
  popupMode,
  onSelectUnit,
  onSelectStronghold,
  onSelectConvoy,
  onSelectCell,
  onHoverCell,
  onMapRightClick,
  onBeginMove,
  onBeginAttack,
  onBeginMerge,
  enterSplitSpawnSelection,
  pendingMergeTargetUnitId,
  pendingSplitSubUnitIds,
  onConfirmBattle,
  onBattlePreviewReady,
  onCancel,
  onMoveSucceeded,
  onMoveFailed,
  onBattleSucceeded,
  onBattleFailed,
  enterExecutingCommand,
} = mapInteraction;

const previewRoutePoints = ref<MapPoint[]>([]);
const battlePreview = ref<StrategyBattlePreview | null>(null);
const battleConfirmVisible = ref(false);
const battleResult = ref<StrategyBattleResult | null>(null);
const battleResultVisible = ref(false);
const eventFeed = ref<StrategyEvent[]>([]);
const pendingNotifications = ref<StrategyPendingNotification[]>([]);
const settlementDialogVisible = ref(false);
const settlementDetail = ref<StrategyEconomySettlementDetail | null>(null);
const eventDetailVisible = ref(false);
const eventDetailTitle = ref("");
const eventDetailText = ref("");
const hoverIntelAnchorSide = ref<AnchorSide>("right");
const hoverIntelVerticalAlign = ref<AnchorVerticalAlign>("start");
const messageDialogVisible = ref(false);
const showPlayerMessages = ref(true);
const showWorldMessages = ref(true);
const directiveDialogVisible = ref(false);
const splitDialogVisible = ref(false);
const expeditionDialogVisible = ref(false);
const pendingSplitUnitName = ref<string | undefined>(undefined);
const intelSystemVisible = ref(false);
const intelSystemInitialTab = ref("force");
const systemMenuVisible = ref(false);
const forceCommandVisible = ref(false);
const forceStatusRef = ref<HTMLElement | null>(null);
const forcePopupRef = ref<InstanceType<typeof StrategyForceCommandPopup> | null>(null);
const intelDialogVisible = ref(false);
const intelDialogTarget = ref<EntityIntelTarget | null>(null);
const hoverIntelStyle = ref<
  { display: "none" } | { position: "fixed"; left: string; top: string; zIndex: string }
>({ display: "none" });
let previewRequestSerial = 0;

const apiMode = computed(() => strategyApiDiagnostics.mode);
const lastRequest = computed(() => strategyApiDiagnostics.last);
const usingMockFallback = computed(() => strategyApiDiagnostics.usingMockFallback);

const dateText = computed(() => {
  if (!state.value) return " —";
  const d = state.value.date;
  const month = String(d.month).padStart(2, " ");
  const day = String(d.day).padStart(2, " ");
  return ` ${d.year}年${month}月${day}日`;
});

const selectedUnit = computed(
  () => state.value?.units.find((u) => u.id === selectedUnitId.value) ?? null
);

const selectedStronghold = computed(
  () => state.value?.strongholds.find((s) => s.id === selectedStrongholdId.value) ?? null
);

const popupStronghold = computed(() => {
  if (!menuAnchor.value || !state.value) return null;
  return (
    state.value.strongholds.find(
      (s) => s.x === menuAnchor.value!.x && s.y === menuAnchor.value!.y
    ) ?? null
  );
});

const canSiegePopupStronghold = computed(() => {
  const unit = selectedUnit.value;
  const sh = popupStronghold.value;
  const playerForceId = state.value?.playerForceId;
  if (!unit || !sh || playerForceId == null) return false;
  if (sh.forceId === playerForceId) return false;
  const dist = Math.abs(unit.x - sh.x) + Math.abs(unit.y - sh.y);
  return dist <= 1;
});

const canExpeditionStronghold = computed(() => {
  const sh = selectedStronghold.value;
  const playerForceId = state.value?.playerForceId;
  if (!sh || playerForceId == null) return false;
  if (sh.forceId !== playerForceId || !sh.isLordResidence) return false;
  return !state.value?.units.some((u) => u.soldiers > 0 && u.x === sh.x && u.y === sh.y);
});

const selectedConvoy = computed(
  () => state.value?.supplyConvoys.find((c) => c.id === selectedConvoyId.value) ?? null
);

const selectedEntityName = computed(
  () =>
    selectedUnit.value?.name ??
    selectedStronghold.value?.name ??
    (selectedConvoy.value ? `运输队 #${selectedConvoy.value.id}` : undefined)
);

const popupEntityName = computed(() => selectedEntityName.value);

const playerForce = computed(
  () =>
    state.value?.forces.find((f) => f.id === state.value!.playerForceId) ?? null
);

const lordResidenceName = computed(() => {
  if (!state.value) return null;
  const fromLord = state.value.lord.residenceStrongholdName?.trim();
  if (fromLord) return fromLord;
  const atLord = state.value.strongholds.find(
    (s) =>
      s.forceId === state.value!.playerForceId &&
      s.x === state.value!.lord.x &&
      s.y === state.value!.lord.y
  );
  return atLord?.name ?? null;
});

const playerLordName = computed(() => state.value?.lord.name?.trim() ?? null);

const playerForceStats = computed(() => {
  const force = playerForce.value;
  if (!force || !state.value) return null;
  const { forces, strongholds, units, characters, lord } = state.value;
  const lordName = lord.name;
  return {
    strongholdCount: countRealmStrongholds(force.id, forces, strongholds),
    ownStrongholdCount: countOwnStrongholds(force.id, strongholds),
    characterCount: countRealmCharacters(force.id, forces, strongholds, units, {
      characters,
      forceCharacterCount: force.characterCount,
      lordName,
    }),
    ownCharacterCount: countOwnCharacters(force.id, strongholds, units, {
      characters,
      lordName,
    }),
    prestige: force.prestige ?? 0,
    orthodoxy: force.orthodoxy ?? 0,
  };
});

/** 地图右下角：势力 / 封地 / 外交 着色模式。 */
const mapColorMode = ref<StrategyMapColorMode>("Realm");

/** 默认战略（暂停）；进行 = 自动推进（后续实装）。 */
const gamePaused = ref(true);

/** 倍速占位；后续实装自动推进间隔。 */
const gameSpeed = ref<1 | 2 | 4>(1);

/** M4 可改为从难度/设置读取；M3 起可接入同盟势力列表。 */
const routeVisibilityContext = computed(() => ({
  policy: DEFAULT_ROUTE_VISIBILITY_POLICY,
  playerForceId: playerForce.value?.id ?? 1,
  allyForceIds: [] as readonly number[],
}));

function cellEntities<T extends { x: number; y: number }>(items: T[], x: number, y: number) {
  return items.filter((item) => item.x === x && item.y === y);
}

function cellEntity<T extends { x: number; y: number }>(items: T[], x: number, y: number) {
  return cellEntities(items, x, y)[0] ?? null;
}

/** 底栏情报：跟随鼠标悬停格；栏位始终显示。 */
const intelBarX = computed(() => hoverCell.value?.x ?? null);
const intelBarY = computed(() => hoverCell.value?.y ?? null);

/** 悬浮框等仍用悬停 > 固定 > 选中格。 */
const intelBarCell = computed(() => {
  if (hoverCell.value) return { x: hoverCell.value.x, y: hoverCell.value.y };
  if (intelPinnedCell.value) return intelPinnedCell.value;
  if (selectedCell.value) return selectedCell.value;
  return null;
});

const intelX = computed(() => intelBarCell.value?.x ?? null);
const intelY = computed(() => intelBarCell.value?.y ?? null);

const intelStronghold = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? cellEntity(state.value.strongholds, intelX.value, intelY.value)
    : null
);

const intelUnit = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? cellEntity(state.value.units, intelX.value, intelY.value)
    : null
);

function battlefieldAt(x: number, y: number) {
  return state.value?.battlefields?.find((b) => b.x === x && b.y === y) ?? null;
}

function fieldBattlefieldAt(x: number, y: number) {
  const bf = battlefieldAt(x, y);
  return bf?.kind === "Field" ? bf : null;
}

function entityCountAt(x: number, y: number): number {
  if (!state.value) return 0;
  let count =
    cellEntities(state.value.strongholds, x, y).length +
    cellEntities(state.value.supplyConvoys, x, y).length +
    cellEntities(state.value.messengers, x, y).length;
  if (battlefieldAt(x, y)) {
    count += 1;
  } else {
    count += cellEntities(state.value.units, x, y).length;
  }
  return count;
}

const pinnedCellEntityCount = computed(() => {
  if (!intelPinnedCell.value) return 0;
  return entityCountAt(intelPinnedCell.value.x, intelPinnedCell.value.y);
});

const hoverUnit = computed(() => intelUnit.value);

const hoverStronghold = computed(() =>
  hoverUnit.value ? null : intelStronghold.value
);

const hoverConvoy = computed(() => {
  if (!state.value || intelX.value === null || intelY.value === null) return null;
  return cellEntity(state.value.supplyConvoys, intelX.value, intelY.value);
});

const hoverUnitId = computed(() => hoverUnit.value?.id ?? null);
const hoverStrongholdId = computed(() => hoverStronghold.value?.id ?? null);
const hoverConvoyId = computed(() => hoverConvoy.value?.id ?? null);

const scopedEventFeed = computed(() =>
  filterEventsByMessageScope(eventFeed.value, {
    player: showPlayerMessages.value,
    world: showWorldMessages.value,
  })
);

/** 浏览态悬停：固定格点悬浮框，移入框内可滚动而不消失。 */
const showHoverIntel = computed(
  () =>
    stateId.value === "navigate" &&
    popupMode.value === "none" &&
    !intelDialogVisible.value &&
    !messageDialogVisible.value &&
    !battleConfirmVisible.value &&
    !battleResultVisible.value &&
    intelPinnedCell.value !== null &&
    pinnedCellEntityCount.value > 0
);

function intelBoxCountAt(x: number, y: number): number {
  if (!state.value) return 0;
  let count = 0;
  if (cellEntities(state.value.strongholds, x, y).length > 0) count += 1;
  if (fieldBattlefieldAt(x, y)) count += 1;
  else if (cellEntities(state.value.units, x, y).length > 0) count += 1;
  if (
    cellEntities(state.value.supplyConvoys, x, y).length +
      cellEntities(state.value.messengers, x, y).length >
    0
  ) {
    count += 1;
  }
  return count;
}

function hasMultiIntelLayout(x: number, y: number): boolean {
  return intelBoxCountAt(x, y) > 1;
}

function estimateHoverIntelPopupSize(): { width: number; height: number } {
  const el = hoverIntelLayerRef.value;
  if (el && el.offsetWidth > 0 && el.offsetHeight > 0) {
    return { width: el.offsetWidth, height: el.offsetHeight };
  }
  if (intelPinnedCell.value && hasMultiIntelLayout(intelPinnedCell.value.x, intelPinnedCell.value.y)) {
    const boxCount = intelBoxCountAt(intelPinnedCell.value.x, intelPinnedCell.value.y);
    return {
      width: HOVER_INTEL_W * boxCount + HOVER_INTEL_DUAL_GAP * Math.max(0, boxCount - 1),
      height: HOVER_INTEL_H,
    };
  }
  return { width: HOVER_INTEL_W, height: HOVER_INTEL_H };
}

function measureHoverIntelPopup(): { width: number; height: number } {
  return estimateHoverIntelPopupSize();
}

function getCellViewportRect(x: number, y: number) {
  const panelRelative = mapCanvasRef.value?.getCellPanelRect(x, y, mapPanelRef.value);
  if (!panelRelative || !mapPanelRef.value) return null;

  const panelBBox = mapPanelRef.value.getBoundingClientRect();
  return {
    left: panelRelative.left + panelBBox.left,
    top: panelRelative.top + panelBBox.top,
    width: panelRelative.width,
    height: panelRelative.height,
  };
}

function getViewportBounds() {
  return {
    left: 0,
    top: 0,
    width: window.innerWidth,
    height: window.innerHeight,
  };
}

function getMapPanelViewportRect() {
  if (!mapPanelRef.value) return getViewportBounds();
  const bbox = mapPanelRef.value.getBoundingClientRect();
  return {
    left: bbox.left,
    top: bbox.top,
    width: bbox.width,
    height: bbox.height,
  };
}

function updateHoverIntelPosition() {
  if (!showHoverIntel.value || !intelPinnedCell.value || !mapPanelRef.value || !mapCanvasRef.value) {
    hoverIntelStyle.value = { display: "none" };
    return;
  }

  const cellRect = getCellViewportRect(intelPinnedCell.value.x, intelPinnedCell.value.y);
  if (!cellRect) {
    hoverIntelStyle.value = { display: "none" };
    return;
  }

  const panel = getViewportBounds();
  const mapPanelRect = getMapPanelViewportRect();

  const applyPosition = (popupSize: { width: number; height: number }, logLayout = false) => {
    const placement = resolveAnchoredPanelPlacement(
      cellRect,
      panel,
      popupSize,
      4,
      mapPanelRect
    );
    hoverIntelAnchorSide.value = placement.side;
    hoverIntelVerticalAlign.value = placement.verticalAlign;
    logStrategyMapCoords("hover-intel-position", {
      cell: { x: intelPinnedCell.value!.x, y: intelPinnedCell.value!.y },
      cellRect,
      panelSize: { width: panel.width, height: panel.height },
      mapPanelRect,
      popupSize,
      position: placement,
    });
    hoverIntelStyle.value = {
      position: "fixed",
      left: `${placement.left}px`,
      top: `${placement.top}px`,
      zIndex: "200",
    };

    if (logLayout) {
      logHoverIntelLayoutAfterLayout(placement, popupSize);
    }
  };

  applyPosition(estimateHoverIntelPopupSize());

  void nextTick(() => {
    if (!showHoverIntel.value) return;
    requestAnimationFrame(() => {
      if (!showHoverIntel.value) return;
      applyPosition(measureHoverIntelPopup(), true);
    });
  });
}

function panelCountFromDom(...elements: (Element | null)[]): number {
  return elements.filter(Boolean).length;
}

function logHoverIntelLayoutAfterLayout(
  placement: {
    left: number;
    top: number;
    side: string;
    rawTop: number;
    verticalAlign: AnchorVerticalAlign;
  },
  popupSize: { width: number; height: number }
) {
  if (!intelPinnedCell.value) return;

  const cellRect = getCellViewportRect(intelPinnedCell.value.x, intelPinnedCell.value.y);
  if (!cellRect) return;

  const layerEl = hoverIntelLayerRef.value;
  const strongholdEl = layerEl?.querySelector(".intel-box--stronghold") ?? null;
  const militaryEl = layerEl?.querySelector(".intel-box--military") ?? null;
  const civilEl = layerEl?.querySelector(".intel-box--civil") ?? null;
  const singleEl =
    layerEl?.querySelector(
      ".cell-intel-stack > .intel-box:not(.intel-box--stronghold):not(.intel-box--military):not(.intel-box--civil)"
    ) ?? null;

  const multiLayout = Boolean(strongholdEl || militaryEl || civilEl) && panelCountFromDom(strongholdEl, militaryEl, civilEl) > 1;

  logHoverIntelLayoutDebug({
    gridCell: { x: intelPinnedCell.value.x, y: intelPinnedCell.value.y },
    viewport: {
      width: window.innerWidth,
      height: window.innerHeight,
    },
    mouse: hoverCell.value
      ? {
          x: Math.round(hoverCell.value.screenX),
          y: Math.round(hoverCell.value.screenY),
        }
      : null,
    cellScreen: {
      left: Math.round(cellRect.left),
      top: Math.round(cellRect.top),
      right: Math.round(cellRect.left + cellRect.width),
      bottom: Math.round(cellRect.top + cellRect.height),
      width: Math.round(cellRect.width),
      height: Math.round(cellRect.height),
    },
    mapPanelScreen: (() => {
      const map = getMapPanelViewportRect();
      return {
        left: Math.round(map.left),
        top: Math.round(map.top),
        right: Math.round(map.left + map.width),
        bottom: Math.round(map.top + map.height),
        width: Math.round(map.width),
        height: Math.round(map.height),
      };
    })(),
    anchorSide: hoverIntelAnchorSide.value,
    verticalAlign: placement.verticalAlign,
    rawTop: placement.rawTop,
    containerScreen: rectToScreenDebug(layerEl),
    strongholdBoxScreen: rectToScreenDebug(strongholdEl),
    otherBoxScreen: rectToScreenDebug(militaryEl ?? civilEl),
    singleBoxScreen: multiLayout ? null : rectToScreenDebug(singleEl),
    placement,
    popupSize,
    dualLayout: multiLayout,
  });
}

watch([showHoverIntel, intelPinnedCell, () => popupMode.value], updateHoverIntelPosition, {
  deep: true,
});

function onViewportChange() {
  updateHoverIntelPosition();
}

const intelRoad = computed(() => {
  if (!state.value || intelX.value === null || intelY.value === null) return null;
  return (
    state.value.map.roadCells?.find((r) => r.x === intelX.value && r.y === intelY.value) ?? null
  );
});

const intelTileInfo = computed(() => {
  if (!state.value || intelBarX.value === null || intelBarY.value === null) {
    return { terrainName: null, regionName: null };
  }
  return mapTileInfo(state.value.map, intelBarX.value, intelBarY.value);
});

const intelLandmark = computed(() => {
  if (!state.value || intelBarX.value === null || intelBarY.value === null) return null;
  return landmarkAtCell(state.value.map, intelBarX.value, intelBarY.value);
});

const intelBarStronghold = computed(() =>
  state.value && intelBarX.value !== null && intelBarY.value !== null
    ? cellEntity(state.value.strongholds, intelBarX.value, intelBarY.value)
    : null
);

const popupUsesCorner = computed(
  () =>
    popupMode.value === "moveSelect" ||
    popupMode.value === "attackSelect" ||
    popupMode.value === "mergeSelect" ||
    popupMode.value === "splitSelect"
);

const cornerHintMode = computed((): "moveSelect" | "attackSelect" | "mergeSelect" | "splitSelect" => {
  if (popupMode.value === "attackSelect") return "attackSelect";
  if (popupMode.value === "mergeSelect") return "mergeSelect";
  if (popupMode.value === "splitSelect") return "splitSelect";
  return "moveSelect";
});

const menuPopupMode = computed(() => {
  const mode = popupMode.value;
  if (
    mode === "none" ||
    mode === "moveSelect" ||
    mode === "attackSelect" ||
    mode === "mergeSelect" ||
    mode === "splitSelect"
  ) {
    return null;
  }
  return mode;
});

const routeOverlays = computed((): MapRouteOverlay[] => {
  const overlays: MapRouteOverlay[] = [];
  const visibleUnits = filterUnitsForRouteDisplay(
    state.value?.units ?? [],
    routeVisibilityContext.value
  );
  const previewActive =
    popupMode.value === "moveSelect" &&
    previewRoutePoints.value.length >= 2 &&
    selectedUnitId.value !== null;

  for (const unit of visibleUnits) {
    const points = normalizeRoute(unit.route);
    if (points.length < 2) continue;
    if (previewActive && unit.id === selectedUnitId.value) continue;

    overlays.push({
      unitId: unit.id,
      points,
      variant: unit.id === selectedUnitId.value ? "emphasized" : "committed",
    });
  }

  if (previewActive) {
    overlays.push({
      unitId: selectedUnitId.value!,
      points: previewRoutePoints.value,
      variant: "preview",
    });
  }

  return overlays;
});

const moveRelayMarkers = computed((): MapMoveRelayMarker[] => {
  if (popupMode.value !== "moveSelect") return [];

  const markers: MapMoveRelayMarker[] = moveCommittedWaypoints.value.map((point, index) => ({
    x: point.x,
    y: point.y,
    kind: "committed" as const,
    order: index + 1,
  }));

  if (movePendingRelay.value) {
    markers.push({
      x: movePendingRelay.value.x,
      y: movePendingRelay.value.y,
      kind: "pending",
      order: markers.length + 1,
    });
  }

  return markers;
});

watch(
  () => popupMode.value,
  (mode) => {
    if (mode !== "moveSelect") {
      previewRoutePoints.value = [];
    }
  }
);

function resetMovePath() {
  moveCommittedWaypoints.value = [];
  movePendingRelay.value = null;
  previewRoutePoints.value = [];
  clearMovePathDebug();
}

function isValidMovePathCell(x: number, y: number): boolean {
  const unit = selectedUnit.value;
  if (!unit) return false;
  return unit.x !== x || unit.y !== y;
}

async function refreshMovePathPreview(): Promise<boolean> {
  const unit = selectedUnit.value;
  if (!unit || selectedUnitId.value === null || !movePendingRelay.value) {
    return false;
  }

  const destination = movePendingRelay.value;
  const via = [...moveCommittedWaypoints.value];
  const serial = ++previewRequestSerial;

  logMovePath("api.request.via", {
    serial,
    unitId: selectedUnitId.value,
    unitAt: { x: unit.x, y: unit.y },
    via,
    destination,
    apiMode: apiMode.value,
    usingMockFallback: usingMockFallback.value,
  });

  try {
    const preview = await previewUnitPath(
      selectedUnitId.value,
      destination.x,
      destination.y,
      { via }
    );
    if (serial !== previewRequestSerial) {
      logMovePath("api.stale", { serial, currentSerial: previewRequestSerial });
      return false;
    }

    const points = normalizeRoute(preview.points);
    logMovePath("api.response.via", {
      serial,
      points: formatPathPoints(points),
      first: points[0] ?? null,
      last: points.length ? points[points.length - 1] : null,
      viaOnPath: via.map((relay) => ({
        relay,
        index: findPointOnPath(points, relay),
      })),
    });

    if (points.length < 2) {
      logMovePath("preview.tooShort", { serial, points: formatPathPoints(points) });
      return false;
    }

    previewRoutePoints.value = points;
    return true;
  } catch (err) {
    logMovePath("api.error", {
      serial,
      message: err instanceof Error ? err.message : String(err),
    });
    return false;
  }
}

function handleBeginMove() {
  resetMovePath();
  onBeginMove();
}

function handleBeginDirective() {
  directiveDialogVisible.value = true;
}

async function handleDirectiveConfirm(payload: { directive: UnitDirectiveValue }) {
  if (selectedUnitId.value === null || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    const response = await setUnitDirective(selectedUnitId.value, payload.directive);

    state.value = response.state;
    const lord = response.state.lord;
    const unit = response.state.units.find((u) => u.id === selectedUnitId.value);
    const sameTile = unit && lord && unit.x === lord.x && unit.y === lord.y;
    info.value =
      response.outcome === "MessengerDispatched"
        ? `方针已从当主所在格 (${lord?.x}, ${lord?.y}) 派出信使，到达后生效`
        : sameTile
          ? "当主与本队同格，方针已即时生效"
          : "方针已即时生效";
    if (response.outcome === "AppliedImmediately") {
      appendEvents([{ category: "PolicyApplied", message: `✅ ${unit?.name ?? "部队"} 方针已即时生效` }]);
    } else {
      appendEvents([{ category: "PolicyDispatched", message: `📨 方针信使已从当主处出发，目标 ${unit?.name ?? "部队"}` }]);
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "方针设定失败";
    if (message.includes("DataNotFound")) {
      error.value = "服务端未加载剧本（可能 WebApi 已重启），请刷新页面或点击「重新加载」";
    } else {
      error.value = message;
    }
  } finally {
    loading.value = false;
  }
}

function formatMovePathLogDetail(detail: Record<string, unknown>): string {
  return JSON.stringify(detail, null, 2);
}

async function handleMovePathClick(payload: {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}) {
  const unit = selectedUnit.value;

  logMovePath("click", {
    cell: { x: payload.x, y: payload.y },
    unitAt: unit ? { x: unit.x, y: unit.y } : null,
    pending: movePendingRelay.value,
    committed: [...moveCommittedWaypoints.value],
    previewBefore: formatPathPoints(previewRoutePoints.value),
    valid: isValidMovePathCell(payload.x, payload.y),
  });

  if (!isValidMovePathCell(payload.x, payload.y)) return;

  onSelectCell(payload);
  if (stateId.value !== "moveTargetSelect") return;

  if (!unit) return;

  const clicked = { x: payload.x, y: payload.y };

  if (
    movePendingRelay.value &&
    movePendingRelay.value.x === clicked.x &&
    movePendingRelay.value.y === clicked.y
  ) {
    logMovePath("click.confirm", { destination: clicked, via: [...moveCommittedWaypoints.value] });
    await confirmMovePath(clicked, payload);
    return;
  }

  const onExistingPath =
    previewRoutePoints.value.length > 0 &&
    findPointOnPath(previewRoutePoints.value, clicked) >= 0;

  logMovePath("click.branch", {
    clicked,
    onExistingPath,
    onPathIdx: onExistingPath ? findPointOnPath(previewRoutePoints.value, clicked) : -1,
  });

  if (onExistingPath) {
    previewRequestSerial++;
    const truncated = truncateMovePathAtCell(
      moveCommittedWaypoints.value,
      movePendingRelay.value,
      previewRoutePoints.value,
      clicked
    );
    if (!truncated) return;

    moveCommittedWaypoints.value = truncated.committed;
    movePendingRelay.value = truncated.pending;
    previewRoutePoints.value = truncated.previewPath;
    selectedCell.value = clicked;
    return;
  }

  const previousRelay = movePendingRelay.value;

  if (previousRelay) {
    moveCommittedWaypoints.value.push({ ...previousRelay });
  }
  movePendingRelay.value = clicked;
  selectedCell.value = clicked;

  const ok = await refreshMovePathPreview();
  if (!ok) {
    logMovePath("click.previewRollback", { previousRelay, clicked });
    if (previousRelay) {
      moveCommittedWaypoints.value.pop();
    }
    movePendingRelay.value = previousRelay;
    selectedCell.value = previousRelay ?? { x: unit.x, y: unit.y };
  } else {
    logMovePath("click.stateAfter", {
      pending: movePendingRelay.value,
      committed: [...moveCommittedWaypoints.value],
      preview: formatPathPoints(previewRoutePoints.value),
    });
  }
}

async function confirmMovePath(
  destination: MapPoint,
  payload: { screenX: number; screenY: number }
) {
  const target: StrategyMoveTarget = {
    x: destination.x,
    y: destination.y,
    screenX: payload.screenX,
    screenY: payload.screenY,
  };
  const via = [...moveCommittedWaypoints.value];
  resetMovePath();
  enterExecutingCommand();
  await executeMove(target, via);
}

function normalizeRoute(
  route: Array<MapPoint | { x?: number; y?: number; X?: number; Y?: number }> | undefined
): MapPoint[] {
  if (!route?.length) return [];
  return route.map((p) => ({
    x: p.x ?? (p as { X: number }).X,
    y: p.y ?? (p as { Y: number }).Y,
  }));
}

/** 指令菜单：锚定在单位格旁；选点提示：地图区域右上角。 */
const popupStyle = computed(() => {
  if (popupMode.value === "none" || !mapPanelRef.value) {
    return { display: "none" };
  }

  if (popupUsesCorner.value) {
    return {};
  }

  if (!menuAnchor.value) return { display: "none" };

  const anchor = menuAnchor.value;
  if (mapCanvasRef.value) {
    const cellRect = mapCanvasRef.value.getCellPanelRect(
      anchor.x,
      anchor.y,
      mapPanelRef.value
    );
    if (cellRect) {
      const panel = {
        left: 0,
        top: 0,
        width: mapPanelRef.value.clientWidth,
        height: mapPanelRef.value.clientHeight,
      };
      const pos = resolveAnchoredPanelPlacement(cellRect, panel, {
        width: MENU_POPUP_W,
        height: MENU_POPUP_H,
      });
      logStrategyMapCoords("menu-popup-position", {
        anchor,
        cellRect,
        panelSize: { width: panel.width, height: panel.height },
        position: pos,
      });
      return { left: `${pos.left}px`, top: `${pos.top}px` };
    }
  }

  const rect = mapPanelRef.value.getBoundingClientRect();
  const left = Math.min(
    Math.max(8, anchor.screenX - rect.left + 8),
    rect.width - MENU_POPUP_W
  );
  const top = Math.min(
    Math.max(8, anchor.screenY - rect.top + 8),
    rect.height - MENU_POPUP_H
  );
  logStrategyMapCoords("menu-popup-fallback", { anchor, rect: { width: rect.width, height: rect.height }, left, top });
  return { left: `${left}px`, top: `${top}px` };
});

/** 指令菜单 tooltip：菜单在左半屏时向右弹出，否则向左。 */
const commandTooltipSide = computed<"left" | "right">(() => {
  if (!menuAnchor.value || !mapPanelRef.value) return "right";
  const rect = mapPanelRef.value.getBoundingClientRect();
  const mid = rect.left + rect.width / 2;
  return menuAnchor.value.screenX < mid ? "right" : "left";
});

function clearMapHoverState() {
  onHoverCell(null);
  intelPinnedCell.value = null;
  intelLayerHovered.value = false;
}

function handleHoverCell(
  cell: { x: number; y: number; screenX: number; screenY: number } | null
) {
  if (intelDialogVisible.value) return;

  logStrategyMapCoords("hover-cell", {
    cell,
    stateId: stateId.value,
    popupMode: popupMode.value,
  });
  onHoverCell(cell);

  if (cell && entityCountAt(cell.x, cell.y) > 0) {
    intelPinnedCell.value = { x: cell.x, y: cell.y };
  } else if (!intelLayerHovered.value) {
    intelPinnedCell.value = null;
  }
}

function onIntelLayerPointerEnter() {
  intelLayerHovered.value = true;
}

function onIntelLayerPointerLeave() {
  intelLayerHovered.value = false;
  const cell = hoverCell.value;
  if (
    !cell ||
    !intelPinnedCell.value ||
    cell.x !== intelPinnedCell.value.x ||
    cell.y !== intelPinnedCell.value.y
  ) {
    intelPinnedCell.value = null;
  }
}

async function notifyActionBlocked(title: string, reason: string) {
  info.value = reason;
  await ElMessageBox.alert(reason, title, {
    type: "warning",
    confirmButtonText: "知道了",
  });
}

function handleBeginAttack() {
  const reason = attackApBlockReason(selectedUnit.value);
  if (reason) {
    void notifyActionBlocked("无法攻击", reason);
    return;
  }
  onBeginAttack();
}

function handleBeginMerge() {
  onBeginMerge();
}

function handleBeginSplit() {
  if (!selectedUnit.value?.composition.length) {
    void notifyActionBlocked("无法分兵", "该部队没有可拆子编制");
    return;
  }
  splitDialogVisible.value = true;
}

function handleBeginExpedition() {
  if (!canExpeditionStronghold.value) {
    void notifyActionBlocked("无法出征", "仅当主居城且据点格无地图军时可出征");
    return;
  }
  expeditionDialogVisible.value = true;
}

async function handleSplitDialogConfirm(payload: { subUnitIds: number[]; unitName?: string }) {
  pendingSplitSubUnitIds.value = payload.subUnitIds;
  pendingSplitUnitName.value = payload.unitName;
  splitDialogVisible.value = false;
  enterSplitSpawnSelection();
}

async function handleExpeditionConfirm(payload: {
  unitName?: string;
  commanderId: number;
  composition: import("@/api/strategyTypes").StrategyDeployCompositionEntry[];
}) {
  const sh = selectedStronghold.value;
  if (!sh) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await deployFromStronghold(sh.id, payload);
    info.value = `已从 ${sh.name} 出征`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "出征失败";
  } finally {
    loading.value = false;
  }
}

async function executeMerge(sourceUnitId: number, targetUnitId: number) {
  loading.value = true;
  error.value = "";
  try {
    state.value = await mergeUnits(sourceUnitId, targetUnitId);
    info.value = "部队已合并";
    pendingMergeTargetUnitId.value = null;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "合并失败";
    pendingMergeTargetUnitId.value = null;
  } finally {
    loading.value = false;
  }
}

async function executeSplit(unitId: number, target: StrategyMoveTarget, subUnitIds: number[], unitName?: string) {
  loading.value = true;
  error.value = "";
  try {
    state.value = await splitUnit(unitId, subUnitIds, target.x, target.y, unitName);
    info.value = `已在 (${target.x}, ${target.y}) 分兵`;
    pendingSplitSubUnitIds.value = [];
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "分兵失败";
    pendingSplitSubUnitIds.value = [];
    onCancel();
  } finally {
    loading.value = false;
  }
}

async function handleSiegeOrder(mode: "Assault" | "Encircle") {
  const unitId = selectedUnitId.value;
  const sh = popupStronghold.value;
  if (unitId === null || !sh) return;

  const reason = siegeApBlockReason(selectedUnit.value);
  if (reason) {
    void notifyActionBlocked("无法攻城", reason);
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await orderUnitSiege(unitId, sh.id, mode);
    info.value = mode === "Assault" ? `已对 ${sh.name} 下达强攻` : `已对 ${sh.name} 下达包围`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "攻城指令失败";
  } finally {
    loading.value = false;
  }
}

function handleSelectUnit(payload: { unitId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-unit", payload);
  onSelectUnit(payload);

  if (stateId.value === "mergeTargetSelect") {
    const targetId = pendingMergeTargetUnitId.value;
    const sourceId = selectedUnitId.value;
    if (targetId && sourceId !== null && targetId !== sourceId) {
      void executeMerge(sourceId, targetId);
    }
  }
}

function handleSelectStronghold(payload: { strongholdId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-stronghold", payload);
  onSelectStronghold(payload);
}

function handleSelectConvoy(payload: { convoyId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-convoy", payload);
  onSelectConvoy(payload);
}

async function handleSelectCell(payload: { x: number; y: number; screenX: number; screenY: number }) {
  logStrategyMapCoords("select-cell", { ...payload, stateId: stateId.value });

  if (stateId.value === "moveTargetSelect") {
    await handleMovePathClick(payload);
    return;
  }

  const target = onSelectCell(payload);
  if (!target) return;

  if (stateId.value === "attackTargetSelect") {
    const apReason = attackApBlockReason(selectedUnit.value);
    if (apReason) {
      void notifyActionBlocked("无法攻击", apReason);
      onCancel();
      return;
    }
    await loadBattlePreview(target);
    if (battlePreview.value) {
      onBattlePreviewReady();
      battleConfirmVisible.value = true;
    }
    return;
  }

  if (stateId.value === "splitSpawnSelect") {
    const unitId = selectedUnitId.value;
    const subUnitIds = [...pendingSplitSubUnitIds.value];
    if (unitId !== null && subUnitIds.length > 0) {
      await executeSplit(unitId, target, subUnitIds, pendingSplitUnitName.value);
    }
    return;
  }

  await executeMove(target);
}

function handleMapContextMenu() {
  if (mapRightClickEnabled.value) {
    onMapRightClick();
  }
}

async function loadBattlePreview(target: StrategyMoveTarget) {
  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    battlePreview.value = await previewBattle(selectedUnitId.value, target.x, target.y);
  } catch (e) {
    const message = e instanceof Error ? e.message : "战前预览失败";
    const code = parseApiErrorCode(message);
    if (message.includes("DataNotFound")) {
      error.value = "服务端未加载剧本，已尝试自动重载；若仍失败请刷新页面";
    } else if (code === "ApNotEnough") {
      const reason =
        attackApBlockReason(selectedUnit.value) ?? "AP 不足，无法发起攻击";
      await notifyActionBlocked("无法攻击", reason);
    } else {
      error.value = message;
    }
    battlePreview.value = null;
    onCancel();
  } finally {
    loading.value = false;
  }
}

function openIntelDialog() {
  clearMapHoverState();
  if (selectedUnit.value) {
    intelDialogTarget.value = { kind: "unit", unit: selectedUnit.value };
    intelDialogVisible.value = true;
    return;
  }
  if (selectedStronghold.value) {
    intelDialogTarget.value = { kind: "stronghold", stronghold: selectedStronghold.value };
    intelDialogVisible.value = true;
    return;
  }
  if (selectedConvoy.value) {
    intelDialogTarget.value = { kind: "convoy", convoy: selectedConvoy.value };
    intelDialogVisible.value = true;
  }
}

async function handleBattleConfirm(_payload: BattleConfirmPayload) {
  const target = lockedCommand.value;
  if (!target || selectedUnitId.value === null) return;
  onConfirmBattle();
  battleConfirmVisible.value = false;
  await executeBattle(target);
}

async function executeBattle(target: StrategyMoveTarget) {
  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await orderUnitAttack(selectedUnitId.value, target.x, target.y);
    battlePreview.value = null;
    onBattleSucceeded();
    info.value = "攻击命令已下达，推进日期后由系统结算战斗";
  } catch (e) {
    const message = e instanceof Error ? e.message : "攻击命令失败";
    const code = parseApiErrorCode(message);
    if (code === "ApNotEnough") {
      const reason =
        attackApBlockReason(selectedUnit.value) ?? "AP 不足，无法下达攻击命令";
      await notifyActionBlocked("无法攻击", reason);
    } else {
      error.value = message;
    }
    onBattleFailed(target);
    if (battlePreview.value) battleConfirmVisible.value = true;
  } finally {
    loading.value = false;
  }
}

function showResolvedBattle(result: StrategyBattleResult) {
  battleResult.value = result;
  battleResultVisible.value = true;
}

function pushNotification(notification: StrategyPendingNotification) {
  if (notification.kind === "battle" && notification.battleResult) {
    const key = `${notification.battleResult.resolutionSeed}:${notification.battleResult.attackerUnitId}:${notification.battleResult.defenderUnitId}`;
    const exists = pendingNotifications.value.some(
      (item) =>
        item.kind === "battle" &&
        item.battleResult &&
        `${item.battleResult.resolutionSeed}:${item.battleResult.attackerUnitId}:${item.battleResult.defenderUnitId}` === key
    );
    if (exists) return;
  }

  pendingNotifications.value = [...pendingNotifications.value, notification];
}

function openSettlementDialog(event: StrategyEvent) {
  settlementDetail.value = parseEconomySettlementFromEvent(event);
  settlementDialogVisible.value = true;
}

function openEventDetailDialog(event: StrategyEvent) {
  const category =
    event.category === "StrategicReportArrived" && event.detailCategory
      ? event.detailCategory === "SiegeEncircle"
        ? "围城开始"
        : event.detailCategory === "SiegeAssault"
          ? "强攻开始"
          : messageCategoryLabel(event.detailCategory)
      : messageCategoryLabel(event.category);
  eventDetailTitle.value = category;
  eventDetailText.value =
    event.category === "StrategicReportArrived"
      ? strategicReportDetailText(event)
      : event.message;
  eventDetailVisible.value = true;
}

function handleNotificationOpen(notification: StrategyPendingNotification) {
  pendingNotifications.value = pendingNotifications.value.filter(
    (item) => item.id !== notification.id
  );

  if (notification.kind === "battle" && notification.battleResult) {
    const br = notification.battleResult;
    if (
      (br.engagementKind === "SiegeEncircle" || br.engagementKind === "SiegeAssault")
      && br.attackerCasualties === 0
      && br.defenderCasualties === 0
      && br.logEntries?.length === 1
    ) {
      openEventDetailDialog({
        category: "StrategicReportArrived",
        message: br.logEntries[0]?.message ?? `${br.attackerName} 对 ${br.defenderName} 发动攻城。`,
        brief: notification.brief,
        detailCategory: br.engagementKind,
      } as StrategyEvent);
      return;
    }
    showResolvedBattle(notification.battleResult);
    return;
  }

  if (notification.kind === "economy" && notification.event) {
    openSettlementDialog(notification.event);
    return;
  }

  if (notification.event) {
    openEventDetailDialog(notification.event);
  }
}

function handlePopupCancel() {
  battlePreview.value = null;
  battleConfirmVisible.value = false;
  resetMovePath();
  onCancel();
}

function toggleForceCommandMenu() {
  forceCommandVisible.value = !forceCommandVisible.value;
}

function closeForceCommandMenu() {
  forceCommandVisible.value = false;
}

function openForceIntelFromMenu() {
  closeForceCommandMenu();
  intelSystemInitialTab.value = "force";
  intelSystemVisible.value = true;
}

function openIntelSystemDialog() {
  intelSystemInitialTab.value = "force";
  intelSystemVisible.value = true;
}

function isInsideForcePopup(target: EventTarget | null): boolean {
  if (!(target instanceof Node)) return false;
  const forceEl = forcePopupRef.value?.$el as HTMLElement | undefined;
  const statusEl = forceStatusRef.value;
  return Boolean(
    (forceEl && forceEl.contains(target)) || (statusEl && statusEl.contains(target))
  );
}

function isInsideMapPopup(target: EventTarget | null): boolean {
  if (!(target instanceof Node)) return false;
  const menuEl = menuPopupRef.value?.$el as HTMLElement | undefined;
  const cornerEl = cornerPopupRef.value?.$el as HTMLElement | undefined;
  return Boolean(
    (menuEl && menuEl.contains(target)) || (cornerEl && cornerEl.contains(target))
  );
}

function isBlockingOverlayTarget(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false;
  return Boolean(target.closest(".el-overlay, .el-message-box"));
}

/** 侧栏调试区、顶部提示条等 UI 不应触发「点空白取消地图 Popup」。 */
function isInsideProtectedChrome(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false;
  return Boolean(target.closest(".side-panel, .error-bar"));
}

function handleGlobalPointerDown(event: PointerEvent) {
  if (forceCommandVisible.value && !isInsideForcePopup(event.target)) {
    closeForceCommandMenu();
  }

  if (popupMode.value === "none") return;
  if (isInsideMapPopup(event.target)) return;
  if (isBlockingOverlayTarget(event.target)) return;
  if (isInsideProtectedChrome(event.target)) return;
  if (popupUsesCorner.value && mapCanvasRef.value?.containsPointerTarget(event.target)) return;

  event.preventDefault();
  event.stopPropagation();
  handlePopupCancel();
}

function onBattleConfirmVisibleChange(visible: boolean) {
  battleConfirmVisible.value = visible;
  if (!visible && stateId.value === "battleConfirm") {
    battlePreview.value = null;
    onCancel();
  }
}

function onIntelDialogVisibleChange(visible: boolean) {
  intelDialogVisible.value = visible;
  if (visible) {
    clearMapHoverState();
  } else {
    intelDialogTarget.value = null;
  }
}

async function executeMove(target: StrategyMoveTarget, via: MapPoint[] = []) {
  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await moveUnit(selectedUnitId.value, target.x, target.y, via);
    onMoveSucceeded();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "移动指令失败";
    onMoveFailed(target);
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function refreshMovementTrace() {
  if (apiMode.value === "mock") return;
  try {
    movementTrace.value = await getMovementTrace();
  } catch {
    /* Live 未启动或无 trace 端点时忽略 */
  }
}

async function fetchGameState() {
  loading.value = true;
  error.value = "";
  info.value = "";
  try {
    state.value = await getStrategyState();
    selectedUnitId.value = null;
    selectedStrongholdId.value = null;
    selectedConvoyId.value = null;
    selectedCell.value = null;
    battleConfirmVisible.value = false;
    battleResultVisible.value = false;
    intelDialogVisible.value = false;
    eventFeed.value = [];
    pendingNotifications.value = [];
    settlementDialogVisible.value = false;
    settlementDetail.value = null;
    mapInteraction.reset();
    resetMovePath();
    if (usingMockFallback.value) {
      info.value = "Live API 不可达，已自动使用 Mock 数据（见下方诊断面板）。";
    } else if (lastRequest.value?.source === "mock") {
      info.value = "当前为 Mock 模式。";
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读取世界状态失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

/** 开发用：从剧本 JSON 重新初始化后端内存仿真。 */
async function reloadScenario() {
  loading.value = true;
  error.value = "";
  info.value = "";
  try {
    state.value = await loadScenario("mini_kanto");
    selectedUnitId.value = null;
    selectedStrongholdId.value = null;
    selectedConvoyId.value = null;
    selectedCell.value = null;
    battleConfirmVisible.value = false;
    battleResultVisible.value = false;
    intelDialogVisible.value = false;
    eventFeed.value = [];
    pendingNotifications.value = [];
    settlementDialogVisible.value = false;
    settlementDetail.value = null;
    mapInteraction.reset();
    resetMovePath();
    info.value = "已重新加载剧本（世界已初始化）";
  } catch (e) {
    error.value = e instanceof Error ? e.message : "加载剧本失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

function switchApiMode(mode: StrategyApiMode) {
  setApiMode(mode);
  void fetchGameState();
}

function appendEvents(events: StrategyEvent[]) {
  if (!events.length) return;
  eventFeed.value = [...eventFeed.value, ...events].slice(-80);
  const playerForceId = state.value?.playerForceId;
  for (const evt of events) {
    const trayItem = notificationFromEvent(evt, playerForceId, state.value ?? undefined);
    if (trayItem) pushNotification(trayItem);
  }
}

async function onAdvanceDay() {
  loading.value = true;
  error.value = "";
  try {
    const response = await advanceDay();
    state.value = response.state;
    appendEvents(response.events ?? []);
  } catch (e) {
    error.value = e instanceof Error ? e.message : "推进日期失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function onSaveGame() {
  loading.value = true;
  error.value = "";
  try {
    await exportStrategySave();
    info.value = "存档已写入浏览器 localStorage";
  } catch (e) {
    error.value = e instanceof Error ? e.message : "存档失败";
  } finally {
    loading.value = false;
  }
}

async function onLoadSave() {
  if (!hasLocalStrategySave()) {
    error.value = "未找到本地存档";
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await restoreStrategySave();
    selectedUnitId.value = null;
    selectedStrongholdId.value = null;
    selectedConvoyId.value = null;
    selectedCell.value = null;
    mapInteraction.reset();
    info.value = "已从本地存档恢复";
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读档失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

onMounted(() => {
  window.addEventListener("pointerdown", handleGlobalPointerDown, true);
  window.addEventListener("resize", updateHoverIntelPosition);
  void fetchGameState();
});

onBeforeUnmount(() => {
  window.removeEventListener("pointerdown", handleGlobalPointerDown, true);
  window.removeEventListener("resize", updateHoverIntelPosition);
});
</script>

<template>
  <div class="strategy-page">
    <el-alert v-if="info" type="info" :title="info" show-icon :closable="false" class="error-bar" />
    <el-alert v-if="error" type="error" :title="error" show-icon :closable="false" class="error-bar" />

    <div class="strategy-body">
      <aside class="side-panel">
        <h3>调试</h3>
        <el-button size="small" :loading="loading" @click="reloadScenario">重新加载剧本</el-button>
        <el-button
          type="primary"
          size="small"
          :loading="loading"
          class="debug-advance-btn"
          @click="onAdvanceDay"
        >
          ▶ 推进 1 日
        </el-button>

        <h3>API 诊断</h3>
        <ul class="diag-list">
          <li>模式：<code>{{ apiMode }}</code></li>
          <li v-if="lastRequest">
            请求：<code>{{ lastRequest.method }} {{ lastRequest.path }}</code>
          </li>
          <li v-if="lastRequest">
            完整 URL：<code class="url">{{ lastRequest.fullUrl }}</code>
          </li>
          <li v-if="lastRequest">页面 Origin：<code>{{ lastRequest.pageOrigin }}</code></li>
          <li v-if="lastRequest">
            来源 / 结果：
            <code>{{ lastRequest.source }}</code>
            /
            <code>{{ lastRequest.ok ? "OK" : "失败" }}</code>
            <span v-if="lastRequest.status"> ({{ lastRequest.status }})</span>
          </li>
          <li v-if="lastRequest?.error" class="diag-error">错误：{{ lastRequest.error }}</li>
          <li v-if="lastRequest">时间：{{ lastRequest.at }}</li>
        </ul>
        <div class="mode-buttons">
          <el-button size="small" :type="apiMode === 'auto' ? 'primary' : 'default'" @click="switchApiMode('auto')">
            Auto
          </el-button>
          <el-button size="small" :type="apiMode === 'live' ? 'primary' : 'default'" @click="switchApiMode('live')">
            Live
          </el-button>
          <el-button size="small" :type="apiMode === 'mock' ? 'primary' : 'default'" @click="switchApiMode('mock')">
            Mock
          </el-button>
        </div>

        <h3>移动路径诊断</h3>
        <p class="hint">开发模式：关注 <code>api.response.via</code> 中 <code>viaOnPath</code> 是否包含全部中继。</p>
        <ul class="move-path-log">
          <li v-for="(entry, i) in movePathDebugEntries" :key="i">
            <span class="log-time">{{ entry.at }}</span>
            <strong>{{ entry.event }}</strong>
            <pre class="log-detail">{{ formatMovePathLogDetail(entry.detail) }}</pre>
          </li>
          <li v-if="!movePathDebugEntries.length" class="hint">暂无日志（进入移动选格后点击地图）</li>
        </ul>

        <h3>选中单位</h3>
        <template v-if="selectedUnit">
          <p class="unit-name" :style="{ color: getForceColorCss(selectedUnit.forceId) }">
            {{ selectedUnit.name }}
          </p>
          <ul class="unit-stats">
            <li>位置：({{ selectedUnit.x }}, {{ selectedUnit.y }})</li>
            <li>兵数：{{ formatSoldiers(selectedUnit.soldiers) }}</li>
            <li>移动力：{{ selectedUnit.movement }}</li>
            <li>AP：{{ selectedUnit.ap }}</li>
            <li>状态：{{ selectedUnit.status }}</li>
            <li v-if="selectedUnit.status === 'Moving'" class="ap-hint">
              移动中：若 AP 不足，需再推进数日沿路径继续（见下方移动诊断）。
            </li>
          </ul>
          <p class="hint">点击地图上的己方单位打开指令菜单。</p>
        </template>
        <p v-else class="empty">点击地图上的单位进行选择</p>

        <h3 v-if="movementTrace.length">移动诊断（Live）</h3>
        <ol v-if="movementTrace.length" class="trace-list">
          <li v-for="entry in movementTrace.slice(-12).reverse()" :key="entry.sequence">
            <code>{{ entry.phase }}</code>
            {{ entry.message }}
            <span v-if="entry.unitId"> #{{ entry.unitId }}</span>
            <span v-if="entry.fromX != null"> ({{ entry.fromX }},{{ entry.fromY }})</span>
            <span v-if="entry.toX != null">→({{ entry.toX }},{{ entry.toY }})</span>
            <span v-if="entry.detail" class="trace-detail"> {{ entry.detail }}</span>
          </li>
        </ol>

        <h3>后勤</h3>
        <ul class="stronghold-list">
          <li>运输队：{{ state?.supplyConvoys.length ?? 0 }}</li>
          <li>信使：{{ state?.messengers.length ?? 0 }}</li>
        </ul>
      </aside>

      <div class="map-column">
        <main ref="mapPanelRef" class="map-panel" @contextmenu.prevent="handleMapContextMenu">
          <StrategyMapCanvas
            v-if="state"
            ref="mapCanvasRef"
            :world-state="state"
            :selected-unit-id="selectedUnitId"
            :selected-convoy-id="selectedConvoyId"
            :hover-unit-id="hoverUnitId"
            :hover-stronghold-id="hoverStrongholdId"
            :hover-convoy-id="hoverConvoyId"
            :selected-cell="selectedCell"
            :route-overlays="routeOverlays"
            :move-relay-markers="moveRelayMarkers"
            :map-unit-selection-enabled="mapUnitSelectionEnabled"
            :map-convoy-selection-enabled="mapConvoySelectionEnabled"
            :map-cell-selection-enabled="mapCellSelectionEnabled"
            :map-stronghold-selection-enabled="mapStrongholdSelectionEnabled"
            :map-hover-suppressed="intelDialogVisible"
            :map-color-mode="mapColorMode"
            @select-unit="handleSelectUnit"
            @select-stronghold="handleSelectStronghold"
            @select-convoy="handleSelectConvoy"
            @select-cell="handleSelectCell"
            @hover-cell="handleHoverCell"
            @viewport-change="onViewportChange"
          />

          <div
            v-if="state"
            class="map-overlay map-overlay--top"
            @pointerdown.stop
            @click.stop
            @wheel.stop
          >
            <div class="map-top-left">
              <div class="map-message-column">
                <div class="map-time-control map-float-panel">
                  <span class="date">{{ dateText }}</span>
                  <el-radio-group v-model="gameSpeed" size="small" class="speed-radios">
                    <el-radio-button :label="1" title="1 倍速（后续实装）">▶</el-radio-button>
                    <el-radio-button :label="2" title="2 倍速（后续实装）">▶▶</el-radio-button>
                    <el-radio-button :label="4" title="4 倍速（后续实装）">▶▶▶</el-radio-button>
                  </el-radio-group>
                </div>
                <div class="map-message-zone">
                  <StrategyMessageFeedToolbar
                    v-model:show-player="showPlayerMessages"
                    v-model:show-world="showWorldMessages"
                    @open-dialog="messageDialogVisible = true"
                  />
                  <StrategyEventFeed :events="scopedEventFeed" />
                </div>
              </div>
              <div
                v-if="playerForce && playerForceStats"
                class="map-top-status map-float-panel"
              >
                <div
                  ref="forceStatusRef"
                  class="map-top-status__force map-top-status__force--clickable"
                  title="点击打开势力指令"
                  @click.stop="toggleForceCommandMenu"
                >
                  <div class="force-identity">
                    <span class="force-name">{{ playerForce.name }}</span>
                    <span v-if="playerLordName" class="force-lord" title="当主">
                      👑 {{ playerLordName }}
                    </span>
                    <span v-if="lordResidenceName" class="force-residence" title="居城">
                      🏠 {{ lordResidenceName }}
                    </span>
                    <span class="force-stat" title="威望">
                      ⭐ {{ playerForceStats.prestige }}
                    </span>
                    <span class="force-stat" title="正统">
                      📜 {{ playerForceStats.orthodoxy }}
                    </span>
                  </div>
                  <div class="force-resources">
                    <span class="force-stat" title="金钱">
                      💰 {{ formatMoneyKan(playerForce.money) }}
                    </span>
                    <span class="force-stat" title="粮食">
                      🌾 {{ formatFoodKoku(playerForce.food) }}
                    </span>
                    <span class="force-stat" title="据点数（封地合计 · 本势力）">
                      🏯 {{ playerForceStats.strongholdCount }}
                      <span class="force-stat-sub">({{ playerForceStats.ownStrongholdCount }})</span>
                    </span>
                    <span class="force-stat" title="将领数（封地合计 · 本势力）">
                      ⚔ {{ playerForceStats.characterCount }}
                      <span class="force-stat-sub">({{ playerForceStats.ownCharacterCount }})</span>
                    </span>
                  </div>
                </div>
                <StrategyForceCommandPopup
                  v-if="forceCommandVisible && playerForce"
                  ref="forcePopupRef"
                  class="map-force-command-layer"
                  :force-name="playerForce.name"
                  tooltip-side="right"
                  @show-intel="openForceIntelFromMenu"
                  @cancel="closeForceCommandMenu"
                />
              </div>
            </div>
            <div class="map-top-actions map-float-panel">
              <el-button size="small" @click="openIntelSystemDialog">情报</el-button>
              <el-button size="small" @click="systemMenuVisible = true">系统</el-button>
            </div>
          </div>

          <div
            v-if="state"
            class="map-overlay map-overlay--bottom"
            @pointerdown.stop
            @click.stop
            @wheel.stop
          >
            <div class="map-bottom-left">
              <div class="map-bottom-left-panel">
                <div v-if="pendingNotifications.length" class="map-bottom-notify">
                  <StrategyNotificationTray
                    :notifications="pendingNotifications"
                    @open="handleNotificationOpen"
                  />
                </div>
                <StrategyIntelBar
                  class="map-bottom-intel"
                  :world-state="state"
                  :x="intelBarX"
                  :y="intelBarY"
                  :terrain-name="intelTileInfo.terrainName"
                  :region-name="intelTileInfo.regionName"
                  :road-name="intelRoad?.typeName ?? null"
                  :road-level="intelRoad?.level ?? null"
                  :stronghold="intelBarStronghold"
                  :landmark-name="intelLandmark?.name ?? null"
                />
              </div>
            </div>
            <div class="map-bottom-right">
              <div class="map-game-pace map-float-panel">
                <el-button
                  v-if="gamePaused"
                  type="primary"
                  size="large"
                  class="game-pace-btn"
                  title="进入进行（自动推进，后续实装）"
                  @click="gamePaused = false"
                >
                  进行
                </el-button>
                <el-button
                  v-else
                  type="primary"
                  size="large"
                  class="game-pace-btn"
                  title="进入战略（暂停，手动操作）"
                  @click="gamePaused = true"
                >
                  战略
                </el-button>
              </div>
              <StrategyMapViewControls v-model="mapColorMode" class="map-bottom-view" />
            </div>
          </div>

          <StrategyMapPopup
            v-if="menuPopupMode && menuAnchor"
            ref="menuPopupRef"
            class="map-popup-layer map-popup-layer--anchor"
            :style="popupStyle"
            :mode="menuPopupMode"
            :entity-name="popupEntityName"
            :x="menuAnchor.x"
            :y="menuAnchor.y"
            :unit="selectedUnit"
            :tooltip-side="commandTooltipSide"
            :can-siege="canSiegePopupStronghold"
            :siege-stronghold-id="popupStronghold?.id ?? null"
            :can-expedition="canExpeditionStronghold"
            @begin-move="handleBeginMove"
            @begin-attack="handleBeginAttack"
            @begin-directive="handleBeginDirective"
            @begin-merge="handleBeginMerge"
            @begin-split="handleBeginSplit"
            @begin-expedition="handleBeginExpedition"
            @siege-assault="handleSiegeOrder('Assault')"
            @siege-encircle="handleSiegeOrder('Encircle')"
            @show-intel="openIntelDialog"
            @cancel="handlePopupCancel"
          />
          <StrategyMapPopup
            v-if="popupUsesCorner"
            ref="cornerPopupRef"
            class="map-popup-layer map-popup-layer--corner"
            :mode="cornerHintMode"
            :x="selectedCell?.x ?? 0"
            :y="selectedCell?.y ?? 0"
            @cancel="handlePopupCancel"
          />
          <div v-if="!state" class="map-placeholder">
            <el-empty description="正在加载 mini_kanto 剧本…" />
          </div>
        </main>
      </div>
    </div>

    <StrategyEntityIntelDialog
      v-if="state"
      :visible="intelDialogVisible"
      :world-state="state"
      :target="intelDialogTarget"
      @update:visible="onIntelDialogVisibleChange"
    />
    <StrategyBattleConfirmDialog
      v-if="state"
      :visible="battleConfirmVisible"
      :world-state="state"
      :attacker="selectedUnit"
      :preview="battlePreview"
      @update:visible="onBattleConfirmVisibleChange"
      @confirm="handleBattleConfirm"
    />
    <StrategyBattleResultDialog
      :visible="battleResultVisible"
      :result="battleResult"
      :player-force-id="state?.playerForceId ?? 1"
      :world-state="state"
      @update:visible="battleResultVisible = $event"
    />
    <StrategyEconomySettlementDialog
      :visible="settlementDialogVisible"
      :detail="settlementDetail"
      @update:visible="settlementDialogVisible = $event"
    />
    <el-dialog
      :model-value="eventDetailVisible"
      :title="eventDetailTitle"
      width="560px"
      align-center
      destroy-on-close
      class="strategy-event-detail-dialog strategy-dialog-centered-footer"
      @update:model-value="eventDetailVisible = $event"
    >
      <textarea
        class="event-detail-textarea"
        readonly
        tabindex="-1"
        :value="eventDetailText"
        aria-label="消息详情"
      />
      <template #footer>
        <el-button type="primary" @click="eventDetailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
    <StrategyMessageDialog
      :visible="messageDialogVisible"
      :events="scopedEventFeed"
      @update:visible="messageDialogVisible = $event"
    />
    <StrategyDirectiveDialog
      v-if="state"
      :visible="directiveDialogVisible"
      :unit="selectedUnit"
      :lord="state.lord"
      :strongholds="state.strongholds"
      @update:visible="directiveDialogVisible = $event"
      @confirm="handleDirectiveConfirm"
    />
    <StrategySplitDialog
      :visible="splitDialogVisible"
      :unit="selectedUnit"
      @update:visible="splitDialogVisible = $event"
      @confirm="handleSplitDialogConfirm"
    />
    <StrategyExpeditionDialog
      v-if="state"
      :visible="expeditionDialogVisible"
      :stronghold="selectedStronghold"
      :characters="state.characters ?? []"
      :player-force-id="state.playerForceId"
      @update:visible="expeditionDialogVisible = $event"
      @confirm="handleExpeditionConfirm"
    />
    <StrategyIntelSystemDialog
      :visible="intelSystemVisible"
      :world-state="state"
      :initial-tab="intelSystemInitialTab"
      @update:visible="intelSystemVisible = $event"
    />
    <StrategySystemMenuDialog
      :visible="systemMenuVisible"
      @update:visible="systemMenuVisible = $event"
      @save="onSaveGame"
      @load="onLoadSave"
    />

    <div
      v-if="showHoverIntel && state && intelPinnedCell"
      ref="hoverIntelLayerRef"
      class="hover-intel-layer"
      :style="hoverIntelStyle"
      @pointerdown.stop
      @click.stop
      @pointerenter="onIntelLayerPointerEnter"
      @pointerleave="onIntelLayerPointerLeave"
    >
      <StrategyCellIntelHover
        :world-state="state"
        :x="intelPinnedCell.x"
        :y="intelPinnedCell.y"
              :anchor-side="hoverIntelAnchorSide"
              :vertical-align="hoverIntelVerticalAlign"
            />
    </div>
  </div>
</template>

<style scoped>
.strategy-page {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 80px);
  min-width: 960px;
  min-height: 640px;
  gap: 8px;
  overflow: auto;
}

.map-float-panel {
  flex-shrink: 0;
  padding: 8px 14px;
  font-size: 0.88rem;
  color: #e2e8f0;
  background: rgba(15, 23, 42, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.35);
  border-radius: 10px;
  backdrop-filter: blur(6px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
  pointer-events: auto;
}

.map-top-left {
  display: flex;
  flex-direction: row;
  align-items: flex-start;
  gap: 8px;
  flex: 0 1 auto;
  min-width: 0;
  max-width: min(calc(100% - 120px), 720px);
}

.map-message-column {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 4px;
  width: 20em;
  flex-shrink: 0;
  min-width: 0;
}

.map-time-control {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  gap: 6px;
  flex-shrink: 0;
  width: fit-content;
  max-width: 100%;
  padding: 6px 10px;
  box-sizing: border-box;
}

.map-time-control .date {
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
  white-space: pre;
}

.map-time-control .speed-radios {
  flex-shrink: 0;
}

.map-time-control .speed-radios :deep(.el-radio-button) {
  flex: 1 1 0;
}

.map-time-control .speed-radios :deep(.el-radio-button__inner) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  min-width: 34px;
  padding: 4px 0;
  font-size: 0.68rem;
  line-height: 1.2;
  box-sizing: border-box;
}

.map-top-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
  margin-left: auto;
}

.map-top-status {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  flex-shrink: 0;
}

.map-top-status__force {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px 12px;
}

.map-top-status__force .force-identity,
.map-top-status__force .force-resources {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px 12px;
}

.map-top-status__force .force-name {
  font-weight: 600;
}

.map-top-status__force .force-lord {
  color: #e2e8f0;
}

.map-top-status__force .force-residence {
  color: #94a3b8;
}

.map-top-status__force .force-stat {
  color: #cbd5e1;
  font-size: 0.84rem;
  white-space: nowrap;
}

.map-top-status__force .force-stat-sub {
  color: #94a3b8;
  font-size: 0.8rem;
}

@media (min-width: 1366px) {
  .map-top-status__force {
    flex-direction: row;
    flex-wrap: nowrap;
    align-items: center;
    gap: 12px;
  }
}

.map-top-status__force--clickable {
  cursor: pointer;
  border-radius: 6px;
  padding: 2px 4px;
  margin: -2px -4px;
  transition: background 0.15s ease;
}

.map-top-status__force--clickable:hover {
  background: rgba(148, 163, 184, 0.15);
}

.map-force-command-layer {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  z-index: 20;
}

.map-top-actions :deep(.el-button) {
  margin: 0;
}

.date {
  color: #e2e8f0;
  font-size: 0.9rem;
  white-space: nowrap;
}

.map-column {
  flex: 1;
  min-width: 640px;
  display: flex;
  flex-direction: column;
  min-height: 0;
  background: #0f172a;
  border-radius: 8px;
  overflow: hidden;
}

.map-column .map-panel {
  flex: 1;
  min-height: 0;
  position: relative;
  overflow: hidden;
  border-radius: 8px;
}

.map-overlay {
  position: absolute;
  left: 0;
  right: 0;
  z-index: 14;
  pointer-events: none;
  padding: 10px 12px;
  box-sizing: border-box;
}

.map-overlay--top {
  top: 0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.map-message-zone {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
  flex-shrink: 0;
  min-width: 0;
  font-size: 0.82rem;
  pointer-events: none;
}

.map-message-zone :deep(.message-feed-toolbar) {
  pointer-events: auto;
}

.map-overlay--bottom {
  bottom: 0;
  display: flex;
  flex-direction: row;
  align-items: flex-end;
  gap: 10px;
}

.map-bottom-left {
  flex: 1;
  display: flex;
  justify-content: center;
  min-width: 0;
}

.map-bottom-left-panel {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  min-width: 0;
}

@media (min-width: 768px) {
  .map-bottom-left-panel {
    width: 80%;
  }
}

@media (min-width: 1200px) {
  .map-bottom-left-panel {
    width: 60%;
  }
}

.map-bottom-notify {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  /* 与 StrategyIntelBar 水平 padding 对齐，使图标行宽 = 情报栏内容区宽 */
  padding: 0 14px;
  box-sizing: border-box;
  pointer-events: auto;
}

.map-bottom-intel {
  width: 100%;
  flex: 0 1 auto;
  min-width: 0;
  pointer-events: auto;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
}

.map-bottom-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
  flex-shrink: 0;
}

.map-game-pace {
  pointer-events: auto;
  display: flex;
  justify-content: flex-end;
}

.game-pace-btn {
  min-width: 5.5em;
  padding: 10px 22px;
  font-size: 0.95rem;
  font-weight: 600;
}

.map-bottom-view {
  flex-shrink: 0;
}

.map-bottom-view :deep(.map-view-controls) {
  pointer-events: auto;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
}

.strategy-body {
  display: flex;
  flex: 1;
  gap: 12px;
  min-height: 0;
}

.side-panel {
  width: 280px;
  flex-shrink: 0;
  padding: 12px;
  background: #1e293b;
  color: #e2e8f0;
  border-radius: 8px;
  overflow-y: auto;
  font-size: 0.9rem;
}

.side-panel h3 {
  margin: 0 0 8px;
  font-size: 0.85rem;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.side-panel h3:not(:first-child) {
  margin-top: 16px;
}

.debug-advance-btn {
  display: block;
  width: 100%;
  margin-top: 8px;
}

.unit-name {
  margin: 0 0 8px;
  font-weight: 600;
  font-size: 1rem;
}

.unit-stats {
  margin: 0 0 12px;
  padding-left: 18px;
}

.hint {
  margin: 0 0 16px;
  color: #64748b;
  font-size: 0.8rem;
  line-height: 1.4;
}

.trace-list {
  margin: 0 0 12px;
  padding-left: 1.2rem;
  font-size: 0.72rem;
  color: #cbd5e1;
  max-height: 220px;
  overflow-y: auto;
}

.trace-list li {
  margin-bottom: 4px;
}

.trace-detail {
  color: #94a3b8;
}

.empty {
  color: #64748b;
  margin: 0 0 16px;
}

.stronghold-list {
  margin: 0;
  padding-left: 18px;
  line-height: 1.6;
}

.diag-list {
  margin: 0 0 10px;
  padding-left: 18px;
  line-height: 1.5;
  font-size: 0.8rem;
  word-break: break-all;
}

.diag-list code {
  font-size: 0.75rem;
  color: #93c5fd;
}

.diag-list code.url {
  display: inline-block;
  margin-top: 2px;
}

.diag-error {
  color: #fca5a5;
}

.mode-buttons {
  display: flex;
  gap: 6px;
  margin-bottom: 16px;
}

.move-path-log {
  margin: 0 0 16px;
  padding-left: 0;
  list-style: none;
  max-height: 320px;
  overflow-y: auto;
  font-size: 0.72rem;
}

.move-path-log li {
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid #334155;
}

.move-path-log .log-time {
  color: #64748b;
  margin-right: 6px;
}

.move-path-log .log-detail {
  margin: 4px 0 0;
  padding: 6px;
  background: #0f172a;
  border-radius: 4px;
  white-space: pre-wrap;
  word-break: break-all;
  color: #cbd5e1;
  font-family: ui-monospace, monospace;
  font-size: 0.68rem;
}

.map-popup-layer {
  position: absolute;
  z-index: 20;
  pointer-events: auto;
}

.map-popup-layer--anchor {
  /* left/top 由 popupStyle 动态设置 */
}

.map-popup-layer--corner {
  top: 12px;
  right: 12px;
  left: auto;
}

.hover-intel-layer {
  position: fixed;
  z-index: 200;
  pointer-events: auto;
  width: max-content;
  max-width: none;
  padding: 0;
  box-sizing: border-box;
  background: transparent;
  border: none;
  border-radius: 0;
  box-shadow: none;
  overflow: visible;
  isolation: isolate;
}

/* 兜底：确保子组件面板背景/边框在地图上方可见 */
.hover-intel-layer :deep(.intel-box) {
  background: #1e293b;
  border: 1px solid #38bdf8;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
}

.hover-intel-layer :deep(.intel-box--civil) {
  border-color: #4ade80;
}

.map-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.error-bar {
  margin: 0;
}

.event-detail-textarea {
  display: block;
  width: 100%;
  min-height: min(360px, 50vh);
  max-height: min(360px, 50vh);
  margin: 0;
  padding: 10px 12px;
  box-sizing: border-box;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  resize: none;
  outline: none;
  background: #f8fafc;
  color: #1e293b;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.88rem;
  line-height: 1.55;
  white-space: pre-wrap;
  overflow-y: auto;
}
</style>
