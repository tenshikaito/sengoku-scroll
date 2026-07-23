<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import {
  cityActorTableColumns,
  STRONGHOLD_CROP_CYCLE_COLUMNS,
  STRONGHOLD_DEFENSE_COLUMNS,
  STRONGHOLD_FACTION_PERSON_COLUMNS,
  STRONGHOLD_FACTION_PRODUCTION_COLUMNS,
  STRONGHOLD_LIST_COLUMN_PRESETS,
  STRONGHOLD_STANDING_GARRISON_COLUMNS,
  STRONGHOLD_TECHNOLOGY_COLUMNS,
  type StrongholdListPreset,
} from "@/utils/strategyIntelSystemColumns";
import {
  strongholdCropCycleTableRows,
  strongholdCultureDetailRows,
  strongholdDefenseFacilityTableRows,
  strongholdDetailFieldRows,
  strongholdEffectsDetailRows,
  strongholdFactionPersonRows,
  strongholdFactionProductionRows,
  strongholdIntelRows,
  strongholdIntroText,
  strongholdMerchantTableRows,
  strongholdReligionDetailRows,
  strongholdStandingGarrisonTableRows,
  strongholdTechnologyTableRows,
  strongholdTempleTableRows,
  type IntelStrongholdRow,
} from "@/utils/strategyIntelSystemData";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
  /** 打开对话框时预选据点 Id。 */
  initialSelectedId?: number | null;
  /** 仅显示详情二级 Tab（隐藏列表）。 */
  detailOnly?: boolean;
}>();

const listPreset = ref<StrongholdListPreset>("status");
const detailTab = ref<
  | "basic"
  | "garrison"
  | "defense"
  | "production"
  | "merchants"
  | "temples"
  | "culture"
  | "religion"
  | "technology"
  | "effects"
  | "intro"
>("basic");
const selectedStrongholdId = ref<number | null>(null);
const selectedCityActorId = ref<number | null>(null);
const cityActorContentTab = ref<"persons" | "production">("persons");

const activeCityActorKind = computed<"Merchant" | "Religion" | null>(() => {
  if (detailTab.value === "merchants") return "Merchant";
  if (detailTab.value === "temples") return "Religion";
  return null;
});

const cityActorRows = computed(() => {
  if (selectedStrongholdId.value == null || activeCityActorKind.value == null) return [];
  return activeCityActorKind.value === "Merchant"
    ? strongholdMerchantTableRows(props.worldState, selectedStrongholdId.value)
    : strongholdTempleTableRows(props.worldState, selectedStrongholdId.value);
});

const cityActorColumns = computed(() =>
  activeCityActorKind.value != null
    ? cityActorTableColumns(activeCityActorKind.value)
    : []
);

const factionPersonRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdFactionPersonRows(
        props.worldState,
        selectedStrongholdId.value,
        selectedCityActorId.value
      )
    : []
);

const factionProductionRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdFactionProductionRows(
        props.worldState,
        selectedStrongholdId.value,
        selectedCityActorId.value
      )
    : []
);

const factionPersonListRows = computed(
  () => factionPersonRows.value as unknown as Array<Record<string, unknown>>
);
const factionProductionListRows = computed(
  () => factionProductionRows.value as unknown as Array<Record<string, unknown>>
);

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

const effectsRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdEffectsDetailRows(props.worldState, selectedStrongholdId.value)
    : []
);

const technologyRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdTechnologyTableRows(props.worldState, selectedStrongholdId.value)
    : []
);

const defenseRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdDefenseFacilityTableRows(props.worldState, selectedStrongholdId.value)
    : []
);

const standingGarrisonRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdStandingGarrisonTableRows(props.worldState, selectedStrongholdId.value)
    : []
);

const cropCycleRows = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdCropCycleTableRows(props.worldState, selectedStrongholdId.value)
    : []
);

const defenseListRows = computed(
  () => defenseRows.value as unknown as Array<Record<string, unknown>>
);
const standingGarrisonListRows = computed(
  () => standingGarrisonRows.value as unknown as Array<Record<string, unknown>>
);
const cropCycleListRows = computed(
  () => cropCycleRows.value as unknown as Array<Record<string, unknown>>
);
const cityActorListRows = computed(
  () => cityActorRows.value as unknown as Array<Record<string, unknown>>
);
const technologyListRows = computed(
  () => technologyRows.value as unknown as Array<Record<string, unknown>>
);

const introText = computed(() =>
  selectedStrongholdId.value != null
    ? strongholdIntroText(props.worldState, selectedStrongholdId.value)
    : "请在上方列表选择据点。"
);

const cityActorEmptyLabel = computed(() =>
  detailTab.value === "merchants" ? "暂无商家" : "暂无寺社"
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
  if (props.detailOnly && props.initialSelectedId != null) {
    selectedStrongholdId.value = props.initialSelectedId;
    return;
  }
  const rows = strongholdRows.value;
  const preferred = props.initialSelectedId;
  if (preferred != null && rows.some((r) => r.id === preferred)) {
    selectedStrongholdId.value = preferred;
    return;
  }
  if (
    selectedStrongholdId.value != null &&
    rows.some((r) => r.id === selectedStrongholdId.value)
  ) {
    return;
  }
  selectedStrongholdId.value = defaultStrongholdId();
}

function syncDefaultCityActorSelection() {
  const rows = cityActorRows.value;
  if (rows.length === 0) {
    selectedCityActorId.value = null;
    return;
  }
  if (selectedCityActorId.value != null && rows.some((row) => row.id === selectedCityActorId.value)) {
    return;
  }
  selectedCityActorId.value = rows[0]?.id ?? null;
}

