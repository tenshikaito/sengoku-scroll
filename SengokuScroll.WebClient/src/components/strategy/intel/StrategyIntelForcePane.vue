<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  DIPLOMACY_BRIEF_COLUMNS,
  FORCE_LIST_COLUMN_PRESETS,
  FORCE_PERSON_COLUMNS,
  FORCE_STRONGHOLD_COLUMNS,
  FORCE_TECHNOLOGY_COLUMNS,
  STANCE_EFFECT_COLUMNS,
  STRONGHOLD_TECHNOLOGY_COLUMNS,
  forceOrgActorTableColumns,
  type ForceListPreset,
} from "@/utils/strategyIntelSystemColumns";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import type { IntelNavigateRequest, IntelNavigateTarget } from "@/utils/strategyIntelNavigation";
import {
  diplomacyForForceRows,
  filterDiplomacyRowsByCategory,
  forceEffectsIntelRows,
  filterDiplomacyRowsByScope,
  filterForceRowsByCategory,
  forceCultureDetailRows,
  forceDetailIntelRows,
  forceHasInnerVassals,
  forceIntelListRows,
  forceIntroText,
  forceOurViewEffectRows,
  forcePersonTableRows,
  forceReligionDetailRows,
  forceStrongholdTableRows,
  forceAllStrongholdCityActorRows,
  forceTechnologyTableRows,
  forceTheirViewEffectRows,
  isOrganizationForce,
  INTEL_DIPLOMACY_SCOPE_FILTER_OPTIONS,
  INTEL_FORCE_CATEGORY_FILTER_OPTIONS,
  isForceRealmRoot,
  showForceStanceEffectTabsForForce,
  type IntelDiplomacyScopeFilter,
  type IntelForceCategoryFilter,
} from "@/utils/strategyIntelSystemData";
import "@/styles/intelCircleRadios.css";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
  /** 打开对话框时预选势力 Id。 */
  initialSelectedId?: number | null;
  /** 仅显示详情二级 Tab（隐藏列表）。 */
  detailOnly?: boolean;
  /** 调试模式：显示全部人物与隐藏属性。 */
  intelDebugMode?: boolean;
  /** 对话框内跨 Tab 跳转请求。 */
  navigateRequest?: IntelNavigateRequest | null;
}>();

const emit = defineEmits<{
  navigate: [target: IntelNavigateTarget];
}>();

const listPreset = ref<ForceListPreset>("status");
const categoryFilter = ref<IntelForceCategoryFilter>("all");
const diplomacyScopeFilter = ref<IntelDiplomacyScopeFilter>("all");
const diplomacyCategoryFilter = ref<IntelForceCategoryFilter>("all");
const includeInnerVassals = ref(false);
const detailTab = ref<
  | "basic"
  | "strongholds"
  | "persons"
  | "diplomacy"
  | "culture"
  | "religion"
  | "technology"
  | "effects"
  | "ourView"
  | "theirView"
  | "intro"
>("basic");
const selectedForceId = ref<number | null>(null);

const detailScopeOptions = computed(() => ({
  includeInnerVassals: includeInnerVassals.value,
}));

const showIncludeInnerVassalsOption = computed(() => {
  if (selectedForceId.value == null) return false;
  const force = props.worldState.forces.find((item) => item.id === selectedForceId.value);
  if (!force || force.category === "Merchant" || force.category === "Religion") return false;
  return isForceRealmRoot(props.worldState, selectedForceId.value) &&
    forceHasInnerVassals(props.worldState, selectedForceId.value);
});

const showForceStanceEffectTabs = computed(() =>
  showForceStanceEffectTabsForForce(props.worldState, selectedForceId.value),
);

const excludeEntity = computed(() =>
  selectedForceId.value != null
    ? { kind: "force" as const, entityId: selectedForceId.value }
    : null,
);

