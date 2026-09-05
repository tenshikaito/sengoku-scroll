<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelBasicDescriptions from "../StrategyIntelBasicDescriptions.vue";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import type { IntelNavigateRequest, IntelNavigateTarget } from "@/utils/strategyIntelNavigation";
import { resolvePlayerLordCharacterId } from "@/utils/strategyPlayerCharacter";
import StrategyIntelPersonSkills from "../StrategyIntelPersonSkills.vue";
import StrategyIntelPersonStats from "../StrategyIntelPersonStats.vue";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  PERSON_RELATION_COLUMNS,
  PERSON_TASK_COLUMNS,
  STANCE_EFFECT_COLUMNS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import {
  filterPersonRowsByCategory,
  personEffectsIntelRows,
  INTEL_PERSON_CATEGORY_FILTER_OPTIONS,
  personDetailIntelRows,
  personIntelRows,
  personIntroText,
  personViewOfCharacterRows,
  personRelationshipTableRows,
  personSkillDetailRows,
  personStatDetailRows,
  personTaskTableRows,
  personCharacterViewOfLordRows,
  type IntelPersonCategoryFilter,
} from "@/utils/strategyIntelSystemData";
import "@/styles/intelCircleRadios.css";

const props = defineProps<{
  worldState: StrategyWorldState;
  realmFilter?: IntelRealmFilterMode;
  /** 打开对话框时预选人物 Id。 */
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
  interact: [targetCharacterId: number, interaction: "Talk" | "Gift" | "Marry" | "DeclineMarriage"];
}>();

const listPreset = ref<PersonListPreset>("status");
const categoryFilter = ref<IntelPersonCategoryFilter>("all");
const detailTab = ref<
  | "basic"
  | "attributes"
  | "skills"
  | "tasks"
  | "relations"
  | "effects"
  | "viewOfCharacter"
  | "characterView"
  | "intro"
>("basic");
const selectedPersonId = ref<number | null>(null);

const playerLordCharacterId = computed(() => resolvePlayerLordCharacterId(props.worldState));
const playerLord = computed(() =>
  props.worldState.characters?.find((item) => item.id === playerLordCharacterId.value),
);
const selectedPerson = computed(() =>
  props.worldState.characters?.find((item) => item.id === selectedPersonId.value),
);
const canInteract = computed(() => {
  const lord = playerLord.value;
  const target = selectedPerson.value;
  if (!lord || !target || lord.id === target.id || props.worldState.allForcesAiControlled) return false;
  return lord.locationType === "Stronghold"
    && target.locationType === "Stronghold"
    && (lord.strongholdId ?? 0) > 0
    && lord.strongholdId === target.strongholdId;
});
const interactionHint = computed(() => {
  if (props.worldState.allForcesAiControlled) return "观战模式不可手动干预";
  if (selectedPersonId.value === playerLordCharacterId.value) return "不能与自己互动";
  return canInteract.value ? "交谈每天一次；金钱赠礼每7天一次；婚约须双方成年、无近亲且同意" : "当主与目标必须在同一据点";
});

const showPersonStanceEffectTabs = computed(() => {
  if (selectedPersonId.value == null) return false;
  const lordId = playerLordCharacterId.value;
  if (lordId != null && selectedPersonId.value === lordId) return false;
  return props.worldState.characters?.some((item) => item.id === selectedPersonId.value) ?? false;
});

