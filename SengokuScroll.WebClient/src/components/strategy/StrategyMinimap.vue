<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import type { StrategyMapMasterState, StrategyWorldState } from "@/api/strategy";
import type { MapViewportWorldRect, MinimapNavigatePayload } from "./strategyMinimapTypes";
import { isPlayerRealmForce, resolveEntityMapColor, type StrategyMapColorMode } from "@/utils/mapEntityColors";
import { mapTileIndex } from "@/utils/mapTileLookup";
import { terrainFillColor } from "@/utils/terrainColors";
import { fogDisabled, isTileExplored, isTileVisible, resolveRoadCellStyle } from "@/utils/strategyFogCell";

const props = defineProps<{
  worldState: StrategyWorldState;
  mapMaster: StrategyMapMasterState;
  mapColorMode: StrategyMapColorMode;
  viewport: MapViewportWorldRect | null;
}>();

const emit = defineEmits<{
  navigate: [MinimapNavigatePayload];
}>();

const canvasRef = ref<HTMLCanvasElement | null>(null);

const MINIMAP_MAX_WIDTH = 200;
const MINIMAP_MAX_HEIGHT = 132;

const mapSize = computed(() => ({
  width: props.worldState.map.width,
  height: props.worldState.map.height,
}));

const layout = computed(() => {
  const { width, height } = mapSize.value;
  const scale = Math.min(MINIMAP_MAX_WIDTH / width, MINIMAP_MAX_HEIGHT / height);
  return {
    width: Math.max(1, Math.round(width * scale)),
    height: Math.max(1, Math.round(height * scale)),
    scale,
  };
});

function pixiColorToCss(color: number): string {
  return `#${(color & 0xffffff).toString(16).padStart(6, "0")}`;
}

function dimPixiColor(color: number, factor: number): string {
  const r = Math.floor(((color >> 16) & 0xff) * factor);
  const g = Math.floor(((color >> 8) & 0xff) * factor);
  const b = Math.floor((color & 0xff) * factor);
  return `rgb(${r}, ${g}, ${b})`;
}

function strongholdDotColor(stronghold: StrategyWorldState["strongholds"][number]): string {
  const isForeignKnown =
    stronghold.visibilityTier === "Known" &&
    !isPlayerRealmForce(
      stronghold.forceId,
      props.worldState.playerForceId,
      props.worldState.forces,
    );
  const color = isForeignKnown
    ? 0x6b7280
    : resolveEntityMapColor(stronghold.forceId, props.worldState, props.mapColorMode);
  return pixiColorToCss(color);
}

function shouldDrawStronghold(stronghold: StrategyWorldState["strongholds"][number]): boolean {
  if (fogDisabled(props.worldState)) return true;
  if (isPlayerRealmForce(stronghold.forceId, props.worldState.playerForceId, props.worldState.forces)) {
    return isTileExplored(props.worldState, stronghold.x, stronghold.y);
  }
  if (stronghold.visibilityTier === "Known") {
    return isTileExplored(props.worldState, stronghold.x, stronghold.y);
  }
  return isTileVisible(props.worldState, stronghold.x, stronghold.y);
}

