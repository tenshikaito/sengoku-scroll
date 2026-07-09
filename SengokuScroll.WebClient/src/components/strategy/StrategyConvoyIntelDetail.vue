<script setup lang="ts">
import type { StrategySupplyConvoyState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { directiveLabel } from "@/utils/unitDirective";
import { convoyDetailIntelRows } from "@/utils/strategyIntelRows";

const props = defineProps<{
  worldState: StrategyWorldState;
  convoy: StrategySupplyConvoyState;
}>();

const routeText = () => {
  if (!props.convoy.route?.length) return "无";
  return props.convoy.route.map((p) => `(${p.x},${p.y})`).join(" → ");
};
</script>

<template>
  <div class="convoy-intel">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(convoy.forceId) }">🌾 {{ convoy.name }}</span>
    </div>

    <StrategyIntelFieldList
      variant="dialog"
      label-width="4.5em"
      :rows="convoyDetailIntelRows(worldState, convoy)"
    />

    <dl class="extra">
      <div class="row">
        <dt>方针</dt>
        <dd>{{ directiveLabel(convoy.directive) }}</dd>
      </div>
      <div v-if="convoy.route?.length" class="row">
        <dt>路径</dt>
        <dd class="route">{{ routeText() }}</dd>
      </div>
      <div class="row hint-row">
        <dt>说明</dt>
        <dd class="hint">非军事单位，可与友军同格；移动由系统自动调度。</dd>
      </div>
    </dl>
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

.extra {
  margin: 10px 0 0;
  display: grid;
  gap: 6px;
}

.row {
  display: grid;
  grid-template-columns: 4.5em 1fr;
  gap: 10px;
}

dt {
  margin: 0;
  color: #64748b;
}

dd {
  margin: 0;
  color: #0f172a;
}

.route {
  word-break: break-all;
}

.hint {
  color: #64748b;
  font-size: 0.85rem;
}
</style>
