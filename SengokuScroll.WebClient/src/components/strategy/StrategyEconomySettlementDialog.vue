<script setup lang="ts">
import { computed } from "vue";
import type { StrategyEconomySettlementDetail } from "@/api/strategyTypes";
import { formatFoodGo, formatMoney } from "@/utils/strategyDisplayUnits";

const props = defineProps<{
  visible: boolean;
  detail: StrategyEconomySettlementDetail | null;
}>();

const emit = defineEmits<{
  "update:visible": [boolean];
}>();

const isAnnual = computed(() => props.detail?.period === "Annual");

const title = computed(() => {
  if (!props.detail) return "收支结算";
  if (isAnnual.value) {
    return `${props.detail.reportingYear}年年度收支结算`;
  }
  return `${props.detail.reportingYear}年${props.detail.reportingMonth}月月度收支结算`;
});

const incomeLabel = computed(() => (isAnnual.value ? "年度贡纳收入" : "上月贡纳收入"));
const emptyHint = computed(() =>
  isAnnual.value ? "上年无运输队抵达当主居城。" : "上月无运输队抵达当主居城。"
);

const tableRows = computed(() => props.detail?.tributeLines ?? []);

const tableFoodTotal = computed(() =>
  tableRows.value.reduce((sum, row) => sum + row.food, 0)
);

const tableMoneyTotal = computed(() =>
  tableRows.value.reduce((sum, row) => sum + row.money, 0)
);

const netMoney = computed(() => {
  if (!props.detail) return 0;
  return props.detail.totalMoney - props.detail.expenseMoney;
});

function tributeSummaryMethod(param: { columns: unknown[] }) {
  const sums: string[] = [];
  param.columns.forEach((_, index) => {
    if (index === 0) sums[index] = "合计";
    else if (index === 3) sums[index] = formatFoodGo(tableFoodTotal.value);
    else if (index === 4) sums[index] = formatMoney(tableMoneyTotal.value);
    else sums[index] = "";
  });
  return sums;
}

function onClose() {
  emit("update:visible", false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="title"
    width="min(720px, 92vw)"
    destroy-on-close
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <template v-if="detail">
      <div class="summary-grid">
        <div class="summary-item">
          <span class="label">{{ incomeLabel }}（粮）</span>
          <span class="value">🌾 {{ formatFoodGo(detail.totalFood) }}</span>
        </div>
        <div class="summary-item">
          <span class="label">{{ incomeLabel }}（金）</span>
          <span class="value">💰 {{ formatMoney(detail.totalMoney) }}</span>
        </div>
        <div class="summary-item">
          <span class="label">本月维持费（总）</span>
          <span class="value">💰 {{ formatMoney(detail.expenseMoney) }}</span>
        </div>
        <div class="summary-item">
          <span class="label">其中军队维护</span>
          <span class="value">💰 {{ formatMoney(detail.armyMaintenanceMoney) }}</span>
        </div>
        <div class="summary-item">
          <span class="label">贡纳减维持（金）</span>
          <span class="value" :class="{ negative: netMoney < 0 }">
            💰 {{ formatMoney(netMoney) }}
          </span>
        </div>
        <div class="summary-item">
          <span class="label">结算后库藏</span>
          <span class="value">
            💰 {{ formatMoney(detail.treasuryMoney) }} · 🌾 {{ formatFoodGo(detail.treasuryFood) }}
          </span>
        </div>
        <div class="summary-item">
          <span class="label">运输批次</span>
          <span class="value">{{ detail.convoyCount }} 批</span>
        </div>
      </div>

      <el-table
        v-if="tableRows.length"
        :data="tableRows"
        size="small"
        stripe
        class="tribute-table"
        show-summary
        :summary-method="tributeSummaryMethod"
        :empty-text="emptyHint"
      >
        <el-table-column prop="originName" label="据点" min-width="96" />
        <el-table-column prop="forceName" label="势力" min-width="88" />
        <el-table-column prop="lordName" label="领主" min-width="88" />
        <el-table-column label="贡粮（石）" min-width="100" align="right">
          <template #default="{ row }">{{ formatFoodGo(row.food) }}</template>
        </el-table-column>
        <el-table-column label="贡金（贯）" min-width="100" align="right">
          <template #default="{ row }">{{ formatMoney(row.money) }}</template>
        </el-table-column>
      </el-table>
      <p v-else class="empty-hint">{{ emptyHint }}</p>
    </template>
    <p v-else class="empty-hint">暂无结构化结算数据，请重启 WebApi 后重试。</p>

    <template #footer>
      <el-button type="primary" @click="onClose">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 10px 16px;
  margin-bottom: 16px;
  padding: 12px;
  background: #0f172a;
  border-radius: 8px;
  border: 1px solid #334155;
}

.summary-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.summary-item .label {
  font-size: 0.75rem;
  color: #94a3b8;
}

.summary-item .value {
  font-size: 0.9rem;
  color: #e2e8f0;
}

.summary-item .value.negative {
  color: #f87171;
}

.tribute-table {
  width: 100%;
}

.empty-hint {
  margin: 0;
  color: #94a3b8;
  font-size: 0.9rem;
}
</style>
