<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyMarketSnapshot, StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { fetchMarketSnapshot } from "@/api/strategy";
import StrategyMarketCommodityPane, {
  type MarketTradeMode,
} from "@/components/strategy/StrategyMarketCommodityPane.vue";
import {
  resolveMarketCommodityMeta,
  resolveMarketCommodityMetas,
  type MarketCommodityTab,
} from "@/utils/strategyCommodityHelpers";
import { logMarketBookSnapshot } from "@/utils/strategyMarketBookHelpers";

export type { MarketTradeMode };

const DEPTH_DEFAULT = 5;

const props = defineProps<{
  visible: boolean;
  worldState?: StrategyWorldState | null;
  strongholdId: number | null;
  strongholdName?: string;
  /** view=只读；lord=当主官府库；unit=商队库存 */
  tradeMode?: MarketTradeMode;
  tradeUnit?: StrategyUnitState | null;
  lordMoney?: number;
  lordFood?: number;
  lordHorse?: number;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  traded: [state: StrategyWorldState];
}>();

const loading = ref(false);
const loadError = ref("");
const snapshot = ref<StrategyMarketSnapshot | null>(null);
const commodityTab = ref<MarketCommodityTab>("Food");
const dialogOpened = ref(false);
const snapshotGeneration = ref(0);

const commodityMetas = computed(() => resolveMarketCommodityMetas(props.worldState?.masterData));
const activeCommodityMeta = computed(() =>
  resolveMarketCommodityMeta(commodityTab.value, props.worldState?.masterData),
);

const tradeMode = computed(() => props.tradeMode ?? "view");

const title = computed(() => {
  const place = props.strongholdName ?? snapshot.value?.strongholdName ?? "市场";
  if (tradeMode.value === "view") return `${place} · 行情`;
  if (tradeMode.value === "lord") return `${place} · 官府交易`;
  return `${place} · 商队交易`;
});

const dailyBars = computed(() => snapshot.value?.dailyBars ?? []);

const chartActive = computed(
  () => props.visible && dialogOpened.value && dailyBars.value.length > 0,
);

async function loadSnapshot() {
  if (!props.strongholdId) return;
  loading.value = true;
  loadError.value = "";
  try {
    snapshot.value = await fetchMarketSnapshot(props.strongholdId, commodityTab.value);
    snapshotGeneration.value += 1;
    if (snapshot.value) {
      logMarketBookSnapshot("loadSnapshot", snapshot.value, DEPTH_DEFAULT);
    }
  } catch (e) {
    loadError.value = e instanceof Error ? e.message : "加载市场失败";
    snapshot.value = null;
  } finally {
    loading.value = false;
  }
}

watch(
  () =>
    [
      props.visible,
      props.strongholdId,
      commodityTab.value,
      props.worldState?.date.year,
      props.worldState?.date.month,
      props.worldState?.date.day,
    ] as const,
  ([visible, id]) => {
    if (!visible || id == null) return;
    void loadSnapshot();
  },
);

function onTraded(state: StrategyWorldState) {
  emit("traded", state);
}

function onRefresh() {
  void loadSnapshot();
}

function closeDialog() {
  emit("update:visible", false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="title"
    width="960px"
    class="strategy-market-dialog"
    destroy-on-close
    @update:model-value="emit('update:visible', $event)"
    @opened="dialogOpened = true"
    @closed="dialogOpened = false"
  >
    <div v-if="snapshot && !snapshot.isOpen" class="market-closed-banner">
      市场已关闭（围城或封锁中）
    </div>

    <div v-if="commodityMetas.length > 1" class="commodity-tabs">
      <button
        v-for="tab in commodityMetas"
        :key="tab.key"
        type="button"
        class="commodity-tab"
        :class="{ active: commodityTab === tab.key }"
        @click="commodityTab = tab.key"
      >
        {{ tab.name }}
        <span v-if="!tab.tradeEnabled" class="commodity-tab__tag">占位</span>
      </button>
    </div>

    <StrategyMarketCommodityPane
      :key="commodityTab"
      :snapshot="snapshot"
      :commodity-meta="activeCommodityMeta"
      :commodity-key="commodityTab"
      :trade-mode="tradeMode"
      :stronghold-id="strongholdId"
      :trade-unit="tradeUnit"
      :lord-money="lordMoney"
      :lord-food="lordFood"
      :lord-horse="lordHorse"
      :loading="loading"
      :chart-active="chartActive"
      :refresh-token="snapshotGeneration"
      @traded="onTraded"
      @refresh="onRefresh"
    />

    <p v-if="loadError" class="market-error">{{ loadError }}</p>

    <template #footer>
      <el-button @click="closeDialog">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.market-closed-banner {
  margin-bottom: 8px;
  padding: 8px 12px;
  background: var(--el-color-warning-light-9);
  border-radius: 6px;
  color: var(--el-color-warning-dark-2);
  font-size: 13px;
}

.commodity-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.commodity-tab {
  border: 1px solid var(--el-border-color);
  background: var(--el-fill-color-blank);
  border-radius: 6px;
  padding: 6px 12px;
  cursor: pointer;
}

.commodity-tab.active {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}

.commodity-tab__tag {
  margin-left: 4px;
  font-size: 11px;
  opacity: 0.7;
}

.market-error {
  margin-top: 8px;
  color: var(--el-color-danger);
  font-size: 13px;
}
</style>