watch(
  () => selectedStrongholdId.value,
  () => {
    selectedCityActorId.value = null;
    cityActorContentTab.value = "persons";
    syncDefaultCityActorSelection();
  }
);

watch(detailTab, (tab) => {
  if (tab === "merchants" || tab === "temples") {
    cityActorContentTab.value = "persons";
  }
});

watch(
  () => [cityActorRows.value, detailTab.value] as const,
  () => syncDefaultCityActorSelection(),
  { immediate: true }
);

watch(
  () => [props.worldState, props.realmFilter] as const,
  () => syncDefaultSelection(),
  { immediate: true }
);

watch(
  () => props.initialSelectedId,
  (id) => {
    if (id == null) return;
    const rows = strongholdRows.value;
    if (rows.some((r) => r.id === id)) {
      selectedStrongholdId.value = id;
      detailTab.value = "basic";
    }
  },
  { immediate: true }
);

function onSelectRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultSelection();
    return;
  }
  selectedStrongholdId.value = Number(row.id);
}

function onSelectCityActorRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultCityActorSelection();
    return;
  }
  selectedCityActorId.value = Number(row.id);
}

function rowClass(row: Record<string, unknown>) {
  return (row as unknown as IntelStrongholdRow).isLordResidence === "○" ? "is-lord-row" : "";
}
</script>

<template>
  <div class="intel-pane" :class="{ 'intel-pane--detail-only': detailOnly }">
    <template v-if="!detailOnly">
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
    </template>

    <div class="detail-section" :class="{ 'detail-section--solo': detailOnly }">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="常备兵" name="garrison" />
        <el-tab-pane label="城防" name="defense" />
        <el-tab-pane label="生产" name="production" />
        <el-tab-pane label="商家" name="merchants" />
        <el-tab-pane label="寺社" name="temples" />
        <el-tab-pane label="文化" name="culture" />
        <el-tab-pane label="信仰" name="religion" />
        <el-tab-pane label="技术" name="technology" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div v-if="detailTab === 'basic'" class="detail-body detail-body--basic">
        <StrategyIntelBasicDescriptions v-if="basicRows.length" :rows="basicRows" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'garrison'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="standingGarrisonListRows"
          :columns="STRONGHOLD_STANDING_GARRISON_COLUMNS"
          :highlight-current="false"
          empty-text="暂无常备兵"
          :max-height="220"
        />
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

      <div v-else-if="detailTab === 'production'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="cropCycleListRows"
          :columns="STRONGHOLD_CROP_CYCLE_COLUMNS"
          :highlight-current="false"
          empty-text="暂无生产数据"
          :max-height="220"
        />
      </div>

      <div
        v-else-if="detailTab === 'merchants' || detailTab === 'temples'"
        class="detail-body detail-body--city-actors"
      >
        <StrategyIntelSystemTable
          :rows="cityActorListRows"
          :columns="cityActorColumns"
          :current-id="selectedCityActorId"
          :empty-text="cityActorEmptyLabel"
          :max-height="160"
          @current-change="onSelectCityActorRow"
        />

        <div class="city-actor-subsection">
          <el-tabs v-model="cityActorContentTab" class="layer-tabs layer-tabs--city-content">
            <el-tab-pane label="现任" name="persons" />
            <el-tab-pane label="生产" name="production" />
          </el-tabs>

          <div v-if="cityActorContentTab === 'persons'" class="city-actor-subsection-body">
            <StrategyIntelSystemTable
              v-if="selectedCityActorId != null && factionPersonRows.length"
              :rows="factionPersonListRows"
              :columns="STRONGHOLD_FACTION_PERSON_COLUMNS"
              :highlight-current="false"
              empty-text="暂无现任"
              scroll-wrap
              fill-width
              :max-height="220"
            />
            <p v-else class="placeholder">
              {{
                cityActorRows.length
                  ? selectedCityActorId != null
                    ? "该势力暂无现任。"
                    : "请选择上方势力。"
                  : cityActorEmptyLabel
              }}
            </p>
          </div>

          <div v-else class="city-actor-subsection-body">
            <StrategyIntelSystemTable
              v-if="selectedCityActorId != null && factionProductionRows.length"
              :rows="factionProductionListRows"
              :columns="STRONGHOLD_FACTION_PRODUCTION_COLUMNS"
              :highlight-current="false"
              empty-text="暂无生产数据"
              :max-height="160"
            />
            <p v-else class="placeholder">
              {{
                cityActorRows.length
                  ? selectedCityActorId != null
                    ? "该势力暂无生产项目。"
                    : "请选择上方势力。"
                  : cityActorEmptyLabel
              }}
            </p>
          </div>
        </div>
      </div>

      <div v-else-if="detailTab === 'culture'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="cultureRows.length" :rows="cultureRows" :column="1" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'religion'" class="detail-body">
        <StrategyIntelBasicDescriptions v-if="religionRows.length" :rows="religionRows" :column="1" />
        <p v-else class="placeholder">请选择据点。</p>
      </div>

      <div v-else-if="detailTab === 'technology'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="technologyListRows"
          :columns="STRONGHOLD_TECHNOLOGY_COLUMNS"
          :highlight-current="false"
          empty-text="暂无技术数据"
          :max-height="220"
        />
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

.detail-section--solo {
  border-top: none;
  padding-top: 0;
}

.detail-body {
  min-height: 120px;
  max-height: 280px;
  overflow: auto;
}

.detail-body--basic {
  max-height: none;
}

.detail-body--city-actors {
  max-height: none;
}

.city-actor-subsection {
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px dashed #e2e8f0;
}

.city-actor-subsection-body {
  min-height: 100px;
  max-height: 200px;
  overflow: auto;
}

.layer-tabs--city-content :deep(.el-tabs__item) {
  font-size: 0.82rem;
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
