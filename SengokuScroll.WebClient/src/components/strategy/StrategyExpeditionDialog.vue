<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type {
  StrategyCharacterSummaryState,
  StrategyDeployCompositionEntry,
  StrategyStrongholdState,
} from "@/api/strategyTypes";

const props = defineProps<{
  visible: boolean;
  stronghold: StrategyStrongholdState | null;
  characters: StrategyCharacterSummaryState[];
  playerForceId: number;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: {
    unitName?: string;
    commanderId: number;
    composition: StrategyDeployCompositionEntry[];
  }];
}>();

const unitName = ref("");
const commanderId = ref<number | null>(null);
const troopCounts = ref<Record<number, number>>({});

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

const availableCommanders = computed(() =>
  props.characters.filter(
    (c) =>
      c.forceId === props.playerForceId &&
      !c.isDead &&
      c.locationType === "Stronghold" &&
      c.strongholdId === props.stronghold?.id &&
      (c.forceStatus === "Idle" || c.forceStatus === "Task")
  )
);

const totalSoldiers = computed(() =>
  troopOptions.value.reduce(
    (sum, opt) => sum + Math.max(0, troopCounts.value[opt.typeId] ?? 0),
    0
  )
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

watch(
  () => [props.visible, props.stronghold?.id, props.stronghold?.garrisonTroopPools] as const,
  ([visible]) => {
    if (!visible || !props.stronghold) return;
    unitName.value = `${props.stronghold.name}出征队`;
    const nextCounts: Record<number, number> = {};
    for (const opt of troopOptions.value) {
      nextCounts[opt.typeId] = 0;
    }
    troopCounts.value = nextCounts;
    commanderId.value = availableCommanders.value[0]?.id ?? null;
  }
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
  });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="stronghold ? `出征 — ${stronghold.name}` : '出征'"
    width="520px"
    append-to-body
    class="strategy-dialog-centered-footer"
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
      <label>总将</label>
      <el-select v-model="commanderId" placeholder="选择将领" style="width: 100%">
        <el-option
          v-for="c in availableCommanders"
          :key="c.id"
          :label="`${c.name ?? `#${c.id}`}（统 ${c.leadership ?? 0} / 武 ${c.power ?? 0}）`"
          :value="c.id"
        />
      </el-select>
      <p v-if="!availableCommanders.length" class="hint warn">该城无可用将领。</p>
    </div>

    <div class="field">
      <label>兵种分配（驻城池）</label>
      <div class="troop-grid">
        <div v-for="opt in troopOptions" :key="opt.typeId" class="troop-row">
          <span>{{ opt.typeName }}</span>
          <div class="troop-input">
            <el-input-number
              v-model="troopCounts[opt.typeId]"
              :min="0"
              :max="opt.max"
              :step="100"
              controls-position="right"
            />
            <span class="pool-cap">/ {{ opt.max.toLocaleString() }}</span>
          </div>
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

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button type="primary" :disabled="!canConfirm" @click="submit">确认出征</el-button>
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

.hint.warn {
  color: #fbbf24;
}

.field {
  margin-bottom: 14px;
}

.field > label {
  display: block;
  font-size: 0.82rem;
  color: #cbd5e1;
  margin-bottom: 6px;
}

.troop-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.troop-row {
  display: grid;
  grid-template-columns: 64px 1fr;
  gap: 10px;
  align-items: center;
}

.troop-input {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pool-cap {
  font-size: 0.78rem;
  color: #64748b;
  white-space: nowrap;
}
</style>
