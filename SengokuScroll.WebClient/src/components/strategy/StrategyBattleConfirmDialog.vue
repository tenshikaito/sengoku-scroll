<script setup lang="ts">
import { ref, watch } from "vue";
import type { StrategyBattlePreview, StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";

export type BattleTactic = "frontal" | "flank" | "surprise";
export type BattleMode = "auto" | "manual";

export interface BattleConfirmPayload {
  mode: BattleMode;
  tactic: BattleTactic;
}

const props = defineProps<{
  visible: boolean;
  worldState: StrategyWorldState;
  attacker: StrategyUnitState | null;
  preview: StrategyBattlePreview | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: BattleConfirmPayload];
}>();

const battleMode = ref<BattleMode>("auto");
const tactic = ref<BattleTactic>("frontal");

watch(
  () => props.visible,
  (open) => {
    if (open) {
      battleMode.value = "auto";
      tactic.value = "frontal";
    }
  }
);

function forceName(forceId: number) {
  return props.worldState.forces.find((f) => f.id === forceId)?.name ?? "未知势力";
}

const defender = () =>
  props.preview
    ? props.worldState.units.find((u) => u.id === props.preview!.defenderUnitId) ?? null
    : null;

function onConfirm() {
  if (battleMode.value !== "auto") return;
  emit("confirm", { mode: battleMode.value, tactic: tactic.value });
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="战斗确认"
    width="560px"
    align-center
    :close-on-click-modal="false"
    destroy-on-close
    class="strategy-dialog-centered-footer"
    @update:model-value="$emit('update:visible', $event)"
  >
    <div v-if="attacker && preview && defender()" class="battle-confirm">
      <div class="side">
        <h4>攻方（己方）</h4>
        <p class="name" :style="{ color: getForceColorCss(attacker.forceId) }">{{ attacker.name }}</p>
        <p>{{ forceName(attacker.forceId) }}</p>
        <ul>
          <li>兵数：{{ formatSoldiers(preview.attackerSoldiers) }}</li>
          <li>AP：{{ attacker.ap }}</li>
          <li>位置：({{ attacker.x }}, {{ attacker.y }})</li>
        </ul>
      </div>

      <div class="vs">VS</div>

      <div class="side">
        <h4>守方（敌军）</h4>
        <p class="name" :style="{ color: getForceColorCss(defender()!.forceId) }">
          {{ preview.defenderName }}
        </p>
        <p>{{ forceName(defender()!.forceId) }}</p>
        <ul>
          <li>兵数：{{ formatSoldiers(preview.defenderSoldiers) }}</li>
          <li>位置：({{ preview.targetX }}, {{ preview.targetY }})</li>
        </ul>
        <p class="win-rate">预估胜率：<strong>{{ preview.attackerWinRatePercent }}%</strong></p>
        <p class="hint">
          伤亡预估：己方 {{ formatSoldiers(preview.estimatedAttackerLossMin) }}～{{ formatSoldiers(preview.estimatedAttackerLossMax) }}，
          敌军 {{ formatSoldiers(preview.estimatedDefenderLossMin) }}～{{ formatSoldiers(preview.estimatedDefenderLossMax) }}
        </p>
      </div>
    </div>

    <div v-if="attacker && preview" class="options">
      <div class="option-block">
        <div class="label">战斗方式</div>
        <el-radio-group v-model="battleMode" size="small">
          <el-radio-button value="auto">自动战斗</el-radio-button>
          <el-radio-button value="manual" disabled>亲自指挥</el-radio-button>
        </el-radio-group>
        <p class="hint">亲自指挥将在后续里程碑开放。</p>
      </div>
      <div class="option-block">
        <div class="label">战术</div>
        <el-radio-group v-model="tactic" size="small">
          <el-radio-button value="frontal">正面攻击</el-radio-button>
          <el-radio-button value="flank">包围</el-radio-button>
          <el-radio-button value="surprise">突袭</el-radio-button>
        </el-radio-group>
        <p class="hint">M3-a：战术选项已预留，当前结算暂不区分。</p>
      </div>
    </div>

    <template #footer>
      <el-button @click="$emit('update:visible', false)">取消</el-button>
      <el-button type="primary" :disabled="battleMode !== 'auto'" @click="onConfirm">开始战斗</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.battle-confirm {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  gap: 16px;
  align-items: start;
}

.side h4 {
  margin: 0 0 8px;
  color: #94a3b8;
  font-size: 0.85rem;
}

.name {
  font-weight: 600;
  font-size: 1rem;
  margin: 0 0 4px;
}

.side ul {
  margin: 8px 0;
  padding-left: 1.1rem;
  color: #cbd5e1;
  font-size: 0.9rem;
}

.vs {
  align-self: center;
  font-weight: 700;
  color: #fbbf24;
  font-size: 1.2rem;
}

.options {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #334155;
  display: grid;
  gap: 14px;
}

.option-block .label {
  font-size: 0.85rem;
  color: #94a3b8;
  margin-bottom: 6px;
}

.win-rate {
  margin-top: 12px;
  color: #e2e8f0;
}

.hint {
  font-size: 0.78rem;
  color: #64748b;
  margin: 6px 0 0;
}
</style>
