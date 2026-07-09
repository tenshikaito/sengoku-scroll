<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { ElMessageBox } from "element-plus";
import {
  advanceDay,
  exportStrategySave,
  restoreStrategySave,
  hasLocalStrategySave,
  getMovementTrace,
  loadScenario,
  moveUnit,
  orderUnitAttack,
  previewBattle,
  previewUnitPath,
  setUnitDirective,
  setStrategySessionRecoveryHandler,
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
import StrategyCellIntelHover from "@/components/strategy/StrategyCellIntelHover.vue";
import StrategyMapViewControls from "@/components/strategy/StrategyMapViewControls.vue";
import type { MapRouteOverlay, MapMoveRelayMarker } from "@/components/strategy/mapRouteStyles";
import { getForceColorCss } from "@/components/strategy/forceColors";
import type { StrategyMapColorMode } from "@/utils/mapEntityColors";
import { formatFoodGo, formatMoney, formatSoldiers } from "@/utils/strategyDisplayUnits";
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
import { attackApBlockReason, parseApiErrorCode } from "@/utils/strategyActionRules";
import {
  notificationFromBattle,
  notificationFromEvent,
} from "@/utils/strategyNotifications";

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
const hoverIntelAnchorSide = ref<AnchorSide>("right");
const hoverIntelVerticalAlign = ref<AnchorVerticalAlign>("start");
const messageDialogVisible = ref(false);
const showPlayerMessages = ref(true);
const showWorldMessages = ref(true);
const directiveDialogVisible = ref(false);
const intelDialogVisible = ref(false);
const intelDialogTarget = ref<EntityIntelTarget | null>(null);
const hoverIntelStyle = ref<{ left: string; top: string } | { display: "none" }>({ display: "none" });
let previewRequestSerial = 0;

const apiMode = computed(() => strategyApiDiagnostics.mode);
const lastRequest = computed(() => strategyApiDiagnostics.last);
const usingMockFallback = computed(() => strategyApiDiagnostics.usingMockFallback);

const dateText = computed(() => {
  if (!state.value) return "—";
  const d = state.value.date;
  return `${d.year}年${d.month}月${d.day}日`;
});

const selectedUnit = computed(
  () => state.value?.units.find((u) => u.id === selectedUnitId.value) ?? null
);

const selectedStronghold = computed(
  () => state.value?.strongholds.find((s) => s.id === selectedStrongholdId.value) ?? null
);

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

/** 地图右下角：势力 / 封地 / 外交 着色模式。 */
const mapColorMode = ref<StrategyMapColorMode>("Realm");

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

/** 底栏情报：仅跟随鼠标悬停格（不用选中/固定悬浮格）。 */
const intelBarX = computed(() => hoverCell.value?.x ?? null);
const intelBarY = computed(() => hoverCell.value?.y ?? null);
const intelBarVisible = computed(() => intelBarX.value !== null && intelBarY.value !== null);

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

function entityCountAt(x: number, y: number): number {
  if (!state.value) return 0;
  return (
    cellEntities(state.value.strongholds, x, y).length +
    cellEntities(state.value.units, x, y).length +
    cellEntities(state.value.supplyConvoys, x, y).length +
    cellEntities(state.value.messengers, x, y).length
  );
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

function hasDualIntelLayout(x: number, y: number): boolean {
  if (!state.value) return false;
  const hasStronghold = cellEntities(state.value.strongholds, x, y).length > 0;
  const hasOther =
    cellEntities(state.value.units, x, y).length +
    cellEntities(state.value.supplyConvoys, x, y).length +
    cellEntities(state.value.messengers, x, y).length >
    0;
  return hasStronghold && hasOther;
}

function estimateHoverIntelPopupSize(): { width: number; height: number } {
  const el = hoverIntelLayerRef.value;
  if (el && el.offsetWidth > 0 && el.offsetHeight > 0) {
    return { width: el.offsetWidth, height: el.offsetHeight };
  }
  if (intelPinnedCell.value && hasDualIntelLayout(intelPinnedCell.value.x, intelPinnedCell.value.y)) {
    return { width: HOVER_INTEL_W * 2 + HOVER_INTEL_DUAL_GAP, height: HOVER_INTEL_H };
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
  const otherEl = layerEl?.querySelector(".intel-box--other") ?? null;
  const singleEl =
    layerEl?.querySelector(".cell-intel-stack > .intel-box:not(.intel-box--stronghold):not(.intel-box--other)") ??
    null;

  const dualLayout = Boolean(strongholdEl && otherEl);

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
    otherBoxScreen: rectToScreenDebug(otherEl),
    singleBoxScreen: dualLayout ? null : rectToScreenDebug(singleEl),
    placement,
    popupSize,
    dualLayout,
  });
}

watch([showHoverIntel, intelPinnedCell, () => popupMode.value], updateHoverIntelPosition, {
  deep: true,
});

function onViewportChange() {
  updateHoverIntelPosition();
}

const intelConvoy = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? cellEntity(state.value.supplyConvoys, intelX.value, intelY.value)
    : null
);

