<script setup lang="ts">
import { computed } from "vue";
import StrategyIntelBasicDescriptions from "./StrategyIntelBasicDescriptions.vue";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

const props = defineProps<{
  rows: IntelFieldRow[];
}>();

const hasRows = computed(() => props.rows.length > 0);

const coreRows = computed(() => props.rows.filter((row) => !row.dev));
const extraRows = computed(() => props.rows.filter((row) => row.dev));
</script>

<template>
  <div v-if="hasRows" class="person-stats-layout">
    <StrategyIntelBasicDescriptions :rows="coreRows" :column="1" class="person-stats-column" />
    <StrategyIntelBasicDescriptions
      v-if="extraRows.length"
      :rows="extraRows"
      :column="1"
      dev-label-style="background"
      class="person-stats-column"
    />
  </div>
  <p v-else class="placeholder">请选择人物。</p>
</template>

<style scoped>
.person-stats-layout {
  display: flex;
  align-items: flex-start;
  gap: 16px;
}

.person-stats-column {
  flex: 1;
  min-width: 0;
}

.placeholder {
  margin: 0;
  font-size: 0.85rem;
  color: #64748b;
}
</style>
