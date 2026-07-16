<script setup lang="ts">
import type { StrategyMessengerState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { messengerDetailIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  messenger: StrategyMessengerState;
}>();
</script>

<template>
  <div class="messenger-intel">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(messenger.forceId) }">
        📨 {{ messenger.name }}
      </span>
    </div>

    <StrategyIntelFieldList
      variant="dialog"
      :columns="3"
      :rows="messengerDetailIntelRows(worldState, messenger)"
    />

    <p class="hint">非军事单位；抵达目标部队后传达方针或交付情报。</p>
  </div>
</template>

<style scoped>
.messenger-intel {
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
