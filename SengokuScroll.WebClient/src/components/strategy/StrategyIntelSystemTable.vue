<script setup lang="ts">
import { ref, watch } from "vue";
import type { IntelTableColumnDef } from "@/utils/strategyIntelSystemColumns";
import { resolveIntelBandTone } from "@/utils/strategyIntelDisplay";
import { diplomacyStatusCellClassName, intelRelationTierClass } from "@/intelDisplay/IntelDisplayBehaviors";
import { resolveIntelColumnLabel } from "@/i18n/intelColumns";
import { useI18n } from "@/i18n";
import {
  resolveIntelCellNavigateTarget,
  type IntelExcludeEntity,
  type IntelNavigateTarget,
} from "@/utils/strategyIntelNavigation";

const props = defineProps<{
  rows: Array<Record<string, unknown>>;
  columns: IntelTableColumnDef[];
  emptyText?: string;
  maxHeight?: number | string;
  highlightCurrent?: boolean;
  rowClassName?: (row: Record<string, unknown>) => string;
  /** 高亮指定 id 的行（与 rows[].id 对应，表示当前选中项）。 */
  currentId?: number | null;
  /** 外层滚动容器（宽高不足时出现滚动条）。 */
  scrollWrap?: boolean;
  /** 表格铺满容器宽度（消除 el-scrollbar__view 右侧空白）。 */
  fillWidth?: boolean;
  /** 不为当前正在查看的实体生成链接。 */
  excludeEntity?: IntelExcludeEntity | null;
}>();

const emit = defineEmits<{
  "current-change": [row: Record<string, unknown> | null];
  navigate: [target: IntelNavigateTarget];
}>();

const { t, locale } = useI18n();

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
  },
  { immediate: true, flush: "post" }
);

function onCurrentChange(row: Record<string, unknown> | null) {
  emit("current-change", row);
}

function resolveRowClass({ row }: { row: Record<string, unknown> }) {
  const parts: string[] = [];
  const custom = props.rowClassName?.(row);
  if (custom) parts.push(custom);
  if (props.currentId != null && Number(row.id) === props.currentId) {
    parts.push("intel-row-selected");
  }
  return parts.join(" ");
}

function shouldUseRowClass() {
  return props.rowClassName != null || props.currentId != null;
}

function resolveLowRedStatClass(value: unknown): string {
  const trimmed = String(value ?? "").trim();
  if (trimmed === "低") return "intel-stat--low";
  const n = Number(trimmed);
  if (Number.isFinite(n) && n < 40) return "intel-stat--low";
  return "";
}

const RELATION_TIER_PROPS = new Set(["relation", "trust", "relationTone"]);

function resolveCellClass(row: Record<string, unknown>, col: IntelTableColumnDef): string {
  if (col.toneField) {
    const tone = row[col.toneField];
    return diplomacyStatusCellClassName(tone == null ? "" : String(tone));
  }
  if (RELATION_TIER_PROPS.has(col.prop)) {
    return intelRelationTierClass(row[col.prop] == null ? "" : String(row[col.prop]));
  }
  if (col.lowRedStat) {
    return resolveLowRedStatClass(row[col.prop]);
  }
  if (!col.band) return "";
  const tone = resolveIntelBandTone(row[col.prop] == null ? "" : String(row[col.prop]));
  return tone ? `intel-band intel-band--${tone}` : "";
}

function onRowClick(row: Record<string, unknown>) {
  emit("current-change", row);
}

function columnLabel(col: IntelTableColumnDef): string {
  locale.value;
  return resolveIntelColumnLabel(col);
}

function cellNavigateTarget(
  row: Record<string, unknown>,
  col: IntelTableColumnDef,
): IntelNavigateTarget | null {
  return resolveIntelCellNavigateTarget(row, col, props.excludeEntity);
}

function onCellLinkClick(row: Record<string, unknown>, col: IntelTableColumnDef) {
  const target = cellNavigateTarget(row, col);
  if (!target) return;
  emit("navigate", target);
}
</script>

<template>
  <div
    :class="{
      'intel-table-scroll-wrap': scrollWrap,
      'intel-table-fill': fillWidth,
    }"
  >
    <el-table
      ref="tableRef"
      :data="rows"
      size="small"
      stripe
      border
      :style="fillWidth ? { width: '100%' } : undefined"
      :empty-text="emptyText ?? t('common.empty')"
      :max-height="scrollWrap ? undefined : (maxHeight ?? 220)"
      :highlight-current-row="highlightCurrent ?? true"
      :row-class-name="shouldUseRowClass() ? resolveRowClass : undefined"
      @current-change="onCurrentChange"
      @row-click="onRowClick"
    >
      <el-table-column
        v-for="col in columns"
        :key="col.prop"
        :prop="col.prop"
        :label="columnLabel(col)"
        :width="col.width"
        :min-width="col.minWidth"
        :align="col.align"
        :header-cell-class-name="col.devOnly ? 'intel-dev-header' : ''"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <button
            v-if="cellNavigateTarget(row, col)"
            type="button"
            class="intel-cell-link"
            :class="resolveCellClass(row, col)"
            @click.stop="onCellLinkClick(row, col)"
          >
            {{ row[col.prop] }}
          </button>
          <span v-else :class="resolveCellClass(row, col)">
            {{ row[col.prop] }}
          </span>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<style scoped>
.intel-table-scroll-wrap {
  overflow: auto;
  max-height: 360px;
  width: 100%;
}

.intel-table-scroll-wrap :deep(.el-table) {
  width: max-content;
  min-width: 100%;
}

.intel-table-fill :deep(.el-table__body-wrapper .el-scrollbar__view),
.intel-table-fill :deep(.el-table__header-wrapper .el-scrollbar__view) {
  display: block !important;
  width: 100% !important;
  vertical-align: top !important;
}

.intel-table-fill :deep(.el-table__body table),
.intel-table-fill :deep(.el-table__header table) {
  width: 100% !important;
  table-layout: fixed;
}

:deep(.el-table__body tr.current-row > td.el-table__cell),
:deep(.el-table__body tr.intel-row-selected > td.el-table__cell) {
  background-color: #dbeafe !important;
}

:deep(th.el-table__cell.intel-dev-header) {
  color: #e2e8f0;
  font-weight: 500;
}

:deep(.intel-band--high) {
  color: red;
  font-weight: 600;
}

:deep(.intel-band--mid) {
  color: orange;
  font-weight: 600;
}

:deep(.intel-band--low) {
  color: green;
  font-weight: 600;
}

:deep(.intel-stat--low) {
  color: #dc2626;
}

:deep(.dip-allied) {
  color: #16a34a;
}

:deep(.dip-enemy) {
  color: #dc2626;
}

:deep(.dip-neutral) {
  color: #ea580c;
}

:deep(.intel-tier--warn) {
  color: #ea580c;
  font-weight: normal;
}

:deep(.intel-tier--danger) {
  color: #dc2626;
  font-weight: normal;
}

:deep(.intel-tier--favorable) {
  color: #2563eb;
  font-weight: normal;
}

:deep(.intel-tier--close) {
  color: #16a34a;
  font-weight: normal;
}

.intel-cell-link {
  display: inline;
  padding: 0;
  border: none;
  background: none;
  font: inherit;
  color: #2563eb;
  text-decoration: underline;
  cursor: pointer;
  text-align: inherit;
}

.intel-cell-link:hover {
  color: #1d4ed8;
}

.intel-cell-link:focus-visible {
  outline: 2px solid #93c5fd;
  outline-offset: 1px;
}
</style>
