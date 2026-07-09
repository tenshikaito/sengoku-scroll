<script setup lang="ts">
import { computed } from "vue";
import type { StrategyBattleResult } from "@/api/strategy";
import { displayUnitName } from "@/utils/battleResult";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";

const props = defineProps<{
  visible: boolean;
  result: StrategyBattleResult | null;
}>();

defineEmits<{
  "update:visible": [value: boolean];
}>();

const sortedLogs = computed(() =>
  [...(props.result?.logEntries ?? [])].sort((a, b) => a.order - b.order)
);

function sideLabel(side: string) {
  if (side === "attacker") return "攻方";
  if (side === "defender") return "守方";
  return "战场";
}

function sideClass(side: string) {
  if (side === "attacker") return "log-attacker";
  if (side === "defender") return "log-defender";
  return "log-system";
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="战斗结果"
    width="520px"
    align-center
    destroy-on-close
    class="battle-result-dialog"
    @update:model-value="$emit('update:visible', $event)"
  >
    <div v-if="result" class="result">
      <p class="outcome" :class="result.attackerWon ? 'win' : 'lose'">
        {{ result.attackerWon ? "⚔ 战斗胜利" : "✖ 战斗失利" }}
      </p>

      <div class="summary-grid">
        <div class="summary-card">
          <h4>攻方</h4>
          <p class="name">{{ displayUnitName(result, "attacker") }}</p>
          <p>
            {{ formatSoldiers(result.attackerSoldiersBefore) }} → −{{ formatSoldiers(result.attackerCasualties) }} →
            <strong>{{ formatSoldiers(result.attackerSoldiersAfter) }}</strong>
          </p>
        </div>
        <div class="summary-card">
          <h4>守方</h4>
          <p class="name">{{ displayUnitName(result, "defender") }}</p>
          <p>
            {{ formatSoldiers(result.defenderSoldiersBefore) }} → −{{ formatSoldiers(result.defenderCasualties) }} →
            <strong>{{ formatSoldiers(result.defenderSoldiersAfter) }}</strong>
          </p>
        </div>
      </div>

      <p class="meta">
        战前胜率 {{ result.attackerWinRatePercent }}%
        <span v-if="result.resolutionRoll >= 0"> · 判定值 {{ result.resolutionRoll }}</span>
      </p>

      <h4 class="log-title">战斗过程</h4>
      <ol v-if="sortedLogs.length" class="battle-log">
        <li v-for="entry in sortedLogs" :key="entry.order" :class="sideClass(entry.side)">
          <span class="log-side">{{ sideLabel(entry.side) }}</span>
          <span class="log-phase">{{ entry.phase }}</span>
          <span class="log-message">{{ entry.message }}</span>
        </li>
      </ol>
      <p v-else class="hint">暂无过程记录。</p>
    </div>
    <template #footer>
      <el-button type="primary" @click="$emit('update:visible', false)">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.result {
  color: #1e293b;
}

.outcome {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0 0 16px;
}

.outcome.win {
  color: #15803d;
}

.outcome.lose {
  color: #b91c1c;
}

.summary-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-bottom: 12px;
}

.summary-card {
  padding: 10px 12px;
  border-radius: 8px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
}

.summary-card h4 {
  margin: 0 0 6px;
  font-size: 0.8rem;
  color: #475569;
}

.name {
  margin: 0 0 4px;
  font-weight: 600;
  color: #0f172a;
}

.summary-card p {
  margin: 0;
  font-size: 0.88rem;
  color: #334155;
}

.summary-card strong {
  color: #b45309;
}

.meta {
  margin: 0 0 14px;
  font-size: 0.85rem;
  color: #475569;
}

.log-title {
  margin: 0 0 8px;
  font-size: 0.95rem;
  font-weight: 600;
  color: #0f172a;
}

.battle-log {
  margin: 0;
  padding: 0;
  list-style: none;
  display: grid;
  gap: 8px;
  max-height: 240px;
  overflow-y: auto;
}

.battle-log li {
  display: grid;
  grid-template-columns: 3em 3.5em 1fr;
  gap: 8px;
  padding: 8px 10px;
  border-radius: 6px;
  font-size: 0.86rem;
  line-height: 1.5;
}

.log-attacker {
  background: #eff6ff;
  border-left: 3px solid #2563eb;
}

.log-defender {
  background: #fef2f2;
  border-left: 3px solid #dc2626;
}

.log-system {
  background: #f1f5f9;
  border-left: 3px solid #64748b;
}

.log-side {
  color: #0f172a;
  font-weight: 700;
}

.log-phase {
  color: #92400e;
  font-weight: 600;
}

.log-message {
  color: #1e293b;
}

.hint {
  margin: 0;
  font-size: 0.85rem;
  color: #64748b;
}
</style>
