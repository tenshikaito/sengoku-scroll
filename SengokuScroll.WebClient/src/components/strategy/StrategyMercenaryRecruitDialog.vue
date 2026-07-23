<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import {
  buildMercenaryBudgetKanMarks,
  kanToMoney,
  maxMercenaryBudgetKan,
  mercenarySoldiersFromKan,
  recruitAssignablePersonRows,
} from "@/utils/strategyRecruitDialogs";

const props = withDefaults(
  defineProps<{
    visible: boolean;
    mode?: "assign" | "personal";
    stronghold: StrategyStrongholdState | null;
    worldState: StrategyWorldState | null;
    /** 个人募兵时的执行角色 Id。 */
    actingCharacterId?: number | null;
  }>(),
  {
    mode: "assign",
    actingCharacterId: null,
  },
);

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: { characterId: number; budgetMoney: number }];
}>();

const selectedCharacterId = ref<number | null>(null);
const budgetKan = ref(1);
const personListPreset = ref<PersonListPreset>("status");

const isPersonal = computed(() => props.mode === "personal");

const actingCharacter = computed(() => {
  if (!isPersonal.value || props.actingCharacterId == null) return null;
  return props.worldState?.characters?.find((c) => c.id === props.actingCharacterId) ?? null;
});

const effectiveCharacterId = computed(() =>
  isPersonal.value ? props.actingCharacterId ?? null : selectedCharacterId.value,
);

const effectiveCharacter = computed(() => {
  if (isPersonal.value) return actingCharacter.value;
  return props.worldState?.characters?.find((c) => c.id === selectedCharacterId.value) ?? null;
});

const budgetMoneySource = computed(() => {
  if (isPersonal.value) return effectiveCharacter.value?.money ?? 0;
  return props.stronghold?.money ?? 0;
});

const maxKan = computed(() => maxMercenaryBudgetKan(budgetMoneySource.value));

const budgetMarks = computed(() => buildMercenaryBudgetKanMarks(maxKan.value));

const estimatedSoldiers = computed(() => mercenarySoldiersFromKan(budgetKan.value));

const personRows = computed(() => {
  const sh = props.stronghold;
  const ws = props.worldState;
  if (isPersonal.value || !sh || !ws) return [];
  return recruitAssignablePersonRows(ws, sh.id);
});

const personListColumns = computed(() => {
  const cols = PERSON_LIST_COLUMN_PRESETS[personListPreset.value];
  if (personListPreset.value === "personal" && !isIntelDevFieldsVisible()) {
    const devProps = new Set<string>(PERSON_PERSONAL_DEV_ONLY_PROPS);
    return cols.filter((col) => !devProps.has(col.prop));
  }
  return cols;
});

const personListRows = computed(
  () => personRows.value as unknown as Array<Record<string, unknown>>,
);

const dialogTitle = computed(() => {
  const place = props.stronghold?.name ?? "据点";
  if (isPersonal.value) {
    const name = actingCharacter.value?.name ?? "角色";
    return `个人募兵 — ${name}（${place}）`;
  }
  return props.stronghold ? `募兵 — ${place}` : "募兵";
});

const budgetHint = computed(() => {
  if (isPersonal.value) {
    return effectiveCharacter.value
      ? `${effectiveCharacter.value.name} 个人持有 ${maxKan.value} 贯 · 约募 ${estimatedSoldiers.value} 人`
      : "";
  }
  return effectiveCharacter.value && maxKan.value >= 1
    ? `据点府库 ${maxKan.value} 贯 · 约募 ${estimatedSoldiers.value} 人（委派 ${effectiveCharacter.value.name}）`
    : props.stronghold
      ? `据点府库 ${maxKan.value} 贯 · 约募 ${estimatedSoldiers.value} 人`
      : "";
});

function clampBudgetKan() {
  if (maxKan.value < 1) {
    budgetKan.value = 1;
    return;
  }
  budgetKan.value = Math.min(Math.max(1, budgetKan.value), maxKan.value);
}

