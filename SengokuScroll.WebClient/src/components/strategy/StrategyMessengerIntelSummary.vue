<script setup lang="ts">
import type { StrategyMessengerState, StrategyWorldState } from "@/api/strategy";
import { messengerHoverIntelRows } from "@/utils/strategyIntelRows";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";

defineProps<{
  worldState: StrategyWorldState;
  messenger: StrategyMessengerState;
}>();
</script>

<template>
  <div class="summary">
    <div class="name">📨 {{ messenger.name }}</div>
    <StrategyIntelFieldList variant="hover" :rows="messengerHoverIntelRows(worldState, messenger)" />
  </div>
</template>

<style scoped>
.summary {
  font-size: 0.78rem;
  line-height: 1.45;
  color: #e2e8f0;
}

.name {
  font-weight: 600;
  font-size: 0.88rem;
  margin-bottom: 6px;
}
</style>
