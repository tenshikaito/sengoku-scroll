<script setup lang="ts">
import type { StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { pendingPolicyText, siegeModeLabel } from "@/utils/unitDirective";
import { unitHoverIntelRows } from "@/utils/strategyIntelRows";
import { isStrategySimpleIntelMode } from "@/utils/strategyUnitLabels";

defineProps<{
  worldState: StrategyWorldState;
  unit: StrategyUnitState;
}>();

const showDebugFields = isStrategySimpleIntelMode();
</script>

<template>
  <div class="summary">
    <div class="name" :style="{ color: getForceColorCss(unit.forceId) }">{{ unit.name }}</div>
    <StrategyIntelFieldList
      variant="hover"
      :rows="unitHoverIntelRows(worldState, unit, { includeDebugFields: showDebugFields })"
    />
    <div v-if="unit.siegeMode && unit.siegeMode !== 'None'" class="extra">
      攻城：{{ siegeModeLabel(unit.siegeMode) }}
    </div>
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
