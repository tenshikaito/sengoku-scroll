<script setup lang="ts">
import type { StrategyBattlefieldState } from "@/api/strategy";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import { formatFoodGo, formatMoney, formatSoldiers } from "@/utils/strategyDisplayUnits";

defineProps<{
  battlefield: StrategyBattlefieldState;
}>();

function siegeThreatLabel(value: string | undefined | null): string {
  switch (value) {
    case "Assault":
      return "强攻";
    case "Encircle":
      return "围城";
    default:
      return "—";
  }
}

function battlefieldStatusRows(battlefield: StrategyBattlefieldState) {
  const rows: { label: string; value: string }[] = [];

  if (battlefield.kind === "Siege") {
    rows.push({
      label: "攻城",
      value: siegeThreatLabel(battlefield.siegeThreat),
    });
    if (battlefield.standoffDays > 0) {
      rows.push({
        label: "持续",
        value: `${battlefield.standoffDays} 日`,
      });
    }
  } else {
    rows.push({
      label: "对峙",
      value: battlefield.standoffDays > 0 ? `${battlefield.standoffDays} 日` : "当日",
    });
  }

  return rows;
}
</script>

<template>
  <div class="summary">
    <StrategyIntelFieldList variant="hover" :rows="battlefieldStatusRows(battlefield)" />
    <template v-for="(entry, index) in battlefield.participants" :key="entry.forceId">
      <div v-if="index > 0" class="entity-divider" role="separator" />
      <div class="force-block">
        <StrategyIntelFieldList
          variant="hover"
          :rows="[
            { label: '势力', value: entry.forceName },
            { label: '兵数', value: formatSoldiers(entry.soldiers) },
            { label: '士气', value: `${Math.max(0, Math.min(100, entry.morale))}%` },
            { label: '金钱', value: formatMoney(entry.money ?? 0) },
            { label: '粮草', value: formatFoodGo(entry.food ?? 0) },
          ]"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.summary {
  font-size: 0.78rem;
  line-height: 1.45;
  color: #e2e8f0;
}

.entity-divider {
  height: 0;
  margin: 10px 0;
  border: none;
  border-top: 1px solid #94a3b8;
  opacity: 0.85;
}
</style>
