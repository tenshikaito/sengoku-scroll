<script setup lang="ts">
import { computed } from "vue";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

const props = withDefaults(
  defineProps<{
    rows: IntelFieldRow[];
    /** hover：地图悬浮框深色主题；dialog：情报对话框浅色主题。 */
    variant?: "hover" | "dialog";
    labelWidth?: string;
    /** dialog 模式下字段列数（单位/据点详情为 3 列）。 */
    columns?: 1 | 2 | 3;
    /** 开发字段标题样式。 */
    devLabelStyle?: "text" | "background";
  }>(),
  {
    variant: "hover",
    labelWidth: "4.2em",
    columns: 1,
    devLabelStyle: undefined,
  }
);

const rowsPerColumn = computed(() =>
  props.columns > 1 ? Math.ceil(props.rows.length / props.columns) : props.rows.length
);

const gridStyle = computed(() =>
  props.columns > 1
    ? ({ "--rows-per-col": String(rowsPerColumn.value) } as Record<string, string>)
    : undefined
);
</script>

<template>
  <dl
    class="intel-fields"
    :class="[
      variant,
      columns > 1 ? `columns-${columns}` : '',
      devLabelStyle ? `dev-label-${devLabelStyle}` : '',
    ]"
    :style="gridStyle"
  >
    <div v-for="(row, index) in rows" :key="`${row.label}-${index}`" class="row">
      <dt
        :class="{ 'is-dev-field': row.dev }"
        :style="columns === 1 ? { width: labelWidth } : undefined"
      >
        {{ row.label }}
      </dt>
      <dd>{{ row.value }}</dd>
    </div>
  </dl>
</template>

<style scoped>
.intel-fields {
  margin: 0;
  display: grid;
  gap: 3px;
}

.row {
  display: flex;
  gap: 8px;
  align-items: baseline;
  line-height: 1.45;
}

dt {
  margin: 0;
  flex-shrink: 0;
  color: #94a3b8;
}

dd {
  margin: 0;
  flex: 1;
  min-width: 0;
  word-break: break-word;
}

.hover {
  font-size: 0.78rem;
}

.hover dd {
  color: #e2e8f0;
}

.dialog {
  font-size: 0.88rem;
}

.dialog dt {
  color: #64748b;
}

.dialog dd {
  color: #0f172a;
}

.dialog.columns-2 {
  grid-auto-flow: column;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  grid-template-rows: repeat(var(--rows-per-col), auto);
  gap: 6px 20px;
}

.dialog.columns-2 .row {
  gap: 8px;
}

.dialog.columns-2 dt {
  width: 3.2em;
  flex-shrink: 0;
}

.dialog.dev-label-background dt.is-dev-field {
  background: #f8fafc;
  border-radius: 2px;
  padding: 0 4px;
  color: #64748b;
}

.dialog.dev-label-text dt.is-dev-field {
  color: #cbd5e1;
}

.dialog.columns-3 {
  grid-auto-flow: column;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-template-rows: repeat(var(--rows-per-col), auto);
  gap: 12px 16px;
}

.dialog.columns-3 .row {
  flex-direction: column;
  gap: 2px;
  align-items: stretch;
}

.dialog.columns-3 dt {
  width: auto;
  font-size: 0.75rem;
}

.dialog.columns-3 dd {
  font-size: 0.88rem;
}
</style>
