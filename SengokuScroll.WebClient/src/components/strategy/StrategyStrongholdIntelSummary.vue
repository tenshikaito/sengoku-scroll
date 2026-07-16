<script setup lang="ts">
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import StrategyStrongholdTitle from "./StrategyStrongholdTitle.vue";
import { strongholdHoverIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  stronghold: StrategyStrongholdState;
}>();
</script>

<template>
  <div class="summary">
    <div class="name" :style="{ color: getForceColorCss(stronghold.forceId) }">
      <StrategyStrongholdTitle :stronghold="stronghold" :world-state="worldState" size="hover" />
    </div>
    <StrategyIntelFieldList variant="hover" :rows="strongholdHoverIntelRows(worldState, stronghold)" />
  </div>
</template>

<style scoped>
.summary {
  font-size: 0.78rem;
  line-height: 1.45;
  color: #e2e8f0;
}

.name {
  margin-bottom: 6px;
}
</style>