const personRows = computed(() =>
  filterPersonRowsByCategory(
    personIntelRows(props.worldState, {
      realmFilter: props.realmFilter,
      intelDebugMode: props.intelDebugMode,
    }),
    categoryFilter.value
  )
);
const listColumns = computed(() => {
  const cols = PERSON_LIST_COLUMN_PRESETS[listPreset.value];
  if (listPreset.value === "personal" && !isIntelDevFieldsVisible(props.intelDebugMode)) {
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
    ? personDetailIntelRows(props.worldState, selectedPersonId.value, props.intelDebugMode)
    : []
);

const statRows = computed(() =>
  selectedPersonId.value != null
    ? personStatDetailRows(props.worldState, selectedPersonId.value, props.intelDebugMode)
    : []
);

const skillRows = computed(() =>
  selectedPersonId.value != null
    ? personSkillDetailRows(props.worldState, selectedPersonId.value, props.intelDebugMode)
    : []
);

const effectsRows = computed(() =>
  selectedPersonId.value != null
    ? personEffectsIntelRows(props.worldState, selectedPersonId.value)
    : []
);

const viewOfCharacterRows = computed(() =>
  selectedPersonId.value != null
    ? personViewOfCharacterRows(props.worldState, selectedPersonId.value)
    : []
);

const characterViewOfLordRows = computed(() =>
  selectedPersonId.value != null
    ? personCharacterViewOfLordRows(props.worldState, selectedPersonId.value)
    : []
);

const viewOfCharacterListRows = computed(
  () => viewOfCharacterRows.value as unknown as Array<Record<string, unknown>>
);

const characterViewOfLordListRows = computed(
  () => characterViewOfLordRows.value as unknown as Array<Record<string, unknown>>
);

const relationRows = computed(() =>
  selectedPersonId.value != null
    ? personRelationshipTableRows(props.worldState, selectedPersonId.value)
    : [],
);

const taskRows = computed(() =>
  selectedPersonId.value != null
    ? personTaskTableRows(props.worldState, selectedPersonId.value)
    : [],
);

const taskListRows = computed(
  () => taskRows.value as unknown as Array<Record<string, unknown>>,
);

const relationListRows = computed(
  () => relationRows.value as unknown as Array<Record<string, unknown>>,
);

const introText = computed(() =>
  selectedPersonId.value != null
    ? personIntroText(props.worldState, selectedPersonId.value, props.intelDebugMode)
    : "请在上方列表选择人物。"
);

const excludeEntity = computed(() =>
  selectedPersonId.value != null
    ? { kind: "person" as const, entityId: selectedPersonId.value }
    : null,
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
  if (props.detailOnly && props.initialSelectedId != null) {
    selectedPersonId.value = props.initialSelectedId;
    return;
  }
  const rows = personRows.value;
  const preferred = props.initialSelectedId;
  if (preferred != null && rows.some((r) => r.id === preferred)) {
    selectedPersonId.value = preferred;
    return;
  }
  if (selectedPersonId.value != null && rows.some((r) => r.id === selectedPersonId.value)) {
    return;
  }
  selectedPersonId.value = defaultPersonId();
}

watch(showPersonStanceEffectTabs, (visible) => {
  if (!visible && (detailTab.value === "viewOfCharacter" || detailTab.value === "characterView")) {
    detailTab.value = "basic";
  }
});

watch(
  () => [props.worldState, props.realmFilter, categoryFilter.value, props.intelDebugMode] as const,
  () => syncDefaultSelection(),
  { immediate: true }
);

watch(
  () => props.initialSelectedId,
  (id) => {
    if (id == null) return;
    const rows = personRows.value;
    if (rows.some((r) => r.id === id)) {
      selectedPersonId.value = id;
      detailTab.value = "basic";
    }
  },
  { immediate: true }
);

function onIntelNavigate(target: IntelNavigateTarget) {
  emit("navigate", target);
}

function onSelectRow(row: Record<string, unknown> | null) {
  if (!row) {
    syncDefaultSelection();
    return;
  }
  selectedPersonId.value = Number(row.id);
}
watch(
  () => props.navigateRequest,
  (request) => {
    if (!request || request.kind !== "person") return;
    selectedPersonId.value = request.entityId;
    detailTab.value = "basic";
  },
);
</script>

<template>
  <div class="intel-pane" :class="{ 'intel-pane--detail-only': detailOnly }">
    <template v-if="!detailOnly">
      <el-radio-group
        v-model="categoryFilter"
        class="person-filter-bar intel-circle-radios"
        aria-label="人物类型筛选"
      >
        <el-radio
          v-for="option in INTEL_PERSON_CATEGORY_FILTER_OPTIONS"
          :key="option.value"
          :value="option.value"
        >
          {{ option.label }}
        </el-radio>
      </el-radio-group>

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
        @navigate="onIntelNavigate"
      />
    </template>

    <div class="detail-section" :class="{ 'detail-section--solo': detailOnly }">
      <el-tabs v-model="detailTab" class="layer-tabs layer-tabs--detail">
        <el-tab-pane label="基本" name="basic" />
        <el-tab-pane label="属性" name="attributes" />
        <el-tab-pane label="能力" name="skills" />
        <el-tab-pane label="任务" name="tasks" />
        <el-tab-pane label="人际关系" name="relations" />
        <el-tab-pane label="影响" name="effects" />
        <el-tab-pane v-if="showPersonStanceEffectTabs" label="本人看法" name="viewOfCharacter" />
        <el-tab-pane v-if="showPersonStanceEffectTabs" label="对本人的看法" name="characterView" />
        <el-tab-pane label="介绍" name="intro" />
      </el-tabs>

      <div v-if="detailTab === 'basic'" class="detail-body">
        <StrategyIntelBasicDescriptions
          v-if="basicRows.length"
          :rows="basicRows"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
        <p v-else class="placeholder">请选择人物。</p>
        <div v-if="selectedPersonId != null && selectedPersonId !== playerLordCharacterId" class="social-actions">
          <span class="social-actions__hint">{{ interactionHint }}</span>
          <el-button
            size="small"
            :disabled="!canInteract || (worldState.lord.ap ?? 0) < 1"
            @click="emit('interact', selectedPersonId, 'Talk')"
          >
            交谈（1 AP）
          </el-button>
          <el-button
            size="small"
            type="primary"
            plain
            :disabled="!canInteract || (worldState.lord.ap ?? 0) < 2 || (playerLord?.money ?? 0) < 100"
            @click="emit('interact', selectedPersonId, 'Gift')"
          >
            赠礼（2 AP / 100文）
          </el-button>
          <el-button size="small" :disabled="!canInteract || (worldState.lord.ap ?? 0) < 2"
            @click="emit('interact', selectedPersonId, 'Marry')">
            {{ playerLord?.pendingMarriageFromId === selectedPersonId ? '同意婚约' : '提出婚约' }}（2 AP）
          </el-button>
          <el-button v-if="playerLord?.pendingMarriageFromId === selectedPersonId" size="small" :disabled="!canInteract"
            @click="emit('interact', selectedPersonId, 'DeclineMarriage')">拒绝婚约</el-button>
        </div>
        <p v-if="selectedPerson?.defectionWarningDay != null">⚠ 此人物已有投奔预警，请改善其对当主的关系与忠诚。</p>
        <h4 v-if="selectedPerson?.socialMemories?.length">近期人物记忆（最多64条，仅本势力可见）</h4>
        <ul v-if="selectedPerson?.socialMemories?.length">
          <li v-for="memory in [...selectedPerson.socialMemories].reverse()" :key="memory.id">
            第 {{ memory.day }} 日 · {{ memory.description }} · 对象 #{{ memory.otherCharacterId }}
          </li>
        </ul>
      </div>

      <div v-else-if="detailTab === 'attributes'" class="detail-body">
        <StrategyIntelPersonStats :rows="statRows" />
      </div>

      <div v-else-if="detailTab === 'skills'" class="detail-body">
        <StrategyIntelPersonSkills :rows="skillRows" />
      </div>

      <div v-else-if="detailTab === 'tasks'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="taskListRows"
          :columns="PERSON_TASK_COLUMNS"
          :highlight-current="false"
          empty-text="暂无任务"
          :max-height="220"
          :exclude-entity="excludeEntity"
          @navigate="onIntelNavigate"
        />
      </div>

      <div v-else-if="detailTab === 'relations'" class="detail-body">
        <StrategyIntelSystemTable
          :rows="relationListRows"
          :columns="PERSON_RELATION_COLUMNS"
          :highlight-current="false"
          empty-text="暂无特殊人际关系"
          :max-height="220"
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

      <div v-else-if="detailTab === 'viewOfCharacter'" class="detail-body">
        <StrategyIntelSystemTable
          v-if="selectedPersonId != null"
          :rows="viewOfCharacterListRows"
          :columns="STANCE_EFFECT_COLUMNS"
          :highlight-current="false"
          empty-text="暂无本人看法记录"
          :max-height="220"
          fill-width
        />
        <p v-else class="placeholder">请选择人物。</p>
      </div>

      <div v-else-if="detailTab === 'characterView'" class="detail-body">
        <StrategyIntelSystemTable
          v-if="selectedPersonId != null"
          :rows="characterViewOfLordListRows"
          :columns="STANCE_EFFECT_COLUMNS"
          :highlight-current="false"
          empty-text="暂无对本人的看法记录"
          :max-height="220"
          fill-width
        />
        <p v-else class="placeholder">请选择人物。</p>
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

.person-filter-bar {
  margin-bottom: 2px;
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

.social-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
  padding-top: 10px;
  border-top: 1px solid #e2e8f0;
}

.social-actions__hint {
  margin-right: auto;
  font-size: 12px;
  color: #64748b;
}
</style>
