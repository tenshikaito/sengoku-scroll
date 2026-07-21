<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import {
  STRONGHOLD_DEFENSE_COLUMNS,
  STRONGHOLD_LIST_COLUMN_PRESETS,
  type StrongholdListPreset,
} from "@/utils/strategyIntelSystemColumns";
import {
  entityEffectsIntelRows,
  strongholdCultureDetailRows,
  strongholdDefenseFacilityTableRows,
  strongholdDetailFieldRows,
  strongholdIntelRows,
  strongholdIntroText,
  strongholdReligionDetailRows,
  type IntelStrongholdRow,
} from "@/utils/strategyIntelSystemData";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
}>();

const listPreset = ref<StrongholdListPreset>("status");
const detailTab = ref<"basic" | "defense" | "culture" | "religion" | "effects" | "intro">(
  "basic"
);
const selectedStrongholdId = ref<number | null>(null);

const strongholdRows = computed(() =>
  strongholdIntelRows(props.worldState, { realmFilter: props.realmFilter })
);
const listColumns = computed(() => STRONGHOLD_LIST_COLUMN_PRESETS[listPreset.value]);
const listRows = computed(
  () => strongholdRows.value as unknown as Array<Record<string, unknown>>
);

const basicRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdDetailFieldRows(props.worldState, selectedStrongholdId.value)
    : []
);

const cultureRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdCultureDetailRows(props.worldState, selectedStrongholdId.value)
    : []
);

const religionRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdReligionDetailRows(props.worldState, selectedStrongholdId.value)
    : []
);

const effectsRows = computed(() => entityEffectsIntelRows());

const defenseRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdDefenseFacilityTableRows(props.worldState, selectedStrongholdId.value)
    : []
);

const defenseListRows = computed(
  () => defenseRows.value as unknown as Array<Record<string, unknown>>
);

const introText = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdIntroText(props.worldState, selectedStrongholdId.value)
    : "请在上方列表选择据点。"
);

function defaultStrongholdId(): number | null {
  const rows = strongholdRows.value;
  const residence = props.worldState.strongholds.find(
    (s) => s.forceId === props.worldState.playerForceId && s.isLordResidence
  );
  if (residence && rows.some((r) => r.id === residence.id)) return residence.id;
  return rows[0]?.id ?? null;
}

function syncDefaultSelection() {
  const rows = strongholdRows.value;
  if (
    selectedStrongholdId.value != null &&
    rows.some((r) => r.id === selectedStrongholdId.value)
  ) {
    return;
  }
  selectedStrongholdId.value = defaultStrongholdId();
}

watch(
  () => [props.worldState, props.realmFilter] as const,
  () => syncDefaultSelection(),
  { immediate: true }
);

function onSelectRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultSelection();
    return;
  }
  selectedStrongholdId.value = Number(row.id);
}

function rowClass(row: Record<string, unknown>) {
  return (row as unknown as IntelStrongholdRow).isLordResidence === "○" ? "is-lord-row" : "";
}
</script>

<template>
  <div class="intel-pane">
    <el-tabs v-model="listPreset" class="layer-tabs layer-tabs--list">
      <el-tab-pane label="状态" name="status" />
      <el-tab-pane label="内政" name="supplies" />
      <el-tab-pane label="军备" name="military" />
    </el-tabs>

    <StrategyIntelSystemTable
      :rows="listRows"
      :columns="listColumns"
      :current-id="selectedStrongholdId"
      empty-text="暂无据点数据"
      :row-class-name="rowClass"
      @current-change="onSelectRow"
    />

    <div class="detail-section">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="城防" name="defense" />
        <el-tab-pane label="文化" name="culture" />
        <el-tab-pane label="信仰" name="religion" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div v-if="detailTab === 'basic'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="basicRows.length" :rows="basicRows" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'defense'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="defenseListRows"
          :columns="STRONGHOLD_DEFENSE_COLUMNS"
          :highlight-current="false"
          empty-text="暂无城防设施"
          :max-height="220"
        />
      </div>

      <div v-else-if="detailTab === 'culture'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="cultureRows.length" :rows="cultureRows" :column="1" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'religion'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="religionRows.length" :rows="religionRows" :column="1" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'effects'" class="detail-body">
        <StrategyIntelBasicDescriptions :rows="effectsRows" />
      </div>

      <div v-else class="detail-body">
        <p class="intro-text">{{ introText }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.intel-pane {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.layer-tabs :deep(.el-tabs__item) {
  font-size: 0.85rem;
}

.detail-section {
  border-top: 1px solid #e2e8f0;
  padding-top: 10px;
}

.detail-body {
  min-height: 120px;
  max-height: 280px;
  overflow: auto;
}

.intro-text {
  margin: 0;
  font-size: 0.88rem;
  line-height: 1.6;
  color: #334155;
}

.placeholder {
  margin: 0;
  font-size: 0.85rem;
  color: #64748b;
}

:deep(.is-lord-row:not(.current-row)) {
  --el-table-tr-bg-color: #f8fafc;
}
</style>
