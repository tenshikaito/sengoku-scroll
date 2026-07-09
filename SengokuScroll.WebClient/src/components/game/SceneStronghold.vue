<script setup lang="ts">
import { ref, onMounted } from "vue";
import { postCommand } from "@/api";

const mapInfo = ref({
  height: 0,
  width: 0,
  terrain: [],
  view: 0,
});

const init = async () => {
  console.log("init");
};

const canvas = ref<HTMLCanvasElement>();

const TILE_SIZE = 32;

const tileColors: Record<number, string> = {
  0: "#000",
  1: "#00f",
  2: "#0f0",
  3: "#ff0",
};

const drawTilemap = (ctx: CanvasRenderingContext2D) => {
  const mi = mapInfo.value!;
  for (let row = 0; row < mi.height; row++) {
    for (let col = 0; col < mi.width; col++) {
      const tileType = mi.terrain[row * mi.width + col];
      const tileColor = tileColors[tileType];
      ctx.fillStyle = tileColor;
      ctx.fillRect(col * TILE_SIZE, row * TILE_SIZE, TILE_SIZE, TILE_SIZE);
    }
  }
};

const getTileAtClick = (
  x: number,
  y: number
): { row: number; col: number; tileType: number | undefined } => {
  const mi = mapInfo.value!;
  const col = Math.floor(x / TILE_SIZE);
  const row = Math.floor(y / TILE_SIZE);
  return {
    row,
    col,
    tileType: col < mi.width ? mi.terrain[row * mi.width + col] : undefined,
  };
};

const handleClick = (event: MouseEvent) => {
  const x = event.offsetX;
  const y = event.offsetY;
  const { row, col, tileType } = getTileAtClick(x, y);
  if (tileType !== undefined) {
    console.log(`点击了瓦片 [${row}, ${col}]，瓦片类型: ${tileType}`);
  }
};

onMounted(async () => {
  const getMapInfoResp = await postCommand({
    name: "getMapInfo",
  });

  console.log(getMapInfoResp);

  mapInfo.value = getMapInfoResp.data;

  const ctx = canvas.value!.getContext("2d");
  drawTilemap(ctx!);
});

defineExpose({
  init,
  handleClick,
});
</script>

<template>
  <div>
    <div>Map Scene</div>
  </div>
  <div>
    <canvas ref="canvas" width="320" height="320" @click="handleClick"></canvas>
  </div>
  
</template>

<style scoped></style>
