<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyStrongholdState } from "@/api/strategy";
import { canLordCommandStronghold } from "@/utils/strategyPlayerCharacter";
import { LORD_AT_RESIDENCE_REQUIRED_TIP, resolveLordResidenceStronghold } from "@/utils/strategyLordCommands";
import type { StrategyWorldState } from "@/api/strategyTypes";

const props = defineProps<{
  visible: boolean;
  stronghold: StrategyStrongholdState | null;
  worldState: StrategyWorldState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: {
    pollTaxRate?: number;
    agricultureTaxRate?: number;
    commerceTaxRate?: number;
    tariffTaxRate?: number;
  }];
}>();

const pollTaxRate = ref(0);
const agricultureTaxRate = ref(0);
const commerceTaxRate = ref(0);
const tariffTaxRate = ref(0);

const baseline = ref({
  pollTaxRate: 0,
  agricultureTaxRate: 0,
  commerceTaxRate: 0,
  tariffTaxRate: 0,
});

/** 0–100 刻度标记（可点击停靠点由 step=25 + show-stops 提供）。 */
const taxMarks = {
  0: "0",
  25: "25",
  50: "50",
  75: "75",
  100: "100",
} as const;

const canCommandTax = computed(() => {
  if (!props.worldState || !props.stronghold) return false;
  return canLordCommandStronghold(props.worldState, props.stronghold);
});

const needsMessenger = computed(() => {
  if (!props.worldState || !props.stronghold) return true;
  const residence = resolveLordResidenceStronghold(props.worldState);
  if (!residence) return true;
  return residence.x !== props.stronghold.x || residence.y !== props.stronghold.y;
});

const deliveryHint = computed(() => {
  if (!props.stronghold?.isDirectRule) return "已任命领主领地，税率由城主自行决定";
  if (!canCommandTax.value) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  if (needsMessenger.value) {
    return "税令将从当主居城派出信使，抵达直辖城后生效";
  }
  return "当主居城与目标同格，税令即时生效";
});

watch(
  () => [props.visible, props.stronghold?.id] as const,
  ([visible, id]) => {
    if (!visible || !props.stronghold || id == null) return;
    pollTaxRate.value = props.stronghold.pollTaxRate;
    agricultureTaxRate.value = props.stronghold.agricultureTaxRate;
    commerceTaxRate.value = props.stronghold.commerceTaxRate;
    tariffTaxRate.value = props.stronghold.tariffTaxRate;
    baseline.value = {
      pollTaxRate: props.stronghold.pollTaxRate,
      agricultureTaxRate: props.stronghold.agricultureTaxRate,
      commerceTaxRate: props.stronghold.commerceTaxRate,
      tariffTaxRate: props.stronghold.tariffTaxRate,
    };
  },
);

const hasChange = computed(
  () =>
    pollTaxRate.value !== baseline.value.pollTaxRate
    || agricultureTaxRate.value !== baseline.value.agricultureTaxRate
    || commerceTaxRate.value !== baseline.value.commerceTaxRate
    || tariffTaxRate.value !== baseline.value.tariffTaxRate,
);

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!hasChange.value) return;
  const payload: {
    pollTaxRate?: number;
    agricultureTaxRate?: number;
    commerceTaxRate?: number;
    tariffTaxRate?: number;
  } = {};
  if (pollTaxRate.value !== baseline.value.pollTaxRate) {
    payload.pollTaxRate = pollTaxRate.value;
  }
  if (agricultureTaxRate.value !== baseline.value.agricultureTaxRate) {
    payload.agricultureTaxRate = agricultureTaxRate.value;
  }
  if (commerceTaxRate.value !== baseline.value.commerceTaxRate) {
    payload.commerceTaxRate = commerceTaxRate.value;
  }
  if (tariffTaxRate.value !== baseline.value.tariffTaxRate) {
    payload.tariffTaxRate = tariffTaxRate.value;
  }
  emit("confirm", payload);
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="stronghold ? `税率 — ${stronghold.name}` : '税率'"
    width="480px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="stronghold" class="hint">
      据点 ({{ stronghold.x }}, {{ stronghold.y }}) ·
      {{ stronghold.isDirectRule ? "直辖" : `城主：${stronghold.lordName}` }}
    </p>
    <p class="hint" :class="{ warn: !canCommandTax }">{{ deliveryHint }}</p>

    <div v-if="stronghold" class="tax-grid">
      <div class="tax-row">
        <span class="tax-label">人头税</span>
        <el-slider
          v-model="pollTaxRate"
          class="tax-slider"
          :min="0"
          :max="100"
          :step="1"
          :marks="taxMarks"
        />
        <span class="tax-value">{{ pollTaxRate }}%</span>
      </div>
      <div class="tax-row">
        <span class="tax-label">农税</span>
        <el-slider
          v-model="agricultureTaxRate"
          class="tax-slider"
          :min="0"
          :max="100"
          :step="1"
          :marks="taxMarks"
        />
        <span class="tax-value">{{ agricultureTaxRate }}%</span>
      </div>
      <div class="tax-row">
        <span class="tax-label">商税</span>
        <el-slider
          v-model="commerceTaxRate"
          class="tax-slider"
          :min="0"
          :max="100"
          :step="1"
          :marks="taxMarks"
        />
        <span class="tax-value">{{ commerceTaxRate }}%</span>
      </div>
      <div class="tax-row">
        <span class="tax-label">关税</span>
        <el-slider
          v-model="tariffTaxRate"
          class="tax-slider"
          :min="0"
          :max="100"
          :step="1"
          :marks="taxMarks"
        />
        <span class="tax-value">{{ tariffTaxRate }}%</span>
      </div>
    </div>

    <p class="hint subtle">拖动滑块调整（0–100）；变更将影响民心。</p>

    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button type="primary" :disabled="!stronghold || !hasChange || !canCommandTax" @click="submit">
        确认
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  margin: 0 0 12px;
  font-size: 0.82rem;
  color: #64748b;
  line-height: 1.45;
}

.hint.warn {
  color: #b45309;
}

.hint.subtle {
  margin-top: 8px;
  margin-bottom: 0;
}

.tax-grid {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.tax-row {
  display: grid;
  grid-template-columns: 56px minmax(0, 1fr) 44px;
  align-items: center;
  gap: 10px;
}

.tax-label {
  font-size: 0.85rem;
  color: #334155;
}

.tax-value {
  font-size: 0.88rem;
  font-weight: 600;
  color: #0f172a;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.tax-slider :deep(.el-slider__marks-text) {
  font-size: 0.68rem;
}
</style>
