<script setup lang="ts">
import type { StrategySupplyConvoyState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { convoyHoverIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  convoy: StrategySupplyConvoyState;
}>();
</script>

<template>
  <div class="summary">
    <div class="name" :style="{ color: getForceColorCss(convoy.forceId) }">
      🌾 {{ convoy.name }}
    </div>
    <StrategyIntelFieldList variant="hover" :rows="convoyHoverIntelRows(worldState, convoy)" />
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
