<script setup lang="ts">
import { computed } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";
import { directiveLabel } from "@/utils/unitDirective";
import { unitStatusLabel } from "@/utils/strategyUnitLabels";
import { listOperableUnits } from "@/utils/strategyOperableUnits";

const props = defineProps<{
  worldState: StrategyWorldState;
  selectedUnitId: number | null;
}>();

const emit = defineEmits<{
  select: [unitId: number, event: MouseEvent];
}>();

const entries = computed(() => listOperableUnits(props.worldState));

function onSelect(unitId: number, event: MouseEvent) {
  emit("select", unitId, event);
}
</script>

<template>
  <aside class="unit-roster-panel">
    <h3>可操作部队</h3>
    <p v-if="!entries.length" class="empty">当前无本家可操作部队</p>
    <ul v-else class="unit-roster-list">
      <li
        v-for="entry in entries"
        :key="entry.unit.id"
        :class="[
          'unit-roster-item',
          { selected: selectedUnitId === entry.unit.id, offmap: entry.kind === 'roster' },
        ]"
        @click="onSelect(entry.unit.id, $event)"
      >
        <div class="unit-roster-name" :style="{ color: getForceColorCss(entry.unit.forceId) }">
          {{ entry.unit.name }}
          <span v-if="entry.kind === 'roster'" class="offmap-tag">视野外</span>
        </div>
        <div class="unit-roster-meta">
          <span>{{ formatSoldiers(entry.unit.soldiers) }}</span>
          <span>{{ unitStatusLabel(entry.unit.status) }}</span>
        </div>
        <div class="unit-roster-meta subtle">
          {{ directiveLabel(entry.unit.directive) }}
          <template v-if="entry.unit.commanderName"> · {{ entry.unit.commanderName }}</template>
        </div>
      </li>
    </ul>
  </aside>
</template>

<style scoped>
.unit-roster-panel {
  display: flex;
  flex-direction: column;
  min-height: 0;
  max-height: inherit;
  padding: 12px;
  background: rgba(15, 23, 42, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.35);
  border-radius: 10px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
  color: #e2e8f0;
  box-sizing: border-box;
}

.unit-roster-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.unit-roster-panel h3 {
  margin: 0 0 10px;
  font-size: 0.95rem;
  flex-shrink: 0;
}

.empty {
  margin: 0;
  font-size: 0.82rem;
  color: #94a3b8;
}

.unit-roster-item {
  padding: 8px 10px;
  border: 1px solid rgba(148, 163, 184, 0.25);
  border-radius: 8px;
  cursor: pointer;
  background: rgba(30, 41, 59, 0.65);
}

.unit-roster-item:hover {
  border-color: #38bdf8;
}

.unit-roster-item.selected {
  border-color: #fbbf24;
  box-shadow: inset 0 0 0 1px rgba(251, 191, 36, 0.35);
}

.unit-roster-item.offmap {
  opacity: 0.82;
}

.unit-roster-name {
  font-weight: 600;
  font-size: 0.88rem;
  display: flex;
  align-items: center;
  gap: 6px;
}

.offmap-tag {
  font-size: 0.68rem;
  font-weight: 500;
  color: #94a3b8;
  border: 1px solid rgba(148, 163, 184, 0.45);
  border-radius: 999px;
  padding: 0 6px;
}

.unit-roster-meta {
  margin-top: 4px;
  font-size: 0.78rem;
  display: flex;
  gap: 8px;
}

.unit-roster-meta.subtle {
  color: #94a3b8;
}
</style>