const listColumns = computed(() => FORCE_LIST_COLUMN_PRESETS[listPreset.value]);
const forceRows = computed(() =>
  filterForceRowsByCategory(
    forceIntelListRows(props.worldState, { realmFilter: props.realmFilter }),
    categoryFilter.value
  )
);
const listRows = computed(
  () => forceRows.value as unknown as Array<Record<string, unknown>>
);

const basicRows = computed(() =>
  selectedForceId.value != null
    ? forceDetailIntelRows(props.worldState, selectedForceId.value)
    : []
);

const strongholdRows = computed(() =>
  selectedForceId.value != null
    ? forceStrongholdTableRows(
        props.worldState,
        selectedForceId.value,
        detailScopeOptions.value
      )
    : []
);

const isOrganizationForceSelected = computed(() =>
  selectedForceId.value != null
    ? isOrganizationForce(props.worldState, selectedForceId.value)
    : false
);

const organizationActorKind = computed((): "Merchant" | "Religion" | null => {
  const force = props.worldState.forces.find((item) => item.id === selectedForceId.value);
  if (force?.category === "Merchant") return "Merchant";
  if (force?.category === "Religion") return "Religion";
  return null;
});

const forceAllCityActorRows = computed(() =>
  selectedForceId.value != null
    ? forceAllStrongholdCityActorRows(
        props.worldState,
        selectedForceId.value,
        detailScopeOptions.value
      )
    : []
);

const forceOrgActorColumns = computed(() =>
  organizationActorKind.value ? forceOrgActorTableColumns(organizationActorKind.value) : []
);

const personRows = computed(() =>
  selectedForceId.value != null
    ? forcePersonTableRows(
        props.worldState,
        selectedForceId.value,
        {
          ...detailScopeOptions.value,
          intelDebugMode: props.intelDebugMode,
        }
      )
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

const technologyColumns = computed(() =>
  showIncludeInnerVassalsOption.value && includeInnerVassals.value
    ? FORCE_TECHNOLOGY_COLUMNS
    : STRONGHOLD_TECHNOLOGY_COLUMNS
);

const technologyRows = computed(() =>
  selectedForceId.value != null
    ? forceTechnologyTableRows(props.worldState, selectedForceId.value, {
        ...detailScopeOptions.value,
        showForceColumn:
          showIncludeInnerVassalsOption.value && includeInnerVassals.value,
      })
    : []
);

const effectsRows = computed(() =>
  selectedForceId.value != null
    ? forceEffectsIntelRows(props.worldState, selectedForceId.value)
    : []
);

const ourViewEffectListRows = computed(
  () => ourViewEffectRows.value as unknown as Array<Record<string, unknown>>
);

const theirViewEffectListRows = computed(
  () => theirViewEffectRows.value as unknown as Array<Record<string, unknown>>
);

const ourViewEffectRows = computed(() =>
  selectedForceId.value != null
    ? forceOurViewEffectRows(props.worldState, selectedForceId.value)
    : []
);

const theirViewEffectRows = computed(() =>
  selectedForceId.value != null
    ? forceTheirViewEffectRows(props.worldState, selectedForceId.value)
    : []
);

const diplomacyRows = computed(() => {
  if (selectedForceId.value == null) return [];
  const rows = diplomacyForForceRows(props.worldState, selectedForceId.value);
  return filterDiplomacyRowsByCategory(
    filterDiplomacyRowsByScope(rows, diplomacyScopeFilter.value),
    diplomacyCategoryFilter.value
  );
});

const strongholdListRows = computed(
  () => strongholdRows.value as unknown as Array<Record<string, unknown>>
);
const forceAllCityActorListRows = computed(
  () => forceAllCityActorRows.value as unknown as Array<Record<string, unknown>>
);
const personListRows = computed(
  () => personRows.value as unknown as Array<Record<string, unknown>>
);
const technologyListRows = computed(
  () => technologyRows.value as unknown as Array<Record<string, unknown>>
);
const diplomacyListRows = computed(
  () => diplomacyRows.value as unknown as Array<Record<string, unknown>>
);

const introText = computed(() =>
  selectedForceId.value != null
    ? forceIntroText(props.worldState, selectedForceId.value)
    : "请在上方列表选择势力。"
);

const showDiplomacyScopeFilter = computed(() => {
  if (selectedForceId.value == null) return false;
  const row = forceRows.value.find((item) => item.id === selectedForceId.value);
  if (!row || row.forceType !== "武家" || row.status === "内藩") return false;
  return row.isOwnRealm && isForceRealmRoot(props.worldState, row.id);
});

function defaultDetailTabForForce(forceId: number): "basic" | "persons" {
  const force = props.worldState.forces.find((item) => item.id === forceId);
  if (force?.category === "Merchant" || force?.category === "Religion") return "persons";
  return "basic";
}

function onSelectRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultSelection();
    return;
  }
  selectedForceId.value = Number(row.id);
}

