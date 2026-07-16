<script setup lang="ts">
import { computed } from "vue";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

const props = defineProps<{
  rows: IntelFieldRow[];
}>();

const hasRows = computed(() => props.rows.length > 0);
</script>

<template>
  <StrategyIntelFieldList
    v-if="hasRows"
    :rows="rows"
    variant="dialog"
    :columns="2"
    dev-label-style="background"
  />
  <p v-else class="placeholder">请选择人物。</p>
</template>

<style scoped>
.placeholder {
  margin: 0;
  font-size: 0.85rem;
  color: #64748b;
}
</style>