function drawMinimap() {
  const canvas = canvasRef.value;
  if (!canvas) return;

  const { width: mapW, height: mapH } = mapSize.value;
  const { width, height, scale } = layout.value;
  const dpr = window.devicePixelRatio || 1;

  canvas.width = Math.round(width * dpr);
  canvas.height = Math.round(height * dpr);
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;

  const ctx = canvas.getContext("2d");
  if (!ctx) return;

  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#0f172a";
  ctx.fillRect(0, 0, width, height);

  const master = props.mapMaster;
  const roadSet = new Set((master.roadCells ?? []).map((r) => `${r.x},${r.y}`));

  for (let y = 0; y < mapH; y++) {
    for (let x = 0; x < mapW; x++) {
      const px = x * scale;
      const py = y * scale;
      const cellW = x === mapW - 1 ? width - px : scale;
      const cellH = y === mapH - 1 ? height - py : scale;

      const explored = isTileExplored(props.worldState, x, y);
      if (!explored) {
        ctx.fillStyle = "#0a0a12";
        ctx.fillRect(px, py, cellW, cellH);
        continue;
      }

      const terrainId = master.terrainIds[mapTileIndex(master, x, y)] ?? 0;
      const visible = isTileVisible(props.worldState, x, y);
      const base = terrainFillColor(terrainId, x, y);
      ctx.fillStyle = visible ? pixiColorToCss(base) : dimPixiColor(base, 0.42);

      ctx.fillRect(px, py, cellW, cellH);
    }
  }

  ctx.lineCap = "round";
  ctx.lineWidth = Math.max(1.2, scale * 0.22);

  const roadDirections: Array<[number, number]> = [
    [1, 0],
    [-1, 0],
    [0, 1],
    [0, -1],
  ];

  const drawRoadSegments = (targetStyle: "bright" | "fog", strokeStyle: string) => {
    ctx.strokeStyle = strokeStyle;
    for (let y = 0; y < mapH; y++) {
      for (let x = 0; x < mapW; x++) {
        if (!roadSet.has(`${x},${y}`)) continue;
        const style = resolveRoadCellStyle(props.worldState, x, y);
        if (style !== targetStyle) continue;

        const cx = (x + 0.5) * scale;
        const cy = (y + 0.5) * scale;

        for (const [dx, dy] of roadDirections) {
          if (!roadSet.has(`${x + dx},${y + dy}`)) continue;

          let tx = cx;
          let ty = cy;
          if (dx === 1) tx = (x + 1) * scale;
          else if (dx === -1) tx = x * scale;
          else if (dy === 1) ty = (y + 1) * scale;
          else ty = y * scale;

          ctx.beginPath();
          ctx.moveTo(cx, cy);
          ctx.lineTo(tx, ty);
          ctx.stroke();
        }
      }
    }
  };

  drawRoadSegments("fog", "rgba(120, 113, 108, 0.72)");
  drawRoadSegments("bright", "rgba(250, 204, 21, 0.92)");

  const dotRadius = Math.max(2.2, scale * 0.42);
  for (const stronghold of props.worldState.strongholds) {
    if (!shouldDrawStronghold(stronghold)) continue;

    const cx = (stronghold.x + 0.5) * scale;
    const cy = (stronghold.y + 0.5) * scale;

    ctx.beginPath();
    ctx.arc(cx, cy, dotRadius + 0.8, 0, Math.PI * 2);
    ctx.fillStyle = "rgba(15, 23, 42, 0.85)";
    ctx.fill();

    ctx.beginPath();
    ctx.arc(cx, cy, dotRadius, 0, Math.PI * 2);
    ctx.fillStyle = strongholdDotColor(stronghold);
    ctx.fill();
    ctx.strokeStyle = "rgba(255, 255, 255, 0.92)";
    ctx.lineWidth = Math.max(0.8, scale * 0.12);
    ctx.stroke();
  }

  const viewport = props.viewport;
  if (viewport && viewport.mapWidthPx > 0 && viewport.mapHeightPx > 0) {
    const vx = (viewport.x / viewport.mapWidthPx) * width;
    const vy = (viewport.y / viewport.mapHeightPx) * height;
    const vw = (viewport.width / viewport.mapWidthPx) * width;
    const vh = (viewport.height / viewport.mapHeightPx) * height;

    ctx.fillStyle = "rgba(148, 163, 184, 0.22)";
    ctx.fillRect(vx, vy, vw, vh);
    ctx.strokeStyle = "rgba(203, 213, 225, 0.95)";
    ctx.lineWidth = 1.25;
    ctx.strokeRect(vx + 0.5, vy + 0.5, Math.max(0, vw - 1), Math.max(0, vh - 1));
  }
}

function minimapPointToWorld(clientX: number, clientY: number): MinimapNavigatePayload | null {
  const canvas = canvasRef.value;
  if (!canvas) return null;

  const rect = canvas.getBoundingClientRect();
  const localX = clientX - rect.left;
  const localY = clientY - rect.top;
  const { width, height } = layout.value;
  if (localX < 0 || localY < 0 || localX > width || localY > height) return null;

  const viewport = props.viewport;
  if (!viewport) return null;

  return {
    worldX: (localX / width) * viewport.mapWidthPx,
    worldY: (localY / height) * viewport.mapHeightPx,
  };
}

function onMinimapPointerDown(event: PointerEvent) {
  event.stopPropagation();
  const target = minimapPointToWorld(event.clientX, event.clientY);
  if (target) emit("navigate", target);
}

watch(
  () =>
    [
      props.worldState,
      props.mapMaster,
      props.mapColorMode,
      props.viewport,
      layout.value.width,
      layout.value.height,
    ] as const,
  () => drawMinimap(),
  { deep: true },
);

onMounted(() => drawMinimap());
onBeforeUnmount(() => {});
</script>

<template>
  <div class="strategy-minimap" @pointerdown.stop @click.stop @wheel.stop>
    <canvas
      ref="canvasRef"
      class="strategy-minimap__canvas"
      title="点击小地图移动视口"
      @pointerdown="onMinimapPointerDown"
    />
  </div>
</template>

<style scoped>
.strategy-minimap {
  width: 100%;
  display: flex;
  justify-content: center;
  padding-top: 2px;
}

.strategy-minimap__canvas {
  display: block;
  border-radius: 4px;
  border: 1px solid rgba(100, 116, 139, 0.55);
  cursor: crosshair;
  image-rendering: pixelated;
  image-rendering: crisp-edges;
}
</style>
