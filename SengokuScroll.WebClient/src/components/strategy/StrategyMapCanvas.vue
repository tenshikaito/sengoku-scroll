<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from "vue";
import { Application, Container, Graphics, Text } from "pixi.js";
import type { StrategyWorldState } from "@/api/strategy";
import type { MapRouteOverlay, MapMoveRelayMarker } from "./mapRouteStyles";
import { MOVE_RELAY_MARKER_STYLES, ROUTE_STYLES } from "./mapRouteStyles";
import { getForceColor } from "./forceColors";
import { resolveEntityMapColor, type StrategyMapColorMode } from "@/utils/mapEntityColors";
import { logStrategyMapCoords } from "@/utils/strategyMapDebug";

const TILE_SIZE = 48;

const props = defineProps<{
  worldState: StrategyWorldState | null;
  selectedUnitId: number | null;
  selectedStrongholdId?: number | null;
  selectedConvoyId?: number | null;
  hoverUnitId?: number | null;
  hoverStrongholdId?: number | null;
  hoverConvoyId?: number | null;
  selectedCell: { x: number; y: number } | null;
  routeOverlays?: MapRouteOverlay[];
  moveRelayMarkers?: MapMoveRelayMarker[];
  mapUnitSelectionEnabled?: boolean;
  mapStrongholdSelectionEnabled?: boolean;
  mapConvoySelectionEnabled?: boolean;
  mapCellSelectionEnabled?: boolean;
  /** 为 true 时不向地图传递悬停（如情报对话框打开时）。 */
  mapHoverSuppressed?: boolean;
  /** 地图实体着色模式：势力 / 封地 / 外交。 */
  mapColorMode?: StrategyMapColorMode;
}>();

const emit = defineEmits<{
  selectUnit: [payload: { unitId: number; screenX: number; screenY: number }];
  selectStronghold: [payload: { strongholdId: number; screenX: number; screenY: number }];
  selectConvoy: [payload: { convoyId: number; screenX: number; screenY: number }];
  selectCell: [payload: { x: number; y: number; screenX: number; screenY: number }];
  hoverCell: [cell: { x: number; y: number; screenX: number; screenY: number } | null];
  viewportChange: [];
}>();

const hostRef = ref<HTMLDivElement | null>(null);

let app: Application | null = null;
let worldContainer: Container | null = null;
let mapLayer: Container | null = null;
let pathLayer: Container | null = null;
let entityLayer: Container | null = null;
let highlightLayer: Container | null = null;

let zoom = 1;
let isPanning = false;
let panStart = { x: 0, y: 0 };
let containerStart = { x: 0, y: 0 };
let didPan = false;
let pointerDownOnCanvas = false;
let activePointerId: number | null = null;

function resetPointerGesture() {
  isPanning = false;
  pointerDownOnCanvas = false;
  activePointerId = null;
}

function isInsideMap(x: number, y: number): boolean {
  const map = props.worldState?.map;
  if (!map) return false;
  return x >= 0 && y >= 0 && x < map.width && y < map.height;
}

function cellCenter(x: number, y: number) {
  return {
    x: x * TILE_SIZE + TILE_SIZE / 2,
    y: y * TILE_SIZE + TILE_SIZE / 2,
  };
}

function screenToCell(clientX: number, clientY: number): { x: number; y: number } | null {
  if (!app || !worldContainer || !hostRef.value) return null;

  const rect = hostRef.value.getBoundingClientRect();
  const local = worldContainer.toLocal({
    x: clientX - rect.left,
    y: clientY - rect.top,
  });

  const x = Math.floor(local.x / TILE_SIZE);
  const y = Math.floor(local.y / TILE_SIZE);
  return isInsideMap(x, y) ? { x, y } : null;
}

function unitAtCell(x: number, y: number): number | null {
  const unit = props.worldState?.units.find((u) => u.x === x && u.y === y);
  return unit?.id ?? null;
}

function strongholdAtCell(x: number, y: number): number | null {
  const stronghold = props.worldState?.strongholds.find((s) => s.x === x && s.y === y);
  return stronghold?.id ?? null;
}

