<script setup lang="ts">
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import {
  compactCellEntityRows,
  type CompactCellEntityEntry,
} from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  entries: CompactCellEntityEntry[];
}>();
</script>

<template>
  <div class="compact-stack">
    <template v-for="(entry, index) in entries" :key="entry.key">
      <div v-if="index > 0" class="entity-divider" role="separator" />
      <div class="entity-block">
        <StrategyIntelFieldList
          variant="hover"
          :rows="compactCellEntityRows(worldState, entry)"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.compact-stack {
  font-size: 0.78rem;
  line-height: 1.45;
  color: #e2e8f0;
}

.entity-divider {
  height: 0;
  margin: 10px 0;
  border: none;
  border-top: 1px solid #94a3b8;
  opacity: 0.85;
}
</style>
