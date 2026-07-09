<script setup lang="ts">
import type { StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { directiveLabel, pendingPolicyText } from "@/utils/unitDirective";
import { unitHoverIntelRows } from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  unit: StrategyUnitState;
}>();
</script>

<template>
  <div class="summary">
    <div class="name" :style="{ color: getForceColorCss(unit.forceId) }">{{ unit.name }}</div>
    <StrategyIntelFieldList variant="hover" :rows="unitHoverIntelRows(worldState, unit)" />
    <div class="extra">方针：{{ directiveLabel(unit.directive) }}</div>
    <div v-if="pendingPolicyText(worldState.messengers, unit.id)" class="pending">
      📨 {{ pendingPolicyText(worldState.messengers, unit.id) }}
    </div>
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

.extra {
  margin-top: 6px;
  color: #cbd5e1;
}

.pending {
  color: #7dd3fc;
  margin-top: 4px;
}
</style>