function convoyAtCell(x: number, y: number): number | null {
  const convoy = props.worldState?.supplyConvoys.find((c) => c.x === x && c.y === y);
  return convoy?.id ?? null;
}

function getCellPanelRect(x: number, y: number, panelEl?: HTMLElement | null) {
  if (!worldContainer || !hostRef.value) return null;

  const panel = panelEl ?? hostRef.value;
  const hostRect = hostRef.value.getBoundingClientRect();
  const panelRect = panel.getBoundingClientRect();

  const topLeft = worldContainer.toGlobal({ x: x * TILE_SIZE, y: y * TILE_SIZE });
  const bottomRight = worldContainer.toGlobal({
    x: (x + 1) * TILE_SIZE,
    y: (y + 1) * TILE_SIZE,
  });

  // toGlobal 与 screenToCell 一致：相对 canvas-host 左上角的 CSS 像素，非 window 坐标
  const hostOffsetInPanel = {
    left: hostRect.left - panelRect.left,
    top: hostRect.top - panelRect.top,
  };

  const rect = {
    left: topLeft.x + hostOffsetInPanel.left,
    top: topLeft.y + hostOffsetInPanel.top,
    width: bottomRight.x - topLeft.x,
    height: bottomRight.y - topLeft.y,
  };

  logStrategyMapCoords("cell-panel-rect", {
    cell: { x, y },
    tileSize: TILE_SIZE,
    anchor: panelEl ? "map-panel" : "canvas-host",
    hostOffsetInPanel,
    rect,
    zoom,
    worldContainer: { x: worldContainer.x, y: worldContainer.y },
  });

  return rect;
}

function notifyViewportChange() {
  emit("viewportChange");
}

function fitMapToView() {
  if (!app || !worldContainer || !props.worldState) return;

  const { width, height } = props.worldState.map;
  const mapW = width * TILE_SIZE;
  const mapH = height * TILE_SIZE;
  const padding = 24;

  zoom = Math.min(
    (app.screen.width - padding * 2) / mapW,
    (app.screen.height - padding * 2) / mapH,
    1.5
  );
  zoom = Math.max(zoom, 0.4);

  worldContainer.scale.set(zoom);
  worldContainer.position.set(
    (app.screen.width - mapW * zoom) / 2,
    (app.screen.height - mapH * zoom) / 2
  );
}

function drawMap() {
  if (!mapLayer || !props.worldState?.map) return;

  mapLayer.removeChildren();

  const { width, height } = props.worldState.map;
  const roadSet = new Set(
    (props.worldState.map.roadCells ?? []).map((r) => `${r.x},${r.y}`)
  );

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const tile = new Graphics();
      const hasRoad = roadSet.has(`${x},${y}`);
      const base = hasRoad ? 0x78716c : (x + y) % 2 === 0 ? 0x5a7a1e : 0x6b8e23;
      tile
        .rect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE)
        .fill(base)
        .stroke({ width: 1, color: hasRoad ? 0xa8a29e : 0x3d5229, alpha: 0.6 });
      mapLayer.addChild(tile);

      if (hasRoad) {
        const roadMark = new Graphics();
        roadMark
          .rect(x * TILE_SIZE + 10, y * TILE_SIZE + TILE_SIZE / 2 - 2, TILE_SIZE - 20, 4)
          .fill({ color: 0xd6d3d1, alpha: 0.85 });
        mapLayer.addChild(roadMark);
      }
    }
  }

  for (const landmark of props.worldState.map.landmarks ?? []) {
    if (!isInsideMap(landmark.x, landmark.y)) continue;

    const cx = landmark.x * TILE_SIZE + TILE_SIZE / 2;
    const cy = landmark.y * TILE_SIZE + TILE_SIZE * 0.22;
    const pin = new Graphics();
    pin
      .moveTo(cx, cy - 9)
      .lineTo(cx + 8, cy + 2)
      .lineTo(cx, cy + 7)
      .lineTo(cx - 8, cy + 2)
      .closePath()
      .fill({ color: 0xfbbf24, alpha: 0.98 })
      .stroke({ width: 2, color: 0xffffff, alpha: 0.95 });
    mapLayer.addChild(pin);

    const label = new Text({
      text: "◆",
      style: {
        fontSize: 14,
        fill: 0xfff7ed,
        fontFamily: "sans-serif",
        fontWeight: "700",
        stroke: { color: 0x78350f, width: 2 },
      },
    });
    label.anchor.set(0.5);
    label.position.set(cx, cy - 1);
    mapLayer.addChild(label);
  }
}

