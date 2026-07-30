<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type {
  StrategyDeployCompositionEntry,
  StrategyStrongholdState,
  StrategyWorldState,
} from "@/api/strategyTypes";
import StrategyIntelSystemTable from "@/components/strategy/StrategyIntelSystemTable.vue";
import {
  PERSON_LIST_COLUMN_PRESETS,
  PERSON_PERSONAL_DEV_ONLY_PROPS,
  type PersonListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import {
  buildTroopAllocationMarks,
  expeditionCommanderRows,
} from "@/utils/strategyRecruitDialogs";

const props = defineProps<{
  visible: boolean;
  stronghold: StrategyStrongholdState | null;
  worldState: StrategyWorldState;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: {
    unitName?: string;
    commanderId: number;
    composition: StrategyDeployCompositionEntry[];
    deployToMap: boolean;
  }];
}>();

const deployToMap = ref(false);
const unitName = ref("");
const commanderId = ref<number | null>(null);
const troopCounts = ref<Record<number, number>>({});
const personListPreset = ref<PersonListPreset>("status");

const troopOptions = computed(() => {
  const pools = props.stronghold?.garrisonTroopPools ?? [];
  if (pools.length > 0) {
    return pools.map((pool) => ({
      typeId: pool.typeId,
      typeName: pool.typeName,
      max: Math.max(0, pool.soldiers),
    }));
  }

  return [
    {
      typeId: 1,
      typeName: "足轻",
      max: Math.max(0, props.stronghold?.garrisonSoldiers ?? 0),
    },
  ];
});

