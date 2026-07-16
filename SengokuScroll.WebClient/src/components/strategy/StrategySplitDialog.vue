<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategySubUnitState, StrategyUnitState } from "@/api/strategyTypes";

const MIN_SOLDIERS_PER_SIDE = 100;

const props = defineProps<{
  visible: boolean;
  unit: StrategyUnitState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: { subUnitIds: number[]; unitName?: string }];
}>();

const selectedSubUnitIds = ref<number[]>([]);
const unitName = ref("");

const composition = computed(() => props.unit?.composition ?? []);

const selectedSoldiers = computed(() =>
  composition.value
    .filter((sub) => selectedSubUnitIds.value.includes(sub.id))
    .reduce((sum, sub) => sum + sub.soldiers, 0)
);

const remainSoldiers = computed(() =>
  props.unit ? Math.max(0, props.unit.soldiers - selectedSoldiers.value) : 0
);

const canConfirm = computed(
  () =>
    selectedSubUnitIds.value.length > 0 &&
    selectedSoldiers.value >= MIN_SOLDIERS_PER_SIDE &&
    remainSoldiers.value >= MIN_SOLDIERS_PER_SIDE
);

watch(
  () => [props.visible, props.unit?.id] as const,
  ([visible]) => {
    if (!visible) return;
    selectedSubUnitIds.value = [];
    unitName.value = props.unit ? `${props.unit.name}分遣` : "";
  }
);

function toggleSubUnit(sub: StrategySubUnitState) {
  if (selectedSubUnitIds.value.includes(sub.id)) {
    selectedSubUnitIds.value = selectedSubUnitIds.value.filter((id) => id !== sub.id);
    return;
  }
  selectedSubUnitIds.value = [...selectedSubUnitIds.value, sub.id];
}

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!canConfirm.value) return;
  emit("confirm", {
    subUnitIds: [...selectedSubUnitIds.value],
    unitName: unitName.value.trim() || undefined,
  });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="unit ? `分兵 — ${unit.name}` : '分兵'"
    width="460px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="unit" class="hint">选择要拆出的子编制（每边至少 {{ MIN_SOLDIERS_PER_SIDE }} 兵）</p>

    <div v-if="composition.length" class="sub-list">
      <label
        v-for="sub in composition"
        :key="sub.id"
        class="sub-row"
        :class="{ 'sub-row--selected': selectedSubUnitIds.includes(sub.id) }"
      >
        <input
          type="checkbox"
          :checked="selectedSubUnitIds.includes(sub.id)"
          @change="toggleSubUnit(sub)"
        />
        <span class="sub-name">{{ sub.typeName }}</span>
        <span class="sub-count">{{ sub.soldiers.toLocaleString() }} 兵</span>
        <span v-if="sub.commanderName" class="sub-commander">{{ sub.commanderName }}</span>
      </label>
    </div>
    <p v-else class="hint">该部队无可拆子编制。</p>

    <p class="hint">
      拆出 {{ selectedSoldiers.toLocaleString() }} 兵 · 本队剩余 {{ remainSoldiers.toLocaleString() }} 兵
    </p>

    <el-input v-model="unitName" placeholder="新部队名称（可选）" maxlength="32" />

    <template #footer>
      <el-button @click="close">取消</el-button>
      <el-button type="primary" :disabled="!canConfirm" @click="submit">选择落点</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  font-size: 0.82rem;
  color: #94a3b8;
  margin: 0 0 10px;
  line-height: 1.45;
}

.sub-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 12px;
}

.sub-row {
  display: grid;
  grid-template-columns: auto 1fr auto auto;
  gap: 8px;
  align-items: center;
  padding: 8px 10px;
  border: 1px solid #475569;
  border-radius: 6px;
  cursor: pointer;
}

.sub-row--selected {
  border-color: #38bdf8;
  background: rgba(56, 189, 248, 0.08);
}

.sub-name {
  font-weight: 600;
}

.sub-count,
.sub-commander {
  font-size: 0.82rem;
  color: #cbd5e1;
}
</style>
