<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import { ElMessageBox } from "element-plus";
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

export type AppointOfficialKind = "Lord" | "Mayor";

export type AppointConfirmPayload = {
  strongholdId: number;
  characterId: number;
  appointType: AppointOfficialKind;
  closeAfter: boolean;
};

const props = defineProps<{
  visible: boolean;
  initialStronghold: StrategyStrongholdState | null;
  worldState: StrategyWorldState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: AppointConfirmPayload];
}>();

const appointKind = ref<"lord" | "mayor">("lord");
const selectedStrongholdId = ref<number | null>(null);
const selectedCharacterId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("status");

const selectedStronghold = computed(() =>
  props.worldState?.strongholds.find((s) => s.id === selectedStrongholdId.value) ?? null,
);

const canCommandSelectedStronghold = computed(() => {
  if (!props.worldState || selectedStrongholdId.value == null) return false;
  const sh =
    selectedStronghold.value
    ?? props.worldState.strongholds.find((s) => s.id === selectedStrongholdId.value);
  if (!sh) return false;
  return canLordCommandStronghold(props.worldState, sh);
});

const isInitialStrongholdResidence = computed(() => {
  const ws = props.worldState;
  const sh = props.initialStronghold;
  if (!ws || !sh) return false;
  const res = resolveLordResidenceStronghold(ws);
  return res?.id === sh.id;
});

const playerForceId = computed(() => props.worldState?.playerForceId ?? 0);

const residence = computed(() =>
  props.worldState ? resolveLordResidenceStronghold(props.worldState) : null,
);