function entityMapColor(forceId: number): number {
  if (!props.worldState) return getForceColor(forceId);
  return resolveEntityMapColor(forceId, props.worldState, props.mapColorMode ?? "Force");
}

function drawEntities() {
  if (!entityLayer || !props.worldState) return;

  entityLayer.removeChildren();

  for (const stronghold of props.worldState.strongholds) {
    const color = entityMapColor(stronghold.forceId);
    const px = stronghold.x * TILE_SIZE + 6;
    const py = stronghold.y * TILE_SIZE + 6;
    const size = TILE_SIZE - 12;
    const selected = props.selectedStrongholdId === stronghold.id;
    const hovered = props.hoverStrongholdId === stronghold.id;

    if (hovered && !selected) {
      const glow = new Graphics();
      glow
        .rect(px - 3, py - 3, size + 6, size + 6)
        .stroke({ width: 2, color: 0x38bdf8, alpha: 0.95 });
      entityLayer.addChild(glow);
    }

    const block = new Graphics();
    block
      .rect(px, py, size, size)
      .fill({ color, alpha: 0.85 })
      .stroke({
        width: selected ? 3 : hovered ? 2.5 : 2,
        color: selected ? 0xfbbf24 : hovered ? 0x38bdf8 : 0xffffff,
        alpha: 0.95,
      });
    entityLayer.addChild(block);

    const label = new Text({
      text: stronghold.name,
      style: { fontSize: 11, fill: 0xffffff, fontFamily: "sans-serif" },
    });
    label.anchor.set(0.5);
    label.position.set(stronghold.x * TILE_SIZE + TILE_SIZE / 2, stronghold.y * TILE_SIZE + TILE_SIZE / 2);
    entityLayer.addChild(label);
  }

  for (const unit of props.worldState.units) {
    const color = entityMapColor(unit.forceId);
    const center = cellCenter(unit.x, unit.y);
    const radius = TILE_SIZE * 0.28;
    const selected = props.selectedUnitId === unit.id;
    const hovered = props.hoverUnitId === unit.id;

    if (hovered && !selected) {
      const glow = new Graphics();
      glow
        .circle(center.x, center.y, radius + 6)
        .stroke({ width: 2, color: 0x38bdf8, alpha: 0.95 });
      entityLayer.addChild(glow);
    }

    const marker = new Graphics();
    marker.circle(center.x, center.y, radius).fill(color).stroke({
      width: selected ? 3 : hovered ? 2.5 : 2,
      color: selected ? 0xfbbf24 : hovered ? 0x38bdf8 : 0xffffff,
    });
    entityLayer.addChild(marker);

    const badge = new Text({
      text: String(unit.soldiers),
      style: { fontSize: 12, fill: 0xffffff, fontWeight: "bold", fontFamily: "sans-serif" },
    });
    badge.anchor.set(0.5);
    badge.position.set(center.x, center.y);
    entityLayer.addChild(badge);
  }

  for (const convoy of props.worldState.supplyConvoys) {
    const color = entityMapColor(convoy.forceId);
    const cx = convoy.x * TILE_SIZE + TILE_SIZE * 0.72;
    const cy = convoy.y * TILE_SIZE + TILE_SIZE * 0.28;
    const size = TILE_SIZE * 0.22;
    const selected = props.selectedConvoyId === convoy.id;
    const hovered = props.hoverConvoyId === convoy.id;

    if (hovered && !selected) {
      const glow = new Graphics();
      glow
        .rect(cx - size / 2 - 3, cy - size / 2 - 3, size + 6, size + 6)
        .stroke({ width: 2, color: 0x38bdf8, alpha: 0.95 });
      entityLayer.addChild(glow);
    }

    const marker = new Graphics();
    marker
      .rect(cx - size / 2, cy - size / 2, size, size)
      .fill(convoy.isReturningToOrigin ? 0x94a3b8 : 0xfbbf24)
      .stroke({
        width: selected ? 2.5 : hovered ? 2 : 1.5,
        color: selected ? 0xfbbf24 : hovered ? 0x38bdf8 : color,
        alpha: 0.95,
      });
    entityLayer.addChild(marker);

    const label = new Text({
      text: "🌾",
      style: { fontSize: 10, fontFamily: "sans-serif" },
    });
    label.anchor.set(0.5);
    label.position.set(cx, cy);
    entityLayer.addChild(label);
  }

  for (const messenger of props.worldState.messengers) {
    const color = entityMapColor(messenger.forceId);
    const cx = messenger.x * TILE_SIZE + TILE_SIZE * 0.28;
    const cy = messenger.y * TILE_SIZE + TILE_SIZE * 0.72;
    const r = TILE_SIZE * 0.14;

    const marker = new Graphics();
    marker.circle(cx, cy, r).fill(0xfef3c7).stroke({ width: 1.5, color, alpha: 0.95 });
    entityLayer.addChild(marker);

    const label = new Text({
      text: "📨",
      style: { fontSize: 9, fontFamily: "sans-serif" },
    });
    label.anchor.set(0.5);
    label.position.set(cx, cy);
    entityLayer.addChild(label);
  }
}

