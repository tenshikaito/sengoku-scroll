<script setup lang="ts">
import type { StrategySupplyConvoyState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { convoyDetailIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  convoy: StrategySupplyConvoyState;
}>();
</script>

<template>
  <div class="convoy-intel">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(convoy.forceId) }">🌾 {{ convoy.name }}</span>
    </div>

    <StrategyIntelFieldList
      variant="dialog"
      :columns="3"
      :rows="convoyDetailIntelRows(worldState, convoy)"
    />

    <p class="hint">非军事单位，可与友军同格；移动由系统自动调度。</p>
  </div>
</template>

<style scoped>
.convoy-intel {
  color: #1e293b;
  font-size: 0.82rem;
  line-height: 1.45;
}

.header {
  margin-bottom: 10px;
}

.name {
  font-weight: 600;
  font-size: 0.95rem;
}

.hint {
  margin: 12px 0 0;
  color: #64748b;
  font-size: 0.85rem;
}
</style>
