<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategyTypes";
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
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  "update:targetForceId": [forceId: number | null];
  "update:characterId": [characterId: number | null];
  requestPreview: [];
  pickForceFromMap: [];
  confirm: [];
}>();

const characterId = ref<number | null>(null);
const targetForceId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("status");

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
  if (targetValidationError.value || characterSelectionError.value) return "warning";
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
    characterId.value = resolveInitialCharacterId();
    targetForceId.value = resolveInitialTargetId();
    emit("update:characterId", characterId.value);
    emit("update:targetForceId", targetForceId.value);
    emit("requestPreview");
  },
);

watch(
  () => props.initialTargetForceId,
  (id) => {
    if (!props.visible || id == null) return;
    targetForceId.value = id;
    emit("update:targetForceId", id);
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
  emit("requestPreview");
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
</style>