function drawRoutes() {
  if (!pathLayer) return;

  pathLayer.removeChildren();

  for (const route of props.routeOverlays ?? []) {
    if (route.points.length < 2) continue;

    const style = ROUTE_STYLES[route.variant];

    for (const point of route.points) {
      if (!isInsideMap(point.x, point.y)) continue;

      const tile = new Graphics();
      tile
        .rect(point.x * TILE_SIZE + 3, point.y * TILE_SIZE + 3, TILE_SIZE - 6, TILE_SIZE - 6)
        .fill({ color: style.fill, alpha: style.fillAlpha });
      pathLayer.addChild(tile);
    }

    const line = new Graphics();
    const start = cellCenter(route.points[0]!.x, route.points[0]!.y);
    line.moveTo(start.x, start.y);
    for (let i = 1; i < route.points.length; i++) {
      const c = cellCenter(route.points[i]!.x, route.points[i]!.y);
      line.lineTo(c.x, c.y);
    }
    line.stroke({ width: style.width, color: style.stroke, alpha: style.strokeAlpha });
    pathLayer.addChild(line);
  }
}

function drawHighlights() {
  if (!highlightLayer) return;

  highlightLayer.removeChildren();

  const relayAtSelected =
    props.selectedCell &&
    (props.moveRelayMarkers ?? []).some(
      (m) => m.x === props.selectedCell!.x && m.y === props.selectedCell!.y
    );

  if (
    props.selectedCell &&
    isInsideMap(props.selectedCell.x, props.selectedCell.y) &&
    !relayAtSelected
  ) {
    const ring = new Graphics();
    ring
      .rect(
        props.selectedCell.x * TILE_SIZE + 2,
        props.selectedCell.y * TILE_SIZE + 2,
        TILE_SIZE - 4,
        TILE_SIZE - 4
      )
      .stroke({ width: 2, color: 0xfbbf24, alpha: 0.95 });
    highlightLayer.addChild(ring);
  }

  for (const marker of props.moveRelayMarkers ?? []) {
    if (!isInsideMap(marker.x, marker.y)) continue;

    const style = MOVE_RELAY_MARKER_STYLES[marker.kind];
    const cx = marker.x * TILE_SIZE + TILE_SIZE / 2;
    const cy = marker.y * TILE_SIZE + TILE_SIZE / 2;
    const isPending = marker.kind === "pending";
    const radius = isPending ? TILE_SIZE * 0.24 : TILE_SIZE * 0.2;

    const badge = new Graphics();
    badge
      .circle(cx, cy, radius)
      .fill({ color: style.fill, alpha: 0.96 })
      .stroke({ width: isPending ? 2.5 : 2, color: style.stroke, alpha: 1 });
    highlightLayer.addChild(badge);

    if (isPending) {
      const outer = new Graphics();
      outer
        .rect(
          marker.x * TILE_SIZE + 2,
          marker.y * TILE_SIZE + 2,
          TILE_SIZE - 4,
          TILE_SIZE - 4
        )
        .stroke({ width: 2, color: 0x22d3ee, alpha: 0.85 });
      highlightLayer.addChild(outer);
    }

    const labelText = style.label ?? String(marker.order);
    const label = new Text({
      text: labelText,
      style: {
        fontSize: isPending ? 12 : 11,
        fontFamily: "sans-serif",
        fontWeight: "700",
        fill: 0x0f172a,
      },
    });
    label.anchor.set(0.5);
    label.position.set(cx, cy + (isPending ? 0 : 0.5));
    highlightLayer.addChild(label);
  }
}

