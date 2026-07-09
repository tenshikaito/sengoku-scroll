<script setup lang="ts">
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";

defineProps<{
  worldState: StrategyWorldState;
  x: number;
  y: number;
  terrainName: string | null;
  regionName: string | null;
  stronghold: StrategyStrongholdState | null;
  landmarkName: string | null;
}>();

function forceName(worldState: StrategyWorldState, forceId: number) {
  return worldState.forces.find((f) => f.id === forceId)?.name ?? "未知势力";
}
</script>

<template>
  <footer class="intel-bar">
    <span v-if="terrainName || regionName" class="segment terrain">
      <template v-if="terrainName">{{ terrainName }}</template>
      <template v-if="regionName">
        <template v-if="terrainName"> · </template>{{ regionName }}
      </template>
    </span>
    <span v-if="stronghold" class="segment">
      🏯
      <strong :style="{ color: getForceColorCss(stronghold.forceId) }">{{ stronghold.name }}</strong>
      · {{ forceName(worldState, stronghold.forceId) }}
    </span>
    <span v-if="landmarkName" class="segment landmark">
      📍 {{ landmarkName }}
    </span>
  </footer>
</template>

<style scoped>
.intel-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 20px;
  min-height: 36px;
  padding: 8px 14px;
  background: rgba(15, 23, 42, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.35);
  color: #e2e8f0;
  font-size: 0.85rem;
  flex-shrink: 0;
  border-radius: 10px;
  backdrop-filter: blur(6px);
}

.segment {
  white-space: nowrap;
}

.terrain {
  color: #94a3b8;
}

.landmark {
  color: #fcd34d;
}
</style>
