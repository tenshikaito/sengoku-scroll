<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import { conscriptDailyRate, recruitAssignablePersonRows } from "@/utils/strategyRecruitDialogs";

const props = withDefaults(
  defineProps<{
    visible: boolean;
    mode?: "assign" | "personal";
    stronghold: StrategyStrongholdState | null;
    worldState: StrategyWorldState | null;
    actingCharacterId?: number | null;
  }>(),
  {
    mode: "assign",
    actingCharacterId: null,
  },
);

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: { characterId: number }];
}>();

const selectedCharacterId = ref<number | null>(null);
const personListPreset = ref<PersonListPreset>("status");

const isPersonal = computed(() => props.mode === "personal");

const actingCharacter = computed(() => {
  if (!isPersonal.value || props.actingCharacterId == null) return null;
  return props.worldState?.characters?.find((c) => c.id === props.actingCharacterId) ?? null;
});

const effectiveCharacterId = computed(() =>
  isPersonal.value ? props.actingCharacterId ?? null : selectedCharacterId.value,
);

const effectiveCharacter = computed(() => {
  if (isPersonal.value) return actingCharacter.value;
  return props.worldState?.characters?.find((c) => c.id === selectedCharacterId.value) ?? null;
});

const selectedDailyRate = computed(() =>
  conscriptDailyRate(effectiveCharacter.value?.charm),
);

const dialogTitle = computed(() => {
  const place = props.stronghold?.name ?? "据点";
  if (isPersonal.value) {
    const name = actingCharacter.value?.name ?? "角色";
    return `个人征兵 — ${name}（${place}）`;
  }
  return props.stronghold ? `征兵 — ${place}` : "征兵";
});

const personRows = computed(() => {
  const sh = props.stronghold;
  const ws = props.worldState;
  if (isPersonal.value || !sh || !ws) return [];
  return recruitAssignablePersonRows(ws, sh.id);
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

watch(
  () => [props.visible, props.mode, personRows.value] as const,
  ([visible, mode, rows]) => {
    if (!visible) return;
    if (mode === "assign") {
      selectedCharacterId.value = rows[0]?.id ?? null;
      personListPreset.value = "status";
    }
  },
);

function close() {
  emit("update:visible", false);
}

function onPersonSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  selectedCharacterId.value = Number(row.id);
}

function submit() {
  if (effectiveCharacterId.value == null) return;
  emit("confirm", { characterId: effectiveCharacterId.value });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="min(820px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer recruit-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="effectiveCharacterId != null" class="rate-line">
      每日征募约 <strong>{{ selectedDailyRate }}</strong> 人（魅力 {{ effectiveCharacter?.charm ?? 0 }}）
    </p>

    <template v-if="!isPersonal">
      <h4 class="panel-title">城内将领</h4>
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
        :max-height="360"
        empty-text="城内暂无待命将领"
        @current-change="onPersonSelect"
      />
    </template>

    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button
        type="primary"
        :disabled="!stronghold || effectiveCharacterId == null"
        @click="submit"
      >
        确认
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.rate-line {
  margin: 0 0 12px;
  font-size: 0.85rem;
  color: #475569;
}

.rate-line strong {
  color: #0f172a;
  font-variant-numeric: tabular-nums;
}

.panel-title {
  margin: 0 0 6px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #0f172a;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}
</style>