function onIntelNavigate(target: IntelNavigateTarget) {
  emit("navigate", target);
}

watch(showForceStanceEffectTabs, (visible) => {
  if (!visible && (detailTab.value === "ourView" || detailTab.value === "theirView")) {
    detailTab.value = "basic";
  }
});

watch(
  () => props.navigateRequest,
  (request) => {
    if (!request || request.kind !== "force") return;
    selectedForceId.value = request.entityId;
    detailTab.value = defaultDetailTabForForce(request.entityId);
  },
);

function defaultForceId(): number | null {
  const playerRoot = props.worldState.playerForceId;
  const rows = forceRows.value;
  if (rows.some((r) => r.id === playerRoot)) return playerRoot;
  return rows[0]?.id ?? null;
}

function syncDefaultSelection() {
  if (props.detailOnly && props.initialSelectedId != null) {
    selectedForceId.value = props.initialSelectedId;
    return;
  }
  const rows = forceRows.value;
  if (selectedForceId.value != null && rows.some((r) => r.id === selectedForceId.value)) {
    return;
  }
  selectedForceId.value = defaultForceId();
}

watch(
  () => props.initialSelectedId,
  (id) => {
    if (id == null) return;
    selectedForceId.value = id;
    detailTab.value = defaultDetailTabForForce(id);
  },
  { immediate: true }
);

watch(
  () => [props.worldState, props.realmFilter, categoryFilter.value] as const,
  () => syncDefaultSelection(),
  { immediate: true }
);

</script>

