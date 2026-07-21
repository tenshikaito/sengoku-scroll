<script setup lang="ts">
import { computed } from "vue";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

const props = defineProps<{
  rows: IntelFieldRow[];
  /** 描述列表列数（固定 3 列）。 */
  column?: number;
  /** 开发字段标题格样式：文字更浅 / 背景更浅。 */
  devLabelStyle?: "text" | "background";
}>();

const DESCRIPTION_COLUMNS = 3;

const descriptionColumns = computed(() => props.column ?? DESCRIPTION_COLUMNS);

/** 按列优先分列，再按行展开以配合 el-descriptions 的行优先填充。 */
function reorderForColumnMajor(rows: IntelFieldRow[], cols: number): IntelFieldRow[] {
  if (rows.length <= cols) return rows;

  const columns: IntelFieldRow[][] = [];
  const count = rows.length;
  const base = Math.floor(count / cols);
  const extra = count % cols;
  let index = 0;
  for (let col = 0; col < cols; col++) {
    const height = base + (col < extra ? 1 : 0);
    columns.push(rows.slice(index, index + height));
    index += height;
  }

  const maxRows = Math.max(...columns.map((column) => column.length));
  const ordered: IntelFieldRow[] = [];
  for (let row = 0; row < maxRows; row++) {
    for (let col = 0; col < cols; col++) {
      const item = columns[col]?.[row];
      if (item) ordered.push(item);
    }
  }
  return ordered;
}

const displayRows = computed(() =>
  reorderForColumnMajor(props.rows, descriptionColumns.value)
);
</script>

<template>
  <el-descriptions
    :column="descriptionColumns"
    border
    size="small"
    :class="[
      'basic-descriptions',
      devLabelStyle ? `basic-descriptions--dev-${devLabelStyle}` : '',
    ]"
  >
    <el-descriptions-item
      v-for="(row, index) in displayRows"
      :key="`${row.label}-${index}`"
      :label="row.label"
      :class-name="row.dev ? 'is-dev-field' : ''"
      :label-class-name="row.dev ? 'is-dev-field' : ''"
    >
      {{ row.value }}
    </el-descriptions-item>
  </el-descriptions>
</template>

<style scoped>
.basic-descriptions :deep(.el-descriptions__label) {
  width: 88px;
  font-weight: 600;
  color: #475569;
  background: #f1f5f9 !important;
}

.basic-descriptions :deep(.el-descriptions__content) {
  color: #0f172a;
  background: #fff;
  word-break: break-word;
}

.basic-descriptions--dev-background :deep(.el-descriptions__label.is-dev-field) {
  color: #64748b;
  background: #fafbfc !important;
}

.basic-descriptions--dev-text :deep(.el-descriptions__label.is-dev-field) {
  color: #94a3b8;
}
</style>
