<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

const props = defineProps<{
  rows: IntelFieldRow[];
  /** 描述列表列数，默认随视口自适应（3–5 列）。 */
  column?: number;
  /** 开发字段标题格样式：文字更浅 / 背景更浅。 */
  devLabelStyle?: "text" | "background";
}>();

const viewportWidth = ref(typeof window !== "undefined" ? window.innerWidth : 1280);

const descriptionColumns = computed(() => {
  if (props.column != null) return props.column;
  if (viewportWidth.value >= 1280) return 5;
  if (viewportWidth.value >= 960) return 4;
  return 3;
});

/** 按列优先（自上而下）重排，配合 el-descriptions 行优先填充。 */
const displayRows = computed(() => {
  const cols = descriptionColumns.value;
  const rows = props.rows;
  const count = rows.length;
  if (count <= cols) return rows;

  const rowsPerCol = Math.ceil(count / cols);
  const ordered: IntelFieldRow[] = [];
  for (let row = 0; row < rowsPerCol; row++) {
    for (let col = 0; col < cols; col++) {
      const sourceIndex = col * rowsPerCol + row;
      if (sourceIndex < count) ordered.push(rows[sourceIndex]!);
    }
  }
  return ordered;
});

function syncViewportWidth() {
  viewportWidth.value = window.innerWidth;
}

onMounted(() => window.addEventListener("resize", syncViewportWidth));
onUnmounted(() => window.removeEventListener("resize", syncViewportWidth));
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
