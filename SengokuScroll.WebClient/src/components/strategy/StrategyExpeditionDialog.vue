<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type {
  StrategyCharacterSummaryState,
  StrategyDeployCompositionEntry,
  StrategyStrongholdState,
} from "@/api/strategyTypes";

const TROOP_OPTIONS = [
  { typeId: 1, typeName: "足轻" },
  { typeId: 2, typeName: "弓兵" },
  { typeId: 3, typeName: "骑兵" },
  { typeId: 4, typeName: "铁炮" },
] as const;

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
const troopCounts = ref<Record<number, number>>({ 1: 0, 2: 0, 3: 0, 4: 0 });

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
  Object.values(troopCounts.value).reduce((sum, n) => sum + Math.max(0, n), 0)
);

const garrisonRemaining = computed(() =>
  props.stronghold ? Math.max(0, props.stronghold.garrisonSoldiers - totalSoldiers.value) : 0
);

const canConfirm = computed(
  () =>
    props.stronghold != null &&
    commanderId.value != null &&
    totalSoldiers.value > 0 &&
    totalSoldiers.value <= (props.stronghold?.garrisonSoldiers ?? 0)
);

watch(
  () => [props.visible, props.stronghold?.id] as const,
  ([visible]) => {
    if (!visible || !props.stronghold) return;
    unitName.value = `${props.stronghold.name}出征队`;
    troopCounts.value = { 1: 0, 2: 0, 3: 0, 4: 0 };
    commanderId.value = availableCommanders.value[0]?.id ?? null;
  }
);

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!canConfirm.value || commanderId.value == null) return;

  const composition = TROOP_OPTIONS.flatMap((opt) => {
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
    width="480px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="stronghold" class="hint">
      城内驻军 {{ stronghold.garrisonSoldiers.toLocaleString() }} 兵 · 已分配
      {{ totalSoldiers.toLocaleString() }} · 剩余 {{ garrisonRemaining.toLocaleString() }}
    </p>

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
      <label>兵种分配</label>
      <div class="troop-grid">
        <div v-for="opt in TROOP_OPTIONS" :key="opt.typeId" class="troop-row">
          <span>{{ opt.typeName }}</span>
          <el-input-number
            v-model="troopCounts[opt.typeId]"
            :min="0"
            :max="stronghold?.garrisonSoldiers ?? 0"
            :step="100"
            controls-position="right"
          />
        </div>
      </div>
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
</style>
