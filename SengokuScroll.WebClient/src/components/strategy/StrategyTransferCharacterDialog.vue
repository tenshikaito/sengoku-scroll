<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  APPOINT_STRONGHOLD_COLUMNS,
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import { personIntelRows, strongholdIntelRows } from "@/utils/strategyIntelSystemData";
import {
  LORD_COMMAND_STRONGHOLD_TIP,
  resolveLordResidenceStronghold,
} from "@/utils/strategyLordCommands";
import { canLordCommandStronghold } from "@/utils/strategyPlayerCharacter";

export type TransferMode = "dispatch" | "summon";

export type TransferConfirmPayload = {
  mode: TransferMode;
  strongholdId: number;
  destinationStrongholdId?: number;
  characterId: number;
  closeAfter: boolean;
};

const props = defineProps<{
  visible: boolean;
  initialStronghold: StrategyStrongholdState | null;
  worldState: StrategyWorldState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: TransferConfirmPayload];
}>();

const transferMode = ref<TransferMode>("dispatch");
const showAllCharacters = ref(false);
const selectedStrongholdId = ref<number | null>(null);
const selectedCharacterId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("status");

const playerForceId = computed(() => props.worldState?.playerForceId ?? 0);
const originStrongholdId = computed(() => props.initialStronghold?.id ?? null);

const canCommandOriginStronghold = computed(() => {
  if (!props.worldState || originStrongholdId.value == null) return false;
  const sh = props.initialStronghold
    ?? props.worldState.strongholds.find((s) => s.id === originStrongholdId.value);
  if (!sh) return false;
  return canLordCommandStronghold(props.worldState, sh);
});

const hideStrongholdPanel = computed(
  () => transferMode.value === "summon" && showAllCharacters.value,
);

const strongholdPanelTitle = computed(() =>
  transferMode.value === "dispatch" ? "目标据点" : "源据点",
);

const forceLordCharacterId = computed(() => {
  const ws = props.worldState;
  if (!ws) return null;
  const res = resolveLordResidenceStronghold(ws);
  const resId = res?.id;
  if (!ws || resId == null) return null;
  const lordName = ws.lord.name?.trim();
  if (lordName) {
    const byName = (ws.characters ?? []).find(
      (c) => c.forceId === playerForceId.value && c.name === lordName,
    );
    if (byName) return byName.id;
  }
  return (ws.characters ?? []).find(
    (c) => c.forceId === playerForceId.value && c.strongholdId === resId,
  )?.id ?? null;
});

const lordCharacterIds = computed((): Set<number> => {
  if (!props.worldState) return new Set();
  return new Set(
    props.worldState.strongholds
      .filter((sh) => sh.forceId === playerForceId.value && !sh.isDirectRule && sh.lordId > 0)
      .map((sh) => sh.lordId),
  );
});

const mayorCharacterIds = computed((): Set<number> => {
  if (!props.worldState) return new Set();
  return new Set(
    props.worldState.strongholds
      .filter((sh) => sh.forceId === playerForceId.value && (sh.mayorId ?? 0) > 0)
      .map((sh) => sh.mayorId!),
  );
});

const strongholdRows = computed(() => {
  if (!props.worldState) return [];
  const rows = strongholdIntelRows(props.worldState, { realmFilter: "homeOnly" }).map((row) => {
    const sh = props.worldState!.strongholds.find((s) => s.id === row.id);
    const appointedLord = sh?.isDirectRule ? "—" : (row.lordName?.trim() || "—");
    return { ...row, appointedLord };
  });

  if (transferMode.value === "dispatch" && originStrongholdId.value != null) {
    return rows.filter((row) => row.id !== originStrongholdId.value);
  }

  return rows;
});

function isTransferCandidate(rowId: number, character: NonNullable<StrategyWorldState["characters"]>[number]): boolean {
  if (character.forceId !== playerForceId.value || character.isDead) return false;
  if (character.locationType !== "Stronghold") return false;
  if (rowId === forceLordCharacterId.value || lordCharacterIds.value.has(rowId) || mayorCharacterIds.value.has(rowId)) {
    return false;
  }
  if ((character.forceStatus ?? "Idle") !== "Idle") return false;
  if ((character.taskRemainingDays ?? 0) > 0) return false;
  return true;
}

