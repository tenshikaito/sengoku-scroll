<script setup lang="ts">
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";

defineProps<{
  worldState: StrategyWorldState;
  x: number | null;
  y: number | null;
  terrainName: string | null;
  regionName: string | null;
  /** 道路类型名（如「官道」）；无道路时为 null。 */
  roadName: string | null;
  /** 道路等级（与 typeId 一致）；无道路时为 null。 */
  roadLevel: number | null;
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
    <span v-if="stronghold" class="segment stronghold-segment">
      <span class="stronghold-name" :style="{ color: getForceColorCss(stronghold.forceId) }">
        🏯 {{ stronghold.name }}
      </span>
      · {{ forceName(worldState, stronghold.forceId) }}
    </span>
    <span v-if="roadName" class="segment road">
      🛤 {{ roadName }}<template v-if="roadLevel != null"> Lv.{{ roadLevel }}</template>
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

.stronghold-segment {
  display: inline-flex;
  align-items: baseline;
  gap: 0.35em;
  flex-wrap: wrap;
}

.terrain,
.road {
  color: #94a3b8;
}

.stronghold-name {
  font-weight: 600;
}

.landmark {
  color: #fcd34d;
}
</style>