const intelMessenger = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? cellEntity(state.value.messengers, intelX.value, intelY.value)
    : null
);

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
  () => popupMode.value === "moveSelect" || popupMode.value === "attackSelect"
);

const cornerHintMode = computed((): "moveSelect" | "attackSelect" =>
  popupMode.value === "attackSelect" ? "attackSelect" : "moveSelect"
);

const menuPopupMode = computed(() => {
  const mode = popupMode.value;
  if (
    mode === "none" ||
    mode === "moveSelect" ||
    mode === "attackSelect"
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

function handleSelectUnit(payload: { unitId: number; screenX: number; screenY: number }) {
  logStrategyMapCoords("select-unit", payload);
  onSelectUnit(payload);
}

function handleSelectStronghold(payload: { strongholdId: number; screenX: number; screenY: number }) {
  logStrategyMapCoords("select-stronghold", payload);
  onSelectStronghold(payload);
}

function handleSelectConvoy(payload: { convoyId: number; screenX: number; screenY: number }) {
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
  pendingNotifications.value = [...pendingNotifications.value, notification];
}

function openSettlementDialog(event: StrategyEvent) {
  settlementDetail.value = parseEconomySettlementFromEvent(event);
  settlementDialogVisible.value = true;
}

function handleNotificationOpen(notification: StrategyPendingNotification) {
  pendingNotifications.value = pendingNotifications.value.filter(
    (item) => item.id !== notification.id
  );

  if (notification.kind === "battle" && notification.battleResult) {
    showResolvedBattle(notification.battleResult);
    return;
  }

  if (notification.kind === "economy" && notification.event) {
    openSettlementDialog(notification.event);
  }
}

function handlePopupCancel() {
  battlePreview.value = null;
  battleConfirmVisible.value = false;
  resetMovePath();
  onCancel();
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

function handleGlobalPointerDown(event: PointerEvent) {
  if (popupMode.value === "none") return;
  if (isInsideMapPopup(event.target)) return;
  if (isBlockingOverlayTarget(event.target)) return;
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

async function loadGame() {
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
    if (usingMockFallback.value) {
      info.value = "Live API 不可达，已自动使用 Mock 数据（见下方诊断面板）。";
    } else if (lastRequest.value?.source === "mock") {
      info.value = "当前为 Mock 模式。";
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : "加载剧本失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

function switchApiMode(mode: StrategyApiMode) {
  setApiMode(mode);
  loadGame();
}

function appendEvents(events: StrategyEvent[]) {
  if (!events.length) return;
  eventFeed.value = [...eventFeed.value, ...events].slice(-80);
  for (const evt of events) {
    const trayItem = notificationFromEvent(evt);
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
    if (response.resolvedBattles.length > 0) {
      for (const battle of response.resolvedBattles) {
        pushNotification(notificationFromBattle(battle));
      }
      info.value =
        response.resolvedBattles.length > 1
          ? `本日共 ${response.resolvedBattles.length} 场战斗已结算`
          : "战斗已结算";
    }
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
  setStrategySessionRecoveryHandler((recovered) => {
    state.value = recovered;
    selectedUnitId.value = null;
    selectedStrongholdId.value = null;
    selectedConvoyId.value = null;
    selectedCell.value = null;
    mapInteraction.reset();
    info.value = "WebApi 已重启，已自动重新加载 mini_kanto 剧本";
  });
  void loadGame();
});

onBeforeUnmount(() => {
  window.removeEventListener("pointerdown", handleGlobalPointerDown, true);
  window.removeEventListener("resize", updateHoverIntelPosition);
});
</script>

<template>
  <div class="strategy-page">
    <header class="strategy-toolbar">
      <div class="toolbar-left">
        <h2 class="title">{{ state?.map.name ?? "策略模式" }}</h2>
      </div>

      <div class="toolbar-right">
        <el-button-group>
          <el-button disabled title="M2-c 占位">⏸</el-button>
          <el-button type="primary" :loading="loading" @click="onAdvanceDay">▶ 推进 1 日</el-button>
        </el-button-group>
        <el-button :loading="loading" @click="loadGame">重新加载</el-button>
        <el-button :loading="loading" @click="onSaveGame">存档</el-button>
        <el-button :loading="loading" @click="onLoadSave">读档</el-button>
      </div>
    </header>

    <el-alert v-if="info" type="info" :title="info" show-icon :closable="false" class="error-bar" />
    <el-alert v-if="error" type="error" :title="error" show-icon :closable="false" class="error-bar" />

    <div class="strategy-body">
      <aside class="side-panel">
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
            <div class="map-message-zone">
              <StrategyMessageFeedToolbar
                v-model:show-player="showPlayerMessages"
                v-model:show-world="showWorldMessages"
                @open-dialog="messageDialogVisible = true"
              />
              <StrategyEventFeed :events="scopedEventFeed" />
            </div>
            <div class="map-top-status-float">
              <span class="date">{{ dateText }}</span>
              <template v-if="playerForce">
                <span class="force-name">{{ playerForce.name }}</span>
                <span>💰 {{ formatMoney(playerForce.money) }}</span>
                <span>🌾 {{ formatFoodGo(playerForce.food) }}</span>
              </template>
            </div>
          </div>

          <div
            v-if="state"
            class="map-overlay map-overlay--bottom"
            @pointerdown.stop
            @click.stop
            @wheel.stop
          >
            <div class="map-bottom-notify">
              <StrategyNotificationTray
                :notifications="pendingNotifications"
                @open="handleNotificationOpen"
              />
            </div>
            <div class="map-bottom-row" :class="{ 'map-bottom-row--no-intel': !intelBarVisible }">
              <StrategyIntelBar
                v-if="intelBarVisible"
                class="map-bottom-intel"
                :world-state="state"
                :x="intelBarX"
                :y="intelBarY"
                :terrain-name="intelTileInfo.terrainName"
                :region-name="intelTileInfo.regionName"
                :stronghold="intelBarStronghold"
                :landmark-name="intelLandmark?.name ?? null"
              />
              <StrategyMapViewControls v-model="mapColorMode" />
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
            @begin-move="handleBeginMove"
            @begin-attack="handleBeginAttack"
            @begin-directive="handleBeginDirective"
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
      @update:visible="battleResultVisible = $event"
    />
    <StrategyEconomySettlementDialog
      :visible="settlementDialogVisible"
      :detail="settlementDetail"
      @update:visible="settlementDialogVisible = $event"
    />
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
  height: calc(100vh - 120px);
  gap: 12px;
}

.strategy-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 12px 16px;
  background: #0f172a;
  color: #e2e8f0;
  border-radius: 8px;
}

.toolbar-left {
  display: flex;
  align-items: baseline;
  gap: 12px;
}

.title {
  margin: 0;
  font-size: 1.1rem;
}

.date {
  color: #94a3b8;
  font-size: 0.9rem;
}

.map-column {
  flex: 1;
  min-width: 0;
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
  flex: 1;
  min-width: 0;
  max-width: min(480px, 52vw);
  pointer-events: none;
}

.map-message-zone :deep(.message-feed-toolbar) {
  pointer-events: auto;
}

.map-top-status-float {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px 14px;
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

.map-top-status-float .force-name {
  font-weight: 600;
}

.map-overlay--bottom {
  bottom: 0;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
}

.map-bottom-notify {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  pointer-events: auto;
}

.map-bottom-row {
  display: flex;
  flex-direction: row;
  align-items: flex-end;
  gap: 10px;
  min-width: 0;
}

.map-bottom-row--no-intel {
  justify-content: flex-end;
}

.map-bottom-intel {
  flex: 1;
  min-width: 0;
  pointer-events: auto;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
}

.map-bottom-row :deep(.map-view-controls) {
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
</style>
