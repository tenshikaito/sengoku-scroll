<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategySaveSlotSummary } from "@/api/strategyTypes";

const props = defineProps<{
  visible: boolean;
  slots: StrategySaveSlotSummary[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  save: [slot: number];
  load: [slot: number];
}>();

const selectedSlot = ref(1);

const tableRows = computed(() =>
  Array.from({ length: 10 }, (_, index) => {
    const slot = index + 1;
    return props.slots.find((row) => row.slot === slot) ?? { slot, occupied: false };
  })
);

watch(
  () => props.visible,
  (visible) => {
    if (!visible) return;
    selectedSlot.value = 1;
  }
);

function close() {
  emit("update:visible", false);
}

function formatSavedAt(value: string | null | undefined): string {
  if (!value?.trim()) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleString();
}

function rowClassName({ row }: { row: StrategySaveSlotSummary }) {
  return row.slot === selectedSlot.value ? "save-slot-row--selected" : "";
}

function onRowClick(row: StrategySaveSlotSummary) {
  selectedSlot.value = row.slot;
}

function onSave() {
  emit("save", selectedSlot.value);
}

function onLoad() {
  emit("load", selectedSlot.value);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="存档 / 读档"
    width="640px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p class="hint">选择存档位（1–10），然后点击下方「存档」或「读档」。</p>

    <el-table
      v-loading="loading"
      :data="tableRows"
      size="small"
      highlight-current-row
      :row-class-name="rowClassName"
      class="slot-table"
      @row-click="onRowClick"
    >
      <el-table-column label="档位" width="64" align="center">
        <template #default="{ row }">
          <span class="slot-no">{{ row.slot }}</span>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="72" align="center">
        <template #default="{ row }">
          <span :class="row.occupied ? 'status-occupied' : 'status-empty'">
            {{ row.occupied ? "有" : "空" }}
          </span>
        </template>
      </el-table-column>
      <el-table-column label="当主" min-width="96">
        <template #default="{ row }">
          {{ row.lordName?.trim() || "—" }}
        </template>
      </el-table-column>
      <el-table-column label="游戏日期" width="120">
        <template #default="{ row }">
          {{ row.dateLabel?.trim() || "—" }}
        </template>
      </el-table-column>
      <el-table-column label="保存时间" min-width="160">
        <template #default="{ row }">
          {{ formatSavedAt(row.savedAtUtc) }}
        </template>
      </el-table-column>
    </el-table>

    <template #footer>
      <el-button type="primary" :loading="loading" @click="onSave">存档</el-button>
      <el-button type="success" :loading="loading" @click="onLoad">读档</el-button>
      <el-button @click="close">取消</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  margin: 0 0 12px;
  font-size: 0.82rem;
  color: #64748b;
  line-height: 1.45;
}

.slot-table {
  width: 100%;
}

.slot-no {
  font-weight: 600;
}

.status-empty {
  color: #94a3b8;
}

.status-occupied {
  color: #059669;
}

:deep(.save-slot-row--selected > td) {
  background: #eff6ff !important;
}
</style>
