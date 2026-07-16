<script setup lang="ts">
import type { StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { unitDetailIntelRows } from "@/utils/strategyIntelRows";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";

defineProps<{
  worldState: StrategyWorldState;
  unit: StrategyUnitState;
  compact?: boolean;
}>();
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
</style>