function redraw() {
  drawMap();
  drawRoutes();
  drawEntities();
  drawHighlights();
}

function onWheel(event: WheelEvent) {
  if (!app || !worldContainer || !hostRef.value) return;

  event.preventDefault();
  const rect = hostRef.value.getBoundingClientRect();
  const cursorX = event.clientX - rect.left;
  const cursorY = event.clientY - rect.top;

  const factor = event.deltaY > 0 ? 0.9 : 1.1;
  const nextZoom = Math.min(Math.max(zoom * factor, 0.35), 2.5);
  const worldPos = {
    x: (cursorX - worldContainer.x) / zoom,
    y: (cursorY - worldContainer.y) / zoom,
  };

  zoom = nextZoom;
  worldContainer.scale.set(zoom);
  worldContainer.position.set(cursorX - worldPos.x * zoom, cursorY - worldPos.y * zoom);
  notifyViewportChange();
}

function onPointerDown(event: PointerEvent) {
  if (!worldContainer || !app?.canvas) return;

  pointerDownOnCanvas = event.target === app.canvas;
  if (!pointerDownOnCanvas) return;

  activePointerId = event.pointerId;

  if (event.button === 1 || event.button === 2 || event.altKey) {
    isPanning = true;
    didPan = false;
    panStart = { x: event.clientX, y: event.clientY };
    containerStart = { x: worldContainer.x, y: worldContainer.y };
    return;
  }

  if (event.button === 0) {
    didPan = false;
    isPanning = true;
    panStart = { x: event.clientX, y: event.clientY };
    containerStart = { x: worldContainer.x, y: worldContainer.y };
  }
}

function shouldSuppressMapHover(event: PointerEvent): boolean {
  if (props.mapHoverSuppressed) return true;
  const target = event.target;
  if (!(target instanceof Element)) return false;
  return Boolean(target.closest(".el-overlay, .el-message-box"));
}

function onPointerMove(event: PointerEvent) {
  if (isPanning && worldContainer) {
    const dx = event.clientX - panStart.x;
    const dy = event.clientY - panStart.y;

    if (Math.hypot(dx, dy) > 4) {
      didPan = true;
    }

    worldContainer.position.set(containerStart.x + dx, containerStart.y + dy);
    return;
  }

  if (shouldSuppressMapHover(event)) {
    emit("hoverCell", null);
    return;
  }

  const cell = screenToCell(event.clientX, event.clientY);
  emit(
    "hoverCell",
    cell ? { x: cell.x, y: cell.y, screenX: event.clientX, screenY: event.clientY } : null
  );
}

function onPointerLeave() {
  emit("hoverCell", null);
}