const forceLordCharacterId = computed(() => {
  const ws = props.worldState;
  const resId = residence.value?.id;
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

const residenceCharacterIds = computed((): Set<number> => {
  const ws = props.worldState;
  const resId = residence.value?.id;
  if (!ws || resId == null) return new Set();

  const ids = (ws.characters ?? [])
    .filter(
      (c) =>
        c.forceId === playerForceId.value
        && !c.isDead
        && c.locationType === "Stronghold"
        && c.strongholdId === resId,
    )
    .map((c) => c.id);

  const lordId = forceLordCharacterId.value;
  if (lordId != null && !ids.includes(lordId)) {
    ids.unshift(lordId);
  }

  return new Set(ids);
});

const strongholdRows = computed(() => {
  if (!props.worldState) return [];
  return strongholdIntelRows(props.worldState, { realmFilter: "homeOnly" }).map((row) => {
    const sh = props.worldState!.strongholds.find((s) => s.id === row.id);
    const appointedLord = sh?.isDirectRule ? "—" : (row.lordName?.trim() || "—");
    return { ...row, appointedLord };
  });
});

const lordCharacterIds = computed((): Set<number> => {
  if (!props.worldState) return new Set();
  const ids = props.worldState.strongholds
    .filter((sh) => sh.forceId === playerForceId.value && !sh.isDirectRule && sh.lordId > 0)
    .map((sh) => sh.lordId);
  return new Set(ids);
});

const personRows = computed(() => {
  if (!props.worldState) return [];
  const allowed = residenceCharacterIds.value;
  const lords = lordCharacterIds.value;
  return personIntelRows(props.worldState, { realmFilter: "homeOnly" }).filter((row) => {
    if (!allowed.has(row.id)) return false;
    if (appointKind.value === "mayor") {
      if (lords.has(row.id)) return false;
      if (row.id === forceLordCharacterId.value) return false;
    }
    return true;
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

const isResidenceTarget = computed(
  () => selectedStronghold.value?.id != null && selectedStronghold.value.id === residence.value?.id,
);

const lordModeBlocked = computed(
  () => appointKind.value === "lord" && isResidenceTarget.value,
);

const mayorSelectionBlocked = computed(
  () =>
    appointKind.value === "mayor"
    && selectedCharacterId.value != null
    && selectedCharacterId.value === forceLordCharacterId.value,
);

const submitDisabled = computed(
  () =>
    !canCommandSelectedStronghold.value
    || selectedStrongholdId.value == null
    || selectedCharacterId.value == null
    || lordModeBlocked.value
    || mayorSelectionBlocked.value,
);

const hintText = computed(() => {
  if (selectedStrongholdId.value == null) {
    return "请在左侧选择目标据点。";
  }
  if (!canCommandSelectedStronghold.value) return LORD_COMMAND_STRONGHOLD_TIP;
  if (appointKind.value === "lord") {
    return "默认选中地图据点，可在左侧表格改选其它据点；选当主表示设为直辖。居城须保持直辖。";
  }
  return "默认选中地图据点，可在左侧表格改选其它据点；任命代官后将领将前往赴任（当主不可任代官）。";
});

function resolveInitialStrongholdId(preferredId: number | null | undefined): number | null {
  if (preferredId != null) return preferredId;
  return strongholdRows.value[0]?.id ?? null;
}

function resolveInitialCharacterId(): number | null {
  const rows = personRows.value;
  if (!rows.length) return forceLordCharacterId.value;
  if (appointKind.value === "mayor") {
    return rows[0]?.id ?? null;
  }
  if (forceLordCharacterId.value != null && rows.some((r) => r.id === forceLordCharacterId.value)) {
    return forceLordCharacterId.value;
  }
  return rows[0]?.id ?? null;
}

function resolveInitialAppointKind(): "lord" | "mayor" {
  return isInitialStrongholdResidence.value ? "mayor" : "lord";
}

function syncTableSelection() {
  if (!props.visible) return;
  selectedStrongholdId.value = resolveInitialStrongholdId(props.initialStronghold?.id);
  selectedCharacterId.value = resolveInitialCharacterId();
}

watch(
  () => [props.visible, props.initialStronghold?.id] as const,
  async ([visible]) => {
    if (!visible) return;
    appointKind.value = resolveInitialAppointKind();
    personListPreset.value = "status";
    syncTableSelection();
    await nextTick();
    syncTableSelection();
  },
);

watch(appointKind, async () => {
  if (!props.visible) return;
  selectedCharacterId.value = resolveInitialCharacterId();
  await nextTick();
});

function close() {
  emit("update:visible", false);
}

function onStrongholdSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedStrongholdId.value = Number(row.id);
}

function onPersonSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedCharacterId.value = Number(row.id);
}

function strongholdRowClass(row: Record<string, unknown>): string {
  if (appointKind.value === "lord" && Number(row.id) === residence.value?.id) {
    return "row-residence-blocked";
  }
  return "";
}

async function submit(closeAfter: boolean) {
  if (submitDisabled.value) return;
  const shId = selectedStrongholdId.value;
  const charId = selectedCharacterId.value;
  if (shId == null || charId == null) return;

  const appointType: AppointOfficialKind = appointKind.value === "mayor" ? "Mayor" : "Lord";

  if (appointType === "Lord" && charId === forceLordCharacterId.value) {
    try {
      await ElMessageBox.confirm(
        `是否将「${selectedStronghold.value?.name ?? "该据点"}」设为当主直辖？`,
        "设为直辖",
        { confirmButtonText: "确认", cancelButtonText: "取消", type: "warning" },
      );
    } catch {
      return;
    }
  }

  emit("confirm", { strongholdId: shId, characterId: charId, appointType, closeAfter });
  if (closeAfter) close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="任命"
    width="min(980px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer appoint-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <div class="kind-row">
      <el-radio-group v-model="appointKind">
        <el-radio value="lord">领主</el-radio>
        <el-radio value="mayor">代官</el-radio>
      </el-radio-group>
    </div>

    <p class="hint" :class="{ 'hint--warn': !canCommandSelectedStronghold || lordModeBlocked }">
      {{ hintText }}
    </p>

    <div class="panel-headers">
      <h4 class="panel-title">据点</h4>
      <h4 class="panel-title">居城将领</h4>
    </div>

    <div class="panels">
      <section class="panel panel--stronghold">
        <StrategyIntelSystemTable
          :rows="strongholdRows as unknown as Array<Record<string, unknown>>"
          :columns="APPOINT_STRONGHOLD_COLUMNS"
          :current-id="selectedStrongholdId"
          :max-height="360"
          fill-width
          :row-class-name="strongholdRowClass"
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
          empty-text="居城暂无可用将领"
          @current-change="onPersonSelect"
        />
      </section>
    </div>

    <p v-if="lordModeBlocked" class="warn-line">当主居城须保持直辖，不可任命外臣领主，请改选其它据点。</p>

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button :disabled="submitDisabled" @click="submit(false)">应用</el-button>
      <el-button type="primary" :disabled="submitDisabled" @click="submit(true)">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.kind-row {
  margin-bottom: 8px;
}

.hint {
  font-size: 0.85rem;
  color: #334155;
  margin: 0 0 10px;
  line-height: 1.45;
}

.hint--warn,
.warn-line {
  color: #b45309;
}

.warn-line {
  margin: 10px 0 0;
  font-size: 0.85rem;
}

.panel-headers {
  display: grid;
  grid-template-columns: minmax(200px, 24%) minmax(0, 1fr);
  gap: 12px;
  align-items: end;
  margin-bottom: 6px;
}

.panels {
  display: grid;
  grid-template-columns: minmax(200px, 24%) minmax(0, 1fr);
  gap: 12px;
  align-items: start;
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

.appoint-dialog-root :deep(.el-table__body tr.row-residence-blocked > td.el-table__cell) {
  opacity: 0.5;
}
</style>
