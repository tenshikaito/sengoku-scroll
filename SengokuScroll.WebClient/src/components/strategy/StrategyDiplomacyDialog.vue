<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategyTypes";
import type { StrategyPeaceTermsPayload } from "@/api/strategy";
import StrategyCharacterSpeechBubble from "@/components/strategy/StrategyCharacterSpeechBubble.vue";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  DIPLOMACY_BRIEF_COLUMNS,
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import {
  diplomacyMissionTargetRows,
  validateDiplomacyMissionTarget,
  type DiplomacyMissionActionKind,
} from "@/utils/strategyIntelSystemData";
import { lordResidenceOfficerRows } from "@/utils/strategyRecruitDialogs";

export type DiplomacyMissionAction = DiplomacyMissionActionKind;

const props = defineProps<{
  visible: boolean;
  action: DiplomacyMissionAction;
  worldState: StrategyWorldState;
  lordResidenceStrongholdId: number | null;
  initialTargetForceId?: number | null;
  successChancePercent: number | null;
  travelDays: number | null;
  previewLoading?: boolean;
  peaceRequiredWarScore?: number | null;
  peaceCanForceAcceptance?: boolean;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  "update:targetForceId": [forceId: number | null];
  "update:characterId": [characterId: number | null];
  requestPreview: [];
  "update:peaceTerms": [terms: Omit<StrategyPeaceTermsPayload, "characterId" | "targetForceId">];
  pickForceFromMap: [];
  confirm: [];
}>();

const characterId = ref<number | null>(null);
const targetForceId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("status");
const cededStrongholdIds = ref<number[]>([]);
const reparationsMoney = ref(0);
const demandOuterVassalage = ref(false);

const actionLabel = computed(() => {
  switch (props.action) {
    case "Ally":
      return "提议同盟";
    case "War":
      return "宣战";
    case "Peace":
      return "议和";
    default:
      return "外交";
  }
});

const officerRows = computed(() =>
  lordResidenceOfficerRows(props.worldState, props.lordResidenceStrongholdId),
);

const personListRows = computed(
  () => officerRows.value as unknown as Array<Record<string, unknown>>,
);

const personListColumns = computed(() => {
  const cols = PERSON_LIST_COLUMN_PRESETS[personListPreset.value];
  if (personListPreset.value === "personal" && !isIntelDevFieldsVisible()) {
    const devProps = new Set<string>(PERSON_PERSONAL_DEV_ONLY_PROPS);
    return cols.filter((col) => !devProps.has(col.prop));
  }
  return cols;
});

const selectedCharacter = computed(() =>
  props.worldState.characters?.find((c) => c.id === characterId.value) ?? null,
);

const selectedCharacterName = computed(
  () => selectedCharacter.value?.name?.trim() || "将领",
);

const targetForceRows = computed(() =>
  diplomacyMissionTargetRows(props.worldState).map((row) => ({
    ...row,
    id: row.forceId,
  })),
);

const targetForce = computed(() =>
  props.worldState.forces.find((force) => force.id === targetForceId.value) ?? null,
);

const targetStrongholds = computed(() =>
  props.worldState.strongholds
    .filter((stronghold) => stronghold.forceId === targetForceId.value)
    .sort((a, b) => a.id - b.id),
);

const activeWar = computed(() =>
  props.worldState.wars?.find((war) => {
    const playerOnAggressor = war.aggressorForceIds.includes(props.worldState.playerForceId);
    const opposite = playerOnAggressor ? war.defenderForceIds : war.aggressorForceIds;
    return targetForceId.value != null && opposite.includes(targetForceId.value);
  }) ?? null,
);

const canDemandVassalage = computed(
  () =>
    targetForce.value?.status === "Independence" &&
    props.worldState.forces.find((force) => force.id === props.worldState.playerForceId)?.status ===
      "Independence",
);

const peaceTermValidationError = computed(() => {
  if (props.action !== "Peace") return null;
  if (cededStrongholdIds.value.length > 0 && cededStrongholdIds.value.length >= targetStrongholds.value.length) {
    return "和谈不能割走对方全部据点；至少须保留一座居城";
  }
  if (demandOuterVassalage.value && !canDemandVassalage.value) {
    return "只有两个独立势力之间才能要求外藩臣服";
  }
  return null;
});