watch(
  () => [props.visible, props.mode, personRows.value, props.actingCharacterId] as const,
  ([visible, mode, rows]) => {
    if (!visible) return;
    if (mode === "assign") {
      selectedCharacterId.value = rows[0]?.id ?? null;
      personListPreset.value = "status";
    }
    clampBudgetKan();
  },
);

watch([effectiveCharacterId, maxKan, () => props.mode], () => {
  if (!props.visible) return;
  clampBudgetKan();
});

function close() {
  emit("update:visible", false);
}

function onPersonSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedCharacterId.value = Number(row.id);
}

function submit() {
  if (effectiveCharacterId.value == null || maxKan.value < 1 || budgetKan.value < 1) return;
  emit("confirm", {
    characterId: effectiveCharacterId.value,
    budgetMoney: kanToMoney(budgetKan.value),
  });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="min(820px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer recruit-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <div v-if="effectiveCharacterId != null" class="budget-row">
      <span class="budget-label">预算</span>
      <el-slider
        v-model="budgetKan"
        class="budget-slider"
        :min="1"
        :max="Math.max(1, maxKan)"
        :step="1"
        :marks="budgetMarks"
        :disabled="maxKan < 1"
      />
      <span class="budget-value">{{ budgetKan }} 贯</span>
    </div>
    <p v-if="maxKan >= 1" class="budget-sub">{{ budgetHint }}</p>
    <p v-else-if="effectiveCharacterId != null" class="budget-sub warn">
      {{ isPersonal ? "个人资金不足 1 贯" : "据点府库不足 1 贯" }}
    </p>

    <template v-if="!isPersonal">
      <h4 class="panel-title">城内将领</h4>
      <el-tabs v-model="personListPreset" class="layer-tabs">
        <el-tab-pane label="状态" name="status" />
        <el-tab-pane label="仕官" name="office" />
        <el-tab-pane label="命令" name="order" />
        <el-tab-pane label="个人" name="personal" />
        <el-tab-pane label="能力1" name="ability1" />
        <el-tab-pane label="能力2" name="ability2" />
      </el-tabs>
      <StrategyIntelSystemTable
        :rows="personListRows"
        :columns="personListColumns"
        :current-id="selectedCharacterId"
        scroll-wrap
        :max-height="360"
        empty-text="城内暂无待命将领"
        @current-change="onPersonSelect"
      />
    </template>

    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button
        type="primary"
        :disabled="!stronghold || effectiveCharacterId == null || maxKan < 1"
        @click="submit"
      >
        确认
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.budget-row {
  display: grid;
  grid-template-columns: 40px minmax(0, 1fr) minmax(104px, max-content);
  align-items: center;
  gap: 12px;
  margin-bottom: 6px;
}

.budget-label {
  font-size: 0.85rem;
  color: #334155;
  white-space: nowrap;
}

.budget-value {
  font-size: 0.88rem;
  font-weight: 600;
  color: #0f172a;
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.budget-sub {
  margin: 0 0 14px;
  font-size: 0.82rem;
  color: #64748b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.budget-sub.warn {
  color: #b45309;
}

.budget-slider {
  padding: 0 8px 32px 0;
}

.budget-slider :deep(.el-slider__marks-text) {
  font-size: 0.82rem;
  font-weight: 600;
  color: #334155;
  margin-top: 10px;
  white-space: nowrap;
  cursor: pointer;
  user-select: none;
}

.budget-slider :deep(.el-slider__stop) {
  width: 10px;
  height: 10px;
  border: 2px solid #fff;
  box-shadow: 0 0 0 1px #94a3b8;
}

.budget-slider :deep(.el-slider__button) {
  width: 16px;
  height: 16px;
}

.panel-title {
  margin: 0 0 6px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #0f172a;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}
</style>
