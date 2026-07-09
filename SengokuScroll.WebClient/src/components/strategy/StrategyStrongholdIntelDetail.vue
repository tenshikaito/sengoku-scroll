<script setup lang="ts">
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { strongholdDetailIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  stronghold: StrategyStrongholdState;
}>();
</script>

<template>
  <div class="stronghold-intel">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(stronghold.forceId) }">
        🏯 {{ stronghold.name }}
      </span>
    </div>
    <StrategyIntelFieldList
      variant="dialog"
      :columns="3"
      :rows="strongholdDetailIntelRows(worldState, stronghold)"
    />
  </div>
</template>

<style scoped>
.stronghold-intel {
  color: #1e293b;
  font-size: 0.9rem;
  line-height: 1.45;
}

.header {
  margin-bottom: 10px;
}

.name {
  font-weight: 600;
  font-size: 1rem;
}
</style>