const targetValidationError = computed(() => {
  if (targetForceId.value == null || targetForceId.value <= 0) return null;
  return validateDiplomacyMissionTarget(props.worldState, props.action, targetForceId.value);
});

const characterSelectionError = computed(() => {
  const character = selectedCharacter.value;
  if (!character) return null;
  if (character.forceStatus !== "Idle") return "该将领当前非待命，无法担任使节";
  return null;
});

const speechTone = computed((): "default" | "warning" | "muted" => {
  if (targetValidationError.value || characterSelectionError.value || peaceTermValidationError.value) return "warning";
  if (props.previewLoading) return "muted";
  return "default";
});

const speechMessage = computed(() => {
  if (officerRows.value.length === 0) {
    return "居城暂无现任将领。";
  }
  if (characterId.value == null) {
    return "请先选择将领与目标势力。";
  }
  if (characterSelectionError.value) {
    return characterSelectionError.value;
  }
  if (targetForceId.value == null || targetForceId.value <= 0) {
    return "请选择目标势力。";
  }
  if (targetValidationError.value) {
    return targetValidationError.value;
  }
  if (peaceTermValidationError.value) {
    return peaceTermValidationError.value;
  }
  if (props.previewLoading) {
    return "正在估算成功率…";
  }
  if (props.successChancePercent != null) {
    const travel =
      props.travelDays != null ? `，预计 ${props.travelDays} 日抵达` : "";
    return `我认为此次${actionLabel.value}的成功率约为 ${props.successChancePercent}%${travel}。`;
  }
  return "正在准备估算…";
});

const canConfirm = computed(
  () =>
    characterId.value != null &&
    targetForceId.value != null &&
    targetForceId.value > 0 &&
    targetValidationError.value == null &&
    characterSelectionError.value == null &&
    peaceTermValidationError.value == null &&
    !props.previewLoading,
);

function resolveInitialTargetId(): number | null {
  const preferred = props.initialTargetForceId;
  if (
    preferred != null &&
    targetForceRows.value.some((row) => row.forceId === preferred) &&
    validateDiplomacyMissionTarget(props.worldState, props.action, preferred) == null
  ) {
    return preferred;
  }
  const firstValid = targetForceRows.value.find(
    (row) => validateDiplomacyMissionTarget(props.worldState, props.action, row.forceId) == null,
  );
  return firstValid?.forceId ?? targetForceRows.value[0]?.forceId ?? null;
}

function resolveInitialCharacterId(): number | null {
  const idle = officerRows.value.find((row) => {
    const character = props.worldState.characters?.find((c) => c.id === row.id);
    return character?.forceStatus === "Idle";
  });
  return idle?.id ?? officerRows.value[0]?.id ?? null;
}

watch(
  () => props.visible,
  (v) => {
    if (!v) return;
    personListPreset.value = "status";
    cededStrongholdIds.value = [];
    reparationsMoney.value = 0;
    demandOuterVassalage.value = false;
    characterId.value = resolveInitialCharacterId();
    targetForceId.value = resolveInitialTargetId();
    emit("update:characterId", characterId.value);
    emit("update:targetForceId", targetForceId.value);
    emitPeaceTerms();
    emit("requestPreview");
  },
);

watch(
  () => props.initialTargetForceId,
  (id) => {
    if (!props.visible || id == null) return;
    targetForceId.value = id;
    cededStrongholdIds.value = [];
    reparationsMoney.value = 0;
    demandOuterVassalage.value = false;
    emit("update:targetForceId", id);
    emitPeaceTerms();
    emit("requestPreview");
  },
);

watch(
  () => props.action,
  () => {
    if (!props.visible) return;
    emit("requestPreview");
  },
);

function onCharacterSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  characterId.value = Number(row.id);
  emit("update:characterId", characterId.value);
  emit("requestPreview");
}

function onTargetChange(row: Record<string, unknown> | null) {
  const id = row ? Number(row.forceId ?? row.id) : null;
  targetForceId.value = id != null && Number.isFinite(id) && id > 0 ? id : null;
  emit("update:targetForceId", targetForceId.value);
  cededStrongholdIds.value = [];
  reparationsMoney.value = 0;
  demandOuterVassalage.value = false;
  emitPeaceTerms();
  emit("requestPreview");
}