const personRows = computed(() => {
  if (!props.worldState || originStrongholdId.value == null) return [];

  const originId = originStrongholdId.value;
  const lords = lordCharacterIds.value;
  const mayors = mayorCharacterIds.value;
  const forceLordId = forceLordCharacterId.value;

  return personIntelRows(props.worldState, { realmFilter: "homeOnly" }).filter((row) => {
    const character = props.worldState!.characters?.find((c) => c.id === row.id);
    if (!character) return false;
    if (row.id === forceLordId || lords.has(row.id) || mayors.has(row.id)) return false;
    if (!isTransferCandidate(row.id, character)) return false;

    if (transferMode.value === "dispatch") {
      return (character.strongholdId ?? 0) === originId;
    }

    const targetId = originId;
    if ((character.strongholdId ?? 0) === targetId) return false;

    if (showAllCharacters.value) return true;

    if (selectedStrongholdId.value == null) return false;
    return (character.strongholdId ?? 0) === selectedStrongholdId.value;
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

const submitDisabled = computed(() => {
  if (!canCommandOriginStronghold.value || originStrongholdId.value == null || selectedCharacterId.value == null) {
    return true;
  }

  if (transferMode.value === "dispatch") {
    return selectedStrongholdId.value == null;
  }

  if (!showAllCharacters.value && selectedStrongholdId.value == null) {
    return true;
  }

  return false;
});

const hintText = computed(() => {
  if (originStrongholdId.value == null) {
    return "请先在地图上选择本家据点。";
  }
  if (!canCommandOriginStronghold.value) return LORD_COMMAND_STRONGHOLD_TIP;

  if (transferMode.value === "dispatch") {
    return `派遣：从 ${props.initialStronghold?.name ?? "当前据点"} 选派待命将领（非领主、非代官、无任务）前往左侧所选目标据点。`;
  }

  if (showAllCharacters.value) {
    return `召集：将其它据点的待命将领（非领主、非代官、无任务）召集至 ${props.initialStronghold?.name ?? "当前据点"}。`;
  }

  return `召集：在左侧选择源据点，将待命将领召集至 ${props.initialStronghold?.name ?? "当前据点"}。`;
});

function resolveInitialStrongholdId(): number | null {
  return strongholdRows.value[0]?.id ?? null;
}

function resolveInitialCharacterId(): number | null {
  return personRows.value[0]?.id ?? null;
}

function syncTableSelection() {
  if (!props.visible) return;
  if (!hideStrongholdPanel.value) {
    selectedStrongholdId.value = resolveInitialStrongholdId();
  }
  selectedCharacterId.value = resolveInitialCharacterId();
}

watch(
  () => [props.visible, props.initialStronghold?.id] as const,
  async ([visible]) => {
    if (!visible) return;
    transferMode.value = "dispatch";
    showAllCharacters.value = false;
    personListPreset.value = "status";
    syncTableSelection();
    await nextTick();
    syncTableSelection();
  },
);

watch(transferMode, async () => {
  if (!props.visible) return;
  showAllCharacters.value = false;
  syncTableSelection();
  await nextTick();
  syncTableSelection();
});

watch(showAllCharacters, async () => {
  if (!props.visible) return;
  syncTableSelection();
  await nextTick();
  syncTableSelection();
});

watch(selectedStrongholdId, async () => {
  if (!props.visible || hideStrongholdPanel.value) return;
  selectedCharacterId.value = resolveInitialCharacterId();
  await nextTick();
});

function close() {
  emit("update:visible", false);
}

function onStrongholdSelect(row: Record<string, unknown> | null) {
  if (!row || hideStrongholdPanel.value) return;
  selectedStrongholdId.value = Number(row.id);
}

function onPersonSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedCharacterId.value = Number(row.id);
}

function submit(closeAfter: boolean) {
  if (submitDisabled.value || originStrongholdId.value == null) return;
  const charId = selectedCharacterId.value;
  if (charId == null) return;

  if (transferMode.value === "dispatch") {
    const destinationId = selectedStrongholdId.value;
    if (destinationId == null) return;

    emit("confirm", {
      mode: "dispatch",
      strongholdId: originStrongholdId.value,
      destinationStrongholdId: destinationId,
      characterId: charId,
      closeAfter,
    });
  } else {
    emit("confirm", {
      mode: "summon",
      strongholdId: originStrongholdId.value,
      characterId: charId,
      closeAfter,
    });
  }

  if (closeAfter) close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="调动"
    width="min(980px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer transfer-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <div class="mode-row">
      <el-radio-group v-model="transferMode" size="small">
        <el-radio-button value="dispatch">派遣</el-radio-button>
        <el-radio-button value="summon">召集</el-radio-button>
      </el-radio-group>
      <el-checkbox
        v-model="showAllCharacters"
        :disabled="transferMode !== 'summon'"
        class="mode-checkbox"
      >
        显示所有角色
      </el-checkbox>
    </div>

    <p class="hint" :class="{ 'hint--warn': !canCommandOriginStronghold }">
      {{ hintText }}
    </p>

    <div class="panel-headers" :class="{ 'panel-headers--single': hideStrongholdPanel }">
      <h4 v-if="!hideStrongholdPanel" class="panel-title">{{ strongholdPanelTitle }}</h4>
      <h4 class="panel-title">可调动将领</h4>
    </div>

    <div class="panels" :class="{ 'panels--single': hideStrongholdPanel }">
      <section v-if="!hideStrongholdPanel" class="panel panel--stronghold">
        <StrategyIntelSystemTable
          :rows="strongholdRows as unknown as Array<Record<string, unknown>>"
          :columns="APPOINT_STRONGHOLD_COLUMNS"
          :current-id="selectedStrongholdId"
          :max-height="360"
          fill-width
          empty-text="暂无本家据点"
          @current-change="onStrongholdSelect"
        />
      </section>

      <section class="panel panel--person">
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
          empty-text="暂无可调动将领"
          @current-change="onPersonSelect"
        />
      </section>
    </div>

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button :disabled="submitDisabled" @click="submit(false)">应用</el-button>
      <el-button type="primary" :disabled="submitDisabled" @click="submit(true)">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.mode-row {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 10px;
  flex-wrap: wrap;
}

.mode-checkbox {
  margin: 0;
}

.hint {
  font-size: 0.85rem;
  color: #334155;
  margin: 0 0 10px;
  line-height: 1.45;
}

.hint--warn {
  color: #b45309;
}

.panel-headers {
  display: grid;
  grid-template-columns: minmax(200px, 24%) minmax(0, 1fr);
  gap: 12px;
  align-items: end;
  margin-bottom: 6px;
}

.panel-headers--single {
  grid-template-columns: minmax(0, 1fr);
}

.panels {
  display: grid;
  grid-template-columns: minmax(200px, 24%) minmax(0, 1fr);
  gap: 12px;
  align-items: start;
}

.panels--single {
  grid-template-columns: minmax(0, 1fr);
}

.panel-title {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: #0f172a;
  line-height: 1.25;
}

.panel--stronghold,
.panel--person {
  min-width: 0;
}

.panel--person {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.layer-tabs :deep(.el-tabs__header) {
  margin: 0;
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
