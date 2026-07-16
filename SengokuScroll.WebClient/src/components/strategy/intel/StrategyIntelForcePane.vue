<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  DIPLOMACY_BRIEF_COLUMNS,
  FORCE_LIST_COLUMN_PRESETS,
  type ForceListPreset,
} from "@/utils/strategyIntelSystemColumns";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import {
  diplomacyForForceRows,
  entityEffectsIntelRows,
  forceCultureDetailRows,
  forceDetailIntelRows,
  forceIntelListRows,
  forceIntroText,
  forceReligionDetailRows,
} from "@/utils/strategyIntelSystemData";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
}>();

const listPreset = ref<ForceListPreset>("status");
const detailTab = ref<"basic" | "diplomacy" | "culture" | "religion" | "effects" | "intro">(
  "basic"
);
const selectedForceId = ref<number | null>(null);

const listColumns = computed(() => FORCE_LIST_COLUMN_PRESETS[listPreset.value]);
const forceRows = computed(() =>
  forceIntelListRows(props.worldState, { realmFilter: props.realmFilter })
);
const listRows = computed(
  () => forceRows.value as unknown as Array<Record<string, unknown>>
);

const basicRows = computed(() =>
  selectedForceId.value != null
    ? forceDetailIntelRows(props.worldState, selectedForceId.value)
    : []
);

const cultureRows = computed(() =>
  selectedForceId.value != null
    ? forceCultureDetailRows(props.worldState, selectedForceId.value)
    : []
);

const religionRows = computed(() =>
  selectedForceId.value != null
    ? forceReligionDetailRows(props.worldState, selectedForceId.value)
    : []
);

const effectsRows = computed(() => entityEffectsIntelRows());

const diplomacyRows = computed(() =>
  selectedForceId.value != null
    ? diplomacyForForceRows(props.worldState, selectedForceId.value)
    : []
);

const diplomacyListRows = computed(
  () => diplomacyRows.value as unknown as Array<Record<string, unknown>>
);

const introText = computed(() =>
  selectedForceId.value != null
    ? forceIntroText(props.worldState, selectedForceId.value)
    : "请在上方列表选择势力。"
);

const diplomacyHint = computed(() => {
  const row = forceRows.value.find((item) => item.id === selectedForceId.value);
  if (!row) return "";
  if (row.isOwnRealm) {
    return "封地势力掌握的外交关系一览（含内藩）。";
  }
  return "他势力外交情报尚未掌握，仅显示与自势力关系。";
});

function defaultForceId(): number | null {
  const playerRoot = props.worldState.playerForceId;
  const rows = forceRows.value;
  if (rows.some((r) => r.id === playerRoot)) return playerRoot;
  return rows[0]?.id ?? null;
}

function syncDefaultSelection() {
  const rows = forceRows.value;
  if (selectedForceId.value != null && rows.some((r) => r.id === selectedForceId.value)) {
    return;
  }
  selectedForceId.value = defaultForceId();
}

watch(
  () => [props.worldState, props.realmFilter] as const,
  () => syncDefaultSelection(),
  { immediate: true }
);

function diplomacyRowClassName(row: Record<string, unknown>): string {
  switch (String(row.diplomacyTone ?? "")) {
    case "allied":
      return "dip-allied";
    case "enemy":
      return "dip-enemy";
    case "neutral":
      return "dip-neutral";
    default:
      return "";
  }
}

function onSelectRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultSelection();
    return;
  }
  selectedForceId.value = Number(row.id);
}
</script>

<template>
  <div class="intel-pane">
    <el-tabs v-model="listPreset" class="layer-tabs layer-tabs--list">
      <el-tab-pane label="状态" name="status" />
      <el-tab-pane label="军备" name="military" />
    </el-tabs>

    <StrategyIntelSystemTable
      :rows="listRows"
      :columns="listColumns"
      :current-id="selectedForceId"
      empty-text="暂无势力数据"
      @current-change="onSelectRow"
    />

    <div class="detail-section">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="外交" name="diplomacy" />
        <el-tab-pane label="文化" name="culture" />
        <el-tab-pane label="信仰" name="religion" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div v-if="detailTab === 'basic'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="basicRows.length" :rows="basicRows" />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'diplomacy'" class="detail-body">
        <p v-if="diplomacyHint" class="detail-hint">{{ diplomacyHint }}</p>
        <StrategyIntelSystemTable
          :rows="diplomacyListRows"
          :columns="DIPLOMACY_BRIEF_COLUMNS"
          :highlight-current="false"
          :row-class-name="diplomacyRowClassName"
          empty-text="暂无外交情报"
          :max-height="200"
        />
      </div>

      <div v-else-if="detailTab === 'culture'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="cultureRows.length" :rows="cultureRows" />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'religion'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="religionRows.length" :rows="religionRows" />
        <p v-else class="placeholder">请选择势力。</p>
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
}

.detail-hint {
  margin: 0 0 8px;
  font-size: 0.8rem;
  color: #64748b;
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

:deep(.dip-allied td.el-table__cell) {
  color: #16a34a;
}

:deep(.dip-enemy td.el-table__cell) {
  color: #dc2626;
}

:deep(.dip-neutral td.el-table__cell) {
  color: #ea580c;
}
</style>