const commanderRows = computed(() => {
  const sh = props.stronghold;
  if (!sh) return [];
  return expeditionCommanderRows(props.worldState, sh.id);
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
  () => commanderRows.value as unknown as Array<Record<string, unknown>>,
);

const totalSoldiers = computed(() =>
  troopOptions.value.reduce(
    (sum, opt) => sum + Math.max(0, troopCounts.value[opt.typeId] ?? 0),
    0,
  ),
);

const poolRemaining = computed(() => {
  const remaining: Record<number, number> = {};
  for (const opt of troopOptions.value) {
    remaining[opt.typeId] = Math.max(0, opt.max - Math.max(0, troopCounts.value[opt.typeId] ?? 0));
  }
  return remaining;
});

const laborHint = computed(() => {
  const sh = props.stronghold;
  if (!sh || sh.laborCapacity == null) return null;
  const available = sh.laborAvailable ?? sh.laborCapacity;
  const ratio = sh.laborRatioPercent ?? 100;
  const away = sh.militiaAway ?? 0;
  const pattern = cropPatternLabel(sh.effectiveCropPattern);
  const progress = [
    `早稻 ${sh.earlyCropProgressPercent ?? 0}%`,
    sh.effectiveCropPattern !== "Single" ? `晚稻 ${sh.lateCropProgressPercent ?? 0}%` : null,
    sh.effectiveCropPattern === "Triple" ? `第三季 ${sh.thirdCropProgressPercent ?? 0}%` : null,
  ]
    .filter(Boolean)
    .join(" · ");
  return `作型 ${pattern} · 劳力 ${available.toLocaleString()}/${sh.laborCapacity.toLocaleString()}（${ratio}%） · 外派农兵 ${away.toLocaleString()} · ${progress}`;
});

const canConfirm = computed(() => {
  if (props.stronghold == null || commanderId.value == null || totalSoldiers.value <= 0) {
    return false;
  }

  return troopOptions.value.every((opt) => {
    const count = Math.max(0, troopCounts.value[opt.typeId] ?? 0);
    return count <= opt.max;
  });
});

function clampTroopCount(value: number, max: number): number {
  const n = Math.round(Number(value));
  if (!Number.isFinite(n)) return 0;
  return Math.min(max, Math.max(0, n));
}

function resetTroopAllocation() {
  const nextCounts: Record<number, number> = {};
  for (const opt of troopOptions.value) {
    nextCounts[opt.typeId] = 0;
  }
  troopCounts.value = nextCounts;
}

watch(
  () => [props.visible, props.stronghold?.id, props.stronghold?.garrisonTroopPools] as const,
  ([visible]) => {
    if (!visible || !props.stronghold) return;
    unitName.value = `${props.stronghold.name}队`;
    deployToMap.value = false;
    personListPreset.value = "status";
    resetTroopAllocation();
    commanderId.value = commanderRows.value[0]?.id ?? null;
  },
);

function cropPatternLabel(pattern: string | undefined): string {
  switch (pattern) {
    case "Double":
      return "二季作";
    case "Triple":
      return "三季作";
    default:
      return "单季作";
  }
}

function onCommanderSelect(row: Record<string, unknown> | null) {
  if (!row) return;
  commanderId.value = Number(row.id);
}

function onTroopSliderChange(typeId: number, value: number, max: number) {
  troopCounts.value[typeId] = clampTroopCount(value, max);
}

function troopMarks(max: number): Record<number, string> {
  return buildTroopAllocationMarks(max);
}

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!canConfirm.value || commanderId.value == null) return;

  const composition = troopOptions.value.flatMap((opt) => {
    const soldiers = Math.max(0, troopCounts.value[opt.typeId] ?? 0);
    if (soldiers <= 0) return [];
    return [
      {
        typeId: opt.typeId,
        typeName: opt.typeName,
        soldiers,
      } satisfies StrategyDeployCompositionEntry,
    ];
  });

  emit("confirm", {
    unitName: unitName.value.trim() || undefined,
    commanderId: commanderId.value,
    composition,
    deployToMap: deployToMap.value,
  });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="stronghold ? `组建 — ${stronghold.name}` : '组建'"
    width="min(820px, 96vw)"
    append-to-body
    class="strategy-dialog-centered-footer expedition-dialog-root"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="stronghold" class="hint">
      已分配 {{ totalSoldiers.toLocaleString() }} 兵 · 农兵池
      {{ (stronghold.militiaSoldiers ?? stronghold.garrisonSoldiers).toLocaleString() }}
    </p>
    <p v-if="laborHint" class="hint agri">{{ laborHint }}</p>

    <div class="field">
      <label>部队名称</label>
      <el-input v-model="unitName" maxlength="32" />
    </div>

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
        :current-id="commanderId"
        :links-enabled="false"
        scroll-wrap
        :max-height="280"
        empty-text="该城无可用将领"
        @current-change="onCommanderSelect"
      />
    </div>

    <div class="field">
      <label>兵种分配（驻城池）</label>
      <div class="troop-grid">
        <div v-for="opt in troopOptions" :key="opt.typeId" class="troop-row">
          <span class="troop-label">{{ opt.typeName }}</span>
          <el-slider
            :model-value="troopCounts[opt.typeId] ?? 0"
            class="troop-slider"
            :min="0"
            :max="opt.max"
            :step="1"
            :marks="troopMarks(opt.max)"
            :disabled="opt.max <= 0"
            @update:model-value="onTroopSliderChange(opt.typeId, $event as number, opt.max)"
          />
          <span class="troop-value">
            {{ (troopCounts[opt.typeId] ?? 0).toLocaleString() }}
            / {{ opt.max.toLocaleString() }}
          </span>
        </div>
      </div>
      <p class="hint">
        池内剩余：
        {{
          troopOptions
            .map((opt) => `${opt.typeName} ${poolRemaining[opt.typeId]?.toLocaleString() ?? 0}`)
            .join(" · ")
        }}
      </p>
    </div>

    <div class="field">
      <label>组建后</label>
      <el-radio-group v-model="deployToMap">
        <el-radio :value="false">在城中</el-radio>
        <el-radio :value="true">立即出城</el-radio>
      </el-radio-group>
    </div>

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button type="primary" :disabled="!canConfirm" @click="submit">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  font-size: 0.82rem;
  color: #94a3b8;
  margin: 0 0 12px;
}

.hint.agri {
  color: #86efac;
}

.field {
  margin-bottom: 14px;
}

.field > label {
  display: block;
  font-size: 0.82rem;
  color: #334155;
  font-weight: 600;
  margin-bottom: 6px;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.layer-tabs :deep(.el-tabs__item) {
  font-size: 0.85rem;
}

.troop-grid {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.troop-row {
  display: grid;
  grid-template-columns: 56px minmax(0, 1fr) minmax(120px, max-content);
  align-items: center;
  gap: 12px;
}

.troop-label {
  font-size: 0.85rem;
  color: #334155;
  white-space: nowrap;
}

.troop-value {
  font-size: 0.88rem;
  font-weight: 600;
  color: #0f172a;
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.troop-slider {
  padding: 0 8px 32px 0;
}

.troop-slider :deep(.el-slider__marks-text) {
  font-size: 0.78rem;
  font-weight: 600;
  color: #64748b;
  margin-top: 10px;
  white-space: nowrap;
}

.troop-slider :deep(.el-slider__stop) {
  width: 10px;
  height: 10px;
  border: 2px solid #fff;
  box-shadow: 0 0 0 1px #94a3b8;
}

.troop-slider :deep(.el-slider__button) {
  width: 16px;
  height: 16px;
}
</style>
