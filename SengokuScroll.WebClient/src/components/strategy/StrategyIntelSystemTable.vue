<script setup lang="ts">
import { ref, watch } from "vue";
import type { IntelTableColumnDef } from "@/utils/strategyIntelSystemColumns";

const props = defineProps<{
  rows: Array<Record<string, unknown>>;
  columns: IntelTableColumnDef[];
  emptyText?: string;
  maxHeight?: number | string;
  highlightCurrent?: boolean;
  rowClassName?: (row: Record<string, unknown>) => string;
  /** 高亮指定 id 的行（与 rows[].id 对应，表示当前选中项）。 */
  currentId?: number | null;
}>();

const emit = defineEmits<{
  "current-change": [row: Record<string, unknown> | null];
}>();

const tableRef = ref<{
  setCurrentRow: (row: Record<string, unknown> | undefined) => void;
} | null>(null);

watch(
  () => [props.rows, props.currentId] as const,
  ([rows, currentId]) => {
    if (!rows.length || currentId == null) {
      tableRef.value?.setCurrentRow(undefined);
      return;
    }
    const row = rows.find((item) => Number(item.id) === currentId);
    tableRef.value?.setCurrentRow(row);
    if (!row) {
      emit("current-change", null);
    }
  },
  { immediate: true, flush: "post" }
);

function onCurrentChange(row: Record<string, unknown> | null) {
  emit("current-change", row);
}

function resolveRowClass({ row }: { row: Record<string, unknown> }) {
  return props.rowClassName?.(row) ?? "";
}
</script>

<template>
  <el-table
    ref="tableRef"
    :data="rows"
    size="small"
    stripe
    border
    :empty-text="emptyText ?? '暂无数据'"
    :max-height="maxHeight ?? 220"
    :highlight-current-row="highlightCurrent ?? true"
    :row-class-name="props.rowClassName ? resolveRowClass : undefined"
    @current-change="onCurrentChange"
  >
    <el-table-column
      v-for="col in columns"
      :key="col.prop"
      :prop="col.prop"
      :label="col.label"
      :width="col.width"
      :min-width="col.minWidth"
      :align="col.align"
      :header-cell-class-name="col.devOnly ? 'intel-dev-header' : ''"
      show-overflow-tooltip
    />
  </el-table>
</template>

<style scoped>
:deep(.el-table__body tr.current-row > td.el-table__cell) {
  background-color: #dbeafe !important;
}

:deep(th.el-table__cell.intel-dev-header) {
  color: #e2e8f0;
  font-weight: 500;
}
</style>