<template>
  <div class="intel-pane" :class="{ 'intel-pane--detail-only': detailOnly }">
    <template v-if="!detailOnly">
      <el-radio-group
        v-model="categoryFilter"
        class="force-filter-bar intel-circle-radios"
        aria-label="势力类型筛选"
      >
        <el-radio
          v-for="option in INTEL_FORCE_CATEGORY_FILTER_OPTIONS"
          :key="option.value"
          :value="option.value"
        >
          {{ option.label }}
        </el-radio>
      </el-radio-group>

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
        @navigate="onIntelNavigate"
      />
    </template>

    <div class="detail-section" :class="{ 'detail-section--solo': detailOnly }">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="据点" name="strongholds" />
        <el-tab-pane label="现任" name="persons" />
        <el-tab-pane label="外交" name="diplomacy" />
        <el-tab-pane label="文化" name="culture" />
        <el-tab-pane label="信仰" name="religion" />
        <el-tab-pane label="技术" name="technology" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane v-if="showForceStanceEffectTabs" label="本家看法" name="ourView" />
        <el-tab-pane v-if="showForceStanceEffectTabs" label="对本家的看法" name="theirView" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div
        v-if="showIncludeInnerVassalsOption && ['strongholds', 'persons', 'technology'].includes(detailTab)"
        class="intel-detail-scope-bar"
      >
        <el-checkbox v-model="includeInnerVassals">包含内藩</el-checkbox>
      </div>

      <div v-if="detailTab === 'basic'" class="detail-body">
        <StrategyIntelBasicDescriptions
          v-if="basicRows.length"
          :rows="basicRows"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'strongholds'" class="detail-body">
        <StrategyIntelSystemTable
          v-if="!isOrganizationForceSelected"
          :rows="strongholdListRows"
          :columns="FORCE_STRONGHOLD_COLUMNS"
          :highlight-current="false"
          empty-text="暂无据点数据"
          :max-height="240"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
        <StrategyIntelSystemTable
          v-else
          :rows="forceAllCityActorListRows"
          :columns="forceOrgActorColumns"
          :highlight-current="false"
          empty-text="暂无城内势力情报"
          :max-height="320"
          scroll-wrap
          fill-width
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'persons'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="personListRows"
          :columns="FORCE_PERSON_COLUMNS"
          :highlight-current="false"
          empty-text="暂无现任"
          :max-height="240"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'diplomacy'" class="detail-body">
        <div v-if="showDiplomacyScopeFilter" class="diplomacy-filter-section">
          <span class="diplomacy-filter-label">关系范围</span>
          <el-radio-group
            v-model="diplomacyScopeFilter"
            class="intel-circle-radios diplomacy-filter-bar"
            aria-label="外交关系筛选"
          >
            <el-radio
              v-for="option in INTEL_DIPLOMACY_SCOPE_FILTER_OPTIONS"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </el-radio>
          </el-radio-group>
        </div>
        <div class="diplomacy-filter-section">
          <span class="diplomacy-filter-label">势力类型</span>
          <el-radio-group
            v-model="diplomacyCategoryFilter"
            class="intel-circle-radios diplomacy-filter-bar"
            aria-label="外交势力类型筛选"
          >
            <el-radio
              v-for="option in INTEL_FORCE_CATEGORY_FILTER_OPTIONS"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </el-radio>
          </el-radio-group>
        </div>
        <StrategyIntelSystemTable
          :rows="diplomacyListRows"
          :columns="DIPLOMACY_BRIEF_COLUMNS"
          :highlight-current="false"
          empty-text="暂无外交情报"
          :max-height="200"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'culture'" class="detail-body">
        <StrategyIntelBasicDescriptions
          v-if="cultureRows.length"
          :rows="cultureRows"
          :column="1"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'religion'" class="detail-body">
        <StrategyIntelBasicDescriptions
          v-if="religionRows.length"
          :rows="religionRows"
          :column="1"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'technology'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="technologyListRows"
          :columns="technologyColumns"
          :highlight-current="false"
          empty-text="暂无技术数据"
          :max-height="200"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'effects'" class="detail-body">
        <StrategyIntelBasicDescriptions
          :rows="effectsRows"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'ourView'" class="detail-body">
        <StrategyIntelSystemTable
          v-if="selectedForceId != null"
          :rows="ourViewEffectListRows"
          :columns="STANCE_EFFECT_COLUMNS"
          :highlight-current="false"
          empty-text="暂无本家对该势力的看法记录"
          :max-height="220"
          fill-width
        />
        <p v-else class="placeholder">请选择势力。</p>
      </div>

      <div v-else-if="detailTab === 'theirView'" class="detail-body">
        <StrategyIntelSystemTable
          v-if="selectedForceId != null"
          :rows="theirViewEffectListRows"
          :columns="STANCE_EFFECT_COLUMNS"
          :highlight-current="false"
          empty-text="暂无该势力对本家的看法记录"
          :max-height="220"
          fill-width
        />
        <p v-else class="placeholder">请选择势力。</p>
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

.force-filter-bar {
  margin-bottom: 2px;
}

.diplomacy-filter-section {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px 12px;
  margin-bottom: 8px;
}

.diplomacy-filter-label {
  flex: 0 0 auto;
  min-width: 4.5rem;
  font-size: 0.82rem;
  color: #475569;
}

.diplomacy-filter-bar {
  margin-bottom: 0;
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