function onPointerUp(event: PointerEvent) {
  const startedOnCanvas =
    pointerDownOnCanvas && activePointerId !== null && event.pointerId === activePointerId;

  if (startedOnCanvas && !didPan && event.button === 0) {
    const cell = screenToCell(event.clientX, event.clientY);
    if (cell) {
      const unitId = unitAtCell(cell.x, cell.y);
      const strongholdId = strongholdAtCell(cell.x, cell.y);
      const convoyId = convoyAtCell(cell.x, cell.y);

      if (props.mapUnitSelectionEnabled && unitId !== null) {
        emit("selectUnit", { unitId, screenX: event.clientX, screenY: event.clientY });
      } else if (props.mapStrongholdSelectionEnabled && strongholdId !== null) {
        emit("selectStronghold", {
          strongholdId,
          screenX: event.clientX,
          screenY: event.clientY,
        });
      } else if (props.mapConvoySelectionEnabled && convoyId !== null) {
        emit("selectConvoy", {
          convoyId,
          screenX: event.clientX,
          screenY: event.clientY,
        });
      } else if (props.mapCellSelectionEnabled) {
        emit("selectCell", { x: cell.x, y: cell.y, screenX: event.clientX, screenY: event.clientY });
      }
    }
  }

  if (startedOnCanvas && didPan) {
    notifyViewportChange();
  }

  resetPointerGesture();
}

async function initPixi() {
  if (!hostRef.value) return;

  app = new Application();
  await app.init({
    background: "#1a1a2e",
    resizeTo: hostRef.value,
    antialias: true,
  });

  hostRef.value.appendChild(app.canvas);

  worldContainer = new Container();
  mapLayer = new Container();
  pathLayer = new Container();
  entityLayer = new Container();
  highlightLayer = new Container();

  worldContainer.addChild(mapLayer, pathLayer, entityLayer, highlightLayer);
  app.stage.addChild(worldContainer);

  app.canvas.addEventListener("wheel", onWheel, { passive: false });
  app.canvas.addEventListener("pointerdown", onPointerDown);
  app.canvas.addEventListener("pointermove", onPointerMove);
  app.canvas.addEventListener("pointerleave", onPointerLeave);
  window.addEventListener("pointermove", onPointerMove);
  window.addEventListener("pointerup", onPointerUp);
  app.canvas.addEventListener("contextmenu", (e) => e.preventDefault());

  fitMapToView();
  redraw();
}

function destroyPixi() {
  if (app?.canvas) {
    app.canvas.removeEventListener("wheel", onWheel);
    app.canvas.removeEventListener("pointerdown", onPointerDown);
    app.canvas.removeEventListener("pointermove", onPointerMove);
    app.canvas.removeEventListener("pointerleave", onPointerLeave);
    window.removeEventListener("pointermove", onPointerMove);
    window.removeEventListener("pointerup", onPointerUp);
  }

  app?.destroy(true, { children: true });
  app = null;
  worldContainer = null;
  mapLayer = null;
  pathLayer = null;
  entityLayer = null;
  highlightLayer = null;
}

watch(
  () => props.worldState,
  () => {
    redraw();
  },
  { deep: true }
);

watch(
  () =>
    [
      props.worldState?.scenarioId,
      props.worldState?.map.width,
      props.worldState?.map.height,
    ] as const,
  () => {
    fitMapToView();
    redraw();
  }
);

watch(
  () =>
    [
      props.selectedUnitId,
      props.selectedStrongholdId,
      props.hoverUnitId,
      props.hoverStrongholdId,
      props.selectedCell,
      props.routeOverlays,
      props.moveRelayMarkers,
      props.mapColorMode,
    ] as const,
  () => {
    drawEntities();
    drawRoutes();
    drawHighlights();
  },
  { deep: true }
);

function containsPointerTarget(target: EventTarget | null): boolean {
  if (!hostRef.value || !(target instanceof Node)) return false;
  return hostRef.value.contains(target);
}

defineExpose({ getCellPanelRect, containsPointerTarget });

onMounted(initPixi);
onBeforeUnmount(destroyPixi);
</script>

<template>
  <div ref="hostRef" class="strategy-map-host" />
</template>

<style scoped>
.strategy-map-host {
  width: 100%;
  height: 100%;
  min-height: 0;
  border-radius: 8px;
  overflow: hidden;
  touch-action: none;
}
</style>
