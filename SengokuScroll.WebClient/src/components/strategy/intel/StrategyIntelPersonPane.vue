<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import StrategyIntelPersonSkills from "../StrategyIntelPersonSkills.vue";
import StrategyIntelPersonStats from "../StrategyIntelPersonStats.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import {
  entityEffectsIntelRows,
  personDetailIntelRows,
  personIntelRows,
  personIntroText,
  personSkillDetailRows,
  personStatDetailRows,
} from "@/utils/strategyIntelSystemData";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
}>();

const listPreset = ref<PersonListPreset>("status");
const detailTab = ref<"basic" | "attributes" | "skills" | "effects" | "intro">("basic");
const selectedPersonId = ref<number | null>(null);

const personRows = computed(() =>
  personIntelRows(props.worldState, { realmFilter: props.realmFilter })
);
const listColumns = computed(() => {
  const cols = PERSON_LIST_COLUMN_PRESETS[listPreset.value];
  if (listPreset.value === "personal" && !isIntelDevFieldsVisible()) {
    const devProps = new Set<string>(PERSON_PERSONAL_DEV_ONLY_PROPS);
    return cols.filter((col) => !devProps.has(col.prop));
  }
  return cols;
});
const listRows = computed(
  () => personRows.value as unknown as Array<Record<string, unknown>>
);

const basicRows = computed(() =>
  selectedPersonId.value != null
    ? personDetailIntelRows(props.worldState, selectedPersonId.value)
    : []
);

const statRows = computed(() =>
  selectedPersonId.value != null
    ? personStatDetailRows(props.worldState, selectedPersonId.value)
    : []
);

const skillRows = computed(() =>
  selectedPersonId.value != null
    ? personSkillDetailRows(props.worldState, selectedPersonId.value)
    : []
);

const effectsRows = computed(() => entityEffectsIntelRows());

const introText = computed(() =>
  selectedPersonId.value != null
    ? personIntroText(props.worldState, selectedPersonId.value)
    : "请在上方列表选择人物。"
);

function defaultPersonId(): number | null {
  const rows = personRows.value;
  const lordName = props.worldState.lord.name?.trim();
  if (lordName) {
    const lord = rows.find((p) => p.name === lordName);
    if (lord) return lord.id;
  }
  return rows[0]?.id ?? null;
}

function syncDefaultSelection() {
  const rows = personRows.value;
  if (selectedPersonId.value != null && rows.some((r) => r.id === selectedPersonId.value)) {
    return;
  }
  selectedPersonId.value = defaultPersonId();
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
  selectedPersonId.value = Number(row.id);
}
</script>

<template>
  <div class="intel-pane">
    <el-tabs v-model="listPreset" class="layer-tabs layer-tabs--list">
      <el-tab-pane label="状态" name="status" />
      <el-tab-pane label="仕官" name="office" />
      <el-tab-pane label="命令" name="order" />
      <el-tab-pane label="个人" name="personal" />
      <el-tab-pane label="能力1" name="ability1" />
      <el-tab-pane label="能力2" name="ability2" />
    </el-tabs>

    <StrategyIntelSystemTable
      :rows="listRows"
      :columns="listColumns"
      :current-id="selectedPersonId"
      empty-text="暂无人物数据"
      @current-change="onSelectRow"
    />

    <div class="detail-section">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="属性" name="attributes" />
        <el-tab-pane label="能力" name="skills" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div v-if="detailTab === 'basic'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="basicRows.length" :rows="basicRows" />
        <p v-else class="placeholder">请选择人物。</p>
      </div>

      <div v-else-if="detailTab === 'attributes'" class="detail-body">
        <StrategyIntelPersonStats :rows="statRows" />
      </div>

      <div v-else-if="detailTab === 'skills'" class="detail-body">
        <StrategyIntelPersonSkills :rows="skillRows" />
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
</style>