function emitPeaceTerms() {
  emit("update:peaceTerms", {
    cededStrongholdIds: [...cededStrongholdIds.value],
    reparationsMoney: Math.max(0, reparationsMoney.value),
    demandOuterVassalage: demandOuterVassalage.value,
  });
}

function onPeaceTermsChanged() {
  emitPeaceTerms();
  if (!peaceTermValidationError.value) emit("requestPreview");
}

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!canConfirm.value) return;
  emit("confirm");
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="actionLabel"
    width="min(820px, 96vw)"
    append-to-body
    destroy-on-close
    :close-on-click-modal="false"
    class="strategy-dialog-centered-footer diplomacy-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <StrategyCharacterSpeechBubble
      :character-name="selectedCharacterName"
      :message="speechMessage"
      :tone="speechTone"
    />

    <div class="field">
      <label>将领</label>
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
        :current-id="characterId"
        :links-enabled="false"
        scroll-wrap
        :max-height="280"
        empty-text="居城暂无现任将领"
        @current-change="onCharacterSelect"
      />
    </div>

    <div v-if="action === 'Peace'" class="peace-terms">
      <div class="peace-score-row">
        <span>战争分数</span>
        <strong :class="{ favorable: (activeWar?.playerWarScore ?? 0) > 0 }">
          {{ activeWar?.playerWarScore ?? 0 }}
        </strong>
        <span>条款成本</span>
        <strong>{{ peaceRequiredWarScore ?? 0 }}</strong>
        <el-tag v-if="peaceCanForceAcceptance" type="success" size="small">可强制和谈</el-tag>
      </div>

      <div class="peace-term-field">
        <label>割让据点</label>
        <el-checkbox-group v-model="cededStrongholdIds" @change="onPeaceTermsChanged">
          <el-checkbox
            v-for="stronghold in targetStrongholds"
            :key="stronghold.id"
            :value="stronghold.id"
          >
            {{ stronghold.name }}（规模 {{ stronghold.scale ?? 10 }}）
          </el-checkbox>
        </el-checkbox-group>
        <span class="hint">不选择即维持当前疆界；不能割走全部据点。</span>
      </div>

      <div class="peace-term-grid">
        <label>赔款（文）</label>
        <el-input-number
          v-model="reparationsMoney"
          :min="0"
          :max="Math.max(0, targetForce?.money ?? 0)"
          :step="100"
          controls-position="right"
          @change="onPeaceTermsChanged"
        />
        <label>外藩臣服</label>
        <el-switch
          v-model="demandOuterVassalage"
          :disabled="!canDemandVassalage"
          @change="onPeaceTermsChanged"
        />
      </div>
    </div>

    <div class="field">
      <div class="field-head">
        <label>目标势力</label>
        <el-button size="small" @click="emit('pickForceFromMap')">从地图选择</el-button>
      </div>
      <StrategyIntelSystemTable
        :rows="targetForceRows"
        :columns="DIPLOMACY_BRIEF_COLUMNS"
        :current-id="targetForceId"
        :links-enabled="false"
        empty-text="暂无可选武家势力"
        :max-height="220"
        @current-change="onTargetChange"
      />
    </div>

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button type="primary" :disabled="!canConfirm" @click="submit">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.field {
  margin-bottom: 14px;
}

.field > label,
.field-head label {
  display: block;
  margin-bottom: 6px;
  font-size: 0.82rem;
  color: #334155;
  font-weight: 600;
}

.field-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 6px;
}

.field-head label {
  margin-bottom: 0;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.layer-tabs :deep(.el-tabs__item) {
  font-size: 0.85rem;
}

.peace-terms {
  padding: 12px;
  margin-bottom: 14px;
  border: 1px solid #d9c9a2;
  border-radius: 6px;
  background: #fffcf4;
}

.peace-score-row,
.peace-term-grid {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.peace-score-row {
  margin-bottom: 12px;
}

.peace-score-row strong.favorable {
  color: #16794b;
}

.peace-term-field {
  margin-bottom: 12px;
}

.peace-term-field > label,
.peace-term-grid > label {
  font-size: 0.82rem;
  font-weight: 600;
  color: #334155;
}

.peace-term-field :deep(.el-checkbox-group) {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 14px;
  margin-top: 6px;
}

.hint {
  display: block;
  margin-top: 4px;
  color: #7c6f64;
  font-size: 0.76rem;
}
</style>
