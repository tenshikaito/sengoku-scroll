<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategyTypes";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import { personIntelRows } from "@/utils/strategyIntelSystemData";
import { LORD_COMMAND_STRONGHOLD_TIP } from "@/utils/strategyLordCommands";
import { canLordCommandStronghold } from "@/utils/strategyPlayerCharacter";

export type RecallConfirmPayload = {
  strongholdId: number;
  characterId: number;
  closeAfter: boolean;
};

const props = defineProps<{
  visible: boolean;
  initialStronghold: { id: number; name?: string } | null;
  worldState: StrategyWorldState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: RecallConfirmPayload];
}>();

const selectedCharacterId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("order");

const playerForceId = computed(() => props.worldState?.playerForceId ?? 0);

const canCommandStronghold = computed(() => {
  if (!props.worldState || props.initialStronghold == null) return false;
  const sh = props.worldState.strongholds.find((s) => s.id === props.initialStronghold!.id);
  if (!sh) return false;
  return canLordCommandStronghold(props.worldState, sh);
});

function hasRecallableTask(character: NonNullable<StrategyWorldState["characters"]>[number]): boolean {
  if (character.isDead) return false;
  if ((character.forceStatus ?? "Idle") === "Task") return true;
  if ((character.taskRemainingDays ?? 0) > 0) return true;
  return false;
}

const personRows = computed(() => {
  if (!props.worldState) return [];
  return personIntelRows(props.worldState, { realmFilter: "homeOnly" }).filter((row) => {
    const character = props.worldState!.characters?.find((c) => c.id === row.id);
    if (!character) return false;
    if (character.forceId !== playerForceId.value) return false;
    return hasRecallableTask(character);
  });
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

const submitDisabled = computed(
  () =>
    !canCommandStronghold.value
    || props.initialStronghold == null
    || selectedCharacterId.value == null,
);

const hintText = computed(() => {
  if (props.initialStronghold == null) {
    return "请先在地图上选择本家据点。";
  }
  if (!canCommandStronghold.value) return LORD_COMMAND_STRONGHOLD_TIP;
  return "中断外派任务并令其尽快回城。募兵任务未用资金退回，已执行效果减半；远程将领经信使传达召回令，同格即时生效。";
});

function resolveInitialCharacterId(): number | null {
  return personRows.value[0]?.id ?? null;
}

function syncSelection() {
  if (!props.visible) return;
  selectedCharacterId.value = resolveInitialCharacterId();
}

watch(
  () => [props.visible, props.initialStronghold?.id] as const,
  async ([visible]) => {
    if (!visible) return;
    personListPreset.value = "order";
    syncSelection();
    await nextTick();
    syncSelection();
  },
);

function close() {
  emit("update:visible", false);
}

function onPersonSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedCharacterId.value = Number(row.id);
}

function submit(closeAfter: boolean) {
  if (submitDisabled.value || props.initialStronghold == null) return;
  const charId = selectedCharacterId.value;
  if (charId == null) return;

  emit("confirm", {
    strongholdId: props.initialStronghold.id,
    characterId: charId,
    closeAfter,
  });
  if (closeAfter) close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="召回"
    width="min(760px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer recall-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <p class="hint" :class="{ 'hint--warn': !canCommandStronghold }">
      {{ hintText }}
    </p>

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
      empty-text="暂无外派中的将领"
      @current-change="onPersonSelect"
    />

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button :disabled="submitDisabled" @click="submit(false)">应用</el-button>
      <el-button type="primary" :disabled="submitDisabled" @click="submit(true)">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  font-size: 0.85rem;
  color: #334155;
  margin: 0 0 10px;
  line-height: 1.45;
}

.hint--warn {
  color: #b45309;
}

.layer-tabs :deep(.el-tabs__header) {
  margin: 0 0 6px;
}

.layer-tabs :deep(.el-tabs__item) {
  font-size: 0.82rem;
  padding: 0 10px;
  height: 28px;
}

.layer-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
}
</style>
