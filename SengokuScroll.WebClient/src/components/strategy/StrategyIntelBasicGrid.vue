<script setup lang="ts">
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

defineProps<{
  rows: IntelFieldRow[];
}>();
</script>

<template>
  <div class="basic-grid">
    <div v-for="(row, index) in rows" :key="`${row.label}-${index}`" class="basic-cell">
      <div class="basic-label">{{ row.label }}</div>
      <div class="basic-value">{{ row.value }}</div>
    </div>
  </div>
</template>

<style scoped>
.basic-grid {
  display: grid;
  gap: 8px;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

@media (min-width: 960px) {
  .basic-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

@media (min-width: 1280px) {
  .basic-grid {
    grid-template-columns: repeat(5, minmax(0, 1fr));
  }
}

.basic-cell {
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  overflow: hidden;
  min-width: 0;
}

.basic-label {
  padding: 5px 8px;
  font-size: 0.75rem;
  font-weight: 600;
  color: #f8fafc;
  background: #475569;
  line-height: 1.35;
}

.basic-value {
  padding: 7px 8px;
  font-size: 0.85rem;
  color: #0f172a;
  background: #fff;
  line-height: 1.45;
  word-break: break-word;
  min-height: 2.2em;
}
</style>
