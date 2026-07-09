<script setup lang="ts">
import type { StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { directiveLabel, pendingPolicyText } from "@/utils/unitDirective";
import { unitDetailIntelRows } from "@/utils/strategyIntelRows";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";

const props = defineProps<{
  worldState: StrategyWorldState;
  unit: StrategyUnitState;
  compact?: boolean;
}>();

const routeText = () => {
  if (!props.unit.route?.length) return "无";
  return props.unit.route.map((p) => `(${p.x},${p.y})`).join(" → ");
};
</script>

<template>
  <div class="unit-intel" :class="{ compact }">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(unit.forceId) }">{{ unit.name }}</span>
    </div>

    <StrategyIntelFieldList
      variant="dialog"
      :columns="3"
      :rows="unitDetailIntelRows(worldState, unit)"
    />

    <section v-if="unit.composition?.length" class="composition">
      <h4 class="composition-title">兵种构成</h4>
      <table class="composition-table">
        <thead>
          <tr>
            <th>兵种</th>
            <th>数量</th>
            <th>占比</th>
            <th>队将</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in unit.composition" :key="row.id">
            <td>{{ row.typeName }}</td>
            <td>{{ formatSoldiers(row.soldiers) }}</td>
            <td>{{ row.ratioPercent }}%</td>
            <td>{{ row.commanderName ?? "（总将）" }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <dl class="extra extra--columns">
      <div class="row">
        <dt>方针</dt>
        <dd>{{ directiveLabel(unit.directive) }}</dd>
      </div>
      <div v-if="pendingPolicyText(worldState.messengers, unit.id)" class="row">
        <dt>信使</dt>
        <dd class="pending">{{ pendingPolicyText(worldState.messengers, unit.id) }}</dd>
      </div>
      <div v-if="unit.route?.length" class="row">
        <dt>路径</dt>
        <dd class="route">{{ routeText() }}</dd>
      </div>
    </dl>
  </div>
</template>

<style scoped>
.unit-intel {
  color: #1e293b;
  font-size: 0.82rem;
  line-height: 1.45;
}

.unit-intel.compact {
  font-size: 0.78rem;
}

.header {
  margin-bottom: 10px;
}

.name {
  font-weight: 600;
  font-size: 0.95rem;
}

.compact .name {
  font-size: 0.88rem;
}

.extra {
  margin: 10px 0 0;
  display: grid;
  gap: 6px;
}

.extra--columns {
  grid-auto-flow: column;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-template-rows: repeat(2, auto);
  gap: 12px 16px;
}

.extra--columns .row {
  grid-template-columns: none;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.composition {
  margin-top: 12px;
}

.composition-title {
  margin: 0 0 6px;
  font-size: 0.82rem;
  font-weight: 600;
  color: #334155;
}

.composition-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.78rem;
}

.composition-table th,
.composition-table td {
  border: 1px solid #e2e8f0;
  padding: 4px 8px;
  text-align: left;
}

.composition-table th {
  background: #f8fafc;
  color: #64748b;
  font-weight: 500;
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

.pending {
  color: #0369a1;
}
</style>
