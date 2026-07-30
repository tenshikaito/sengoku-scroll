<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type {
  StrategyMarketOpenOrder,
  StrategyMarketSnapshot,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import {
  strongholdLordCancelMarketOrder,
  strongholdLordSmashBuyFood,
  strongholdLordSmashBuyHorse,
  strongholdLordSmashSellFood,
  strongholdLordSmashSellHorse,
  unitSmashBuyFood,
  unitSmashBuyHorse,
  unitSmashSellFood,
  unitSmashSellHorse,
} from "@/api/strategy";
import StrategyMarketEchartsPanel from "@/components/strategy/StrategyMarketEchartsPanel.vue";
import {
  formatMarketQuantityFromGo,
  resolveMaxTradeUnits,
  resolveTradeQuantityGo as resolveTradeQuantityGoForMeta,
  type MarketCommodityMeta,
  type MarketCommodityTab,
} from "@/utils/strategyCommodityHelpers";
import { formatMoney } from "@/utils/strategyDisplayUnits";
import {
  depthRowPriceClass,
  formatMarketPrice,
  resolveSessionPrice,
} from "@/utils/strategyMarketChart";
import {
  padAskRowsForDisplay,
  padBidRowsForDisplay,
  sliceAskRowsForDisplay,
  sliceBidRowsForDisplay,
  sumAskVolumeUpToPrice,
  sumBidVolumeFromPrice,
} from "@/utils/strategyMarketBookHelpers";
import {
  buildTradeQuantityKokuMarks,
  buildTradeQuantityKokuStops,
} from "@/utils/strategyMarketTradeHelpers";

export type MarketTradeMode = "view" | "lord" | "unit";

const DEPTH_MAX = 10;
const DEPTH_DEFAULT = 5;

type TradeTab = "buy" | "sell";

const TRADE_TABS: { key: TradeTab; label: string }[] = [
  { key: "buy", label: "买入" },
  { key: "sell", label: "卖出" },
];

const props = defineProps<{
  snapshot: StrategyMarketSnapshot | null;
  commodityMeta: MarketCommodityMeta;
  commodityKey: MarketCommodityTab;
  tradeMode: MarketTradeMode;
  strongholdId: number | null;
  tradeUnit?: StrategyUnitState | null;
  lordMoney?: number;
  lordFood?: number;
  lordHorse?: number;
  /** 父级拉取快照时的 loading。 */
  loading?: boolean;
  /** K 线是否可初始化（对话框已展开且有数据）。 */
  chartActive?: boolean;
  refreshToken?: number;
}>();

const emit = defineEmits<{
  traded: [state: StrategyWorldState];
  refresh: [];
}>();

const tradeTab = ref<TradeTab>("buy");
const side = computed(() => (tradeTab.value === "sell" ? "sell" : "buy"));
const limitPrice = ref(100);
const quantityUnits = ref(0);
const depthExpanded = ref(false);
const actionLoading = ref(false);
const error = ref("");
const cancellingOrderId = ref<number | null>(null);

const treasuryMoneyOverride = ref<number | null>(null);
const treasuryStockOverride = ref<number | null>(null);

const tradeUnitId = computed(() => props.tradeUnit?.id ?? null);

const canTrade = computed(() => {
  if (props.tradeMode === "view" || props.snapshot?.isOpen !== true) return false;
  if (props.tradeMode === "lord") return props.strongholdId != null;
  return tradeUnitId.value != null;
});

const commodityTradeEnabled = computed(
  () => canTrade.value && props.commodityMeta.tradeEnabled,
);

function resolveStockFromProps(metaKey: MarketCommodityTab): number {
  if (metaKey === "Horse") {
    return props.tradeMode === "lord"
      ? (props.lordHorse ?? 0)
      : (props.tradeUnit?.horse ?? 0);
  }
  return props.tradeMode === "lord" ? (props.lordFood ?? 0) : (props.tradeUnit?.food ?? 0);
}

const treasuryMoney = computed(() => {
  if (treasuryMoneyOverride.value != null) return treasuryMoneyOverride.value;
  return props.tradeMode === "lord" ? (props.lordMoney ?? 0) : (props.tradeUnit?.money ?? 0);
});

const treasuryStock = computed(() => {
  if (treasuryStockOverride.value != null) return treasuryStockOverride.value;
  return resolveStockFromProps(props.commodityKey);
});

function resetTreasuryOverrides() {
  treasuryMoneyOverride.value = null;
  treasuryStockOverride.value = null;
}

function applyTreasuryFromState(state: StrategyWorldState) {
  if (props.tradeMode === "lord" && props.strongholdId != null) {
    const sh = state.strongholds.find((row) => row.id === props.strongholdId);
    if (sh) {
      treasuryMoneyOverride.value = sh.money;
      treasuryStockOverride.value =
        props.commodityKey === "Horse" ? (sh.horse ?? 0) : sh.food;
    }
    return;
  }

  const unitId = tradeUnitId.value;
  if (unitId == null) return;
  const unit = state.units.find((row) => row.id === unitId);
  if (unit) {
    treasuryMoneyOverride.value = unit.money;
    treasuryStockOverride.value =
      props.commodityKey === "Horse" ? (unit.horse ?? 0) : unit.food;
  }
}

const treasuryLabel = computed(() =>
  props.tradeMode === "lord" ? "官府库" : (props.tradeUnit?.name ?? "商队"),
);

const treasuryMoneyText = computed(() => formatMoney(treasuryMoney.value));
const treasuryStockText = computed(() =>
  formatMarketQuantityFromGo(treasuryStock.value, props.commodityMeta),
);

const dailyBars = computed(() => props.snapshot?.dailyBars ?? []);

const sessionPrice = computed(() => {
  const fromSnapshot = props.snapshot?.sessionPriceMoneyPerGo ?? 0;
  if (fromSnapshot > 0) return fromSnapshot;
  return resolveSessionPrice(
    dailyBars.value,
    props.snapshot?.lastClosePriceMoneyPerGo ?? 0,
  );
});

const depthCount = computed(() => (depthExpanded.value ? DEPTH_MAX : DEPTH_DEFAULT));

function toggleDepthExpanded() {
  depthExpanded.value = !depthExpanded.value;
}

function askDepthRank(index: number): number {
  return depthCount.value - index;
}

function bidDepthRank(index: number): number {
  return index + 1;
}

function formatDepthVolume(quantityGo: number): string {
  return formatMarketQuantityFromGo(quantityGo, props.commodityMeta);
}

const maxTradeUnits = computed(() =>
  resolveMaxTradeUnits(
    side.value,
    props.commodityMeta,
    treasuryMoney.value,
    treasuryStock.value,
    limitPrice.value,
  ),
);

function applyQuantityFromBookGo(totalGo: number) {
  const meta = props.commodityMeta;
  if (totalGo <= 0) {
    quantityUnits.value = 0;
    return;
  }

  const units = meta.usesKokuVolume
    ? Math.max(1, Math.round(totalGo / meta.goPerDisplayUnit))
    : Math.max(1, totalGo);
  const max = maxTradeUnits.value;
  quantityUnits.value = max > 0 ? Math.min(units, max) : 0;
}

function selectAskPrice(price: number) {
  if (price <= 0) return;
  limitPrice.value = price;
  if (!canTrade.value || side.value !== "buy" || !props.snapshot) return;

  const totalGo = sumAskVolumeUpToPrice(
    props.snapshot.askLevels ?? [],
    price,
    sessionPrice.value,
    props.snapshot.closeLevelQuantityGo ?? 0,
  );
  applyQuantityFromBookGo(totalGo);
}

function selectBidPrice(price: number) {
  if (price <= 0) return;
  limitPrice.value = price;
  if (!canTrade.value || side.value !== "sell" || !props.snapshot) return;

  const totalGo = sumBidVolumeFromPrice(
    props.snapshot.bidLevels ?? [],
    price,
    sessionPrice.value,
    props.snapshot.closeLevelQuantityGo ?? 0,
  );
  applyQuantityFromBookGo(totalGo);
}

const visibleAskRows = computed(() =>
  padAskRowsForDisplay(
    sliceAskRowsForDisplay(
      props.snapshot?.askLevels ?? [],
      depthCount.value,
      sessionPrice.value,
      props.snapshot?.closeLevelQuantityGo ?? 0,
    ),
    depthCount.value,
  ),
);

const visibleBidRows = computed(() =>
  padBidRowsForDisplay(
    sliceBidRowsForDisplay(
      props.snapshot?.bidLevels ?? [],
      depthCount.value,
      sessionPrice.value,
      props.snapshot?.closeLevelQuantityGo ?? 0,
    ),
    depthCount.value,
  ),
);

const playerOpenOrders = computed(() => props.snapshot?.playerOpenOrders ?? []);

const quantityStops = computed(() => buildTradeQuantityKokuStops(maxTradeUnits.value));
const quantityUnitMarks = computed(() => buildTradeQuantityKokuMarks(quantityStops.value));

const quantitySliderLabel = computed(() =>
  quantityUnits.value <= 0
    ? `0 = 尽可能多`
    : `限价 ${quantityUnits.value} ${props.commodityMeta.quantityStepLabel}`,
);

function formatOrderTime(order: StrategyMarketOpenOrder): string {
  if (order.createdYear <= 0) return "—";
  return `${order.createdYear}/${order.createdMonth}/${order.createdDay}`;
}

function formatOrderSideLabel(sideValue: string): string {
  return sideValue.toLowerCase() === "sell" ? "卖" : "买";
}

function formatOrderSideClass(sideValue: string): string {
  return sideValue.toLowerCase() === "sell" ? "order-side--sell" : "order-side--buy";
}

function formatOrderQuantityNumber(quantityGo: number): string {
  return formatMarketQuantityFromGo(quantityGo, props.commodityMeta);
}

function formatFillStatusLabel(order: StrategyMarketOpenOrder): string {
  if (order.fillStatus === "Filled") return "已成";
  if (order.fillStatus === "Partial") return "部成";
  return "未成";
}

function formatFillStatusClass(order: StrategyMarketOpenOrder): string {
  if (order.fillStatus === "Filled") return "order-fill--filled";
  if (order.fillStatus === "Partial") return "order-fill--partial";
  return "order-fill--open";
}

function isOrderCancellable(order: StrategyMarketOpenOrder): boolean {
  return order.quantityGo > 0;
}

function formatFilledVolumeText(order: StrategyMarketOpenOrder): string {
  const filled = formatOrderQuantityNumber(order.filledQuantityGo);
  const total = formatOrderQuantityNumber(order.originalQuantityGo);
  return `${filled}/${total}（${formatFillStatusLabel(order)}）`;
}

function resetTradeFormState() {
  tradeTab.value = "buy";
  quantityUnits.value = 0;
  depthExpanded.value = false;
  error.value = "";
  cancellingOrderId.value = null;
}

function syncLimitPriceFromSnapshot() {
  const snap = props.snapshot;
  if (!snap) return;
  if (snap.lastClosePriceMoneyPerGo > 0) {
    limitPrice.value = resolveSessionPrice(snap.dailyBars, snap.lastClosePriceMoneyPerGo);
  } else if (props.commodityMeta.defaultPriceMoneyPerUnit > 0) {
    limitPrice.value = props.commodityMeta.defaultPriceMoneyPerUnit;
  }
}

watch(
  () => props.commodityKey,
  () => {
    resetTradeFormState();
    resetTreasuryOverrides();
    syncLimitPriceFromSnapshot();
  },
);

watch(
  () => props.snapshot,
  () => {
    syncLimitPriceFromSnapshot();
  },
);

watch([side, maxTradeUnits, () => props.commodityKey], () => {
  quantityUnits.value = 0;
});

watch(quantityUnits, (value) => {
  const max = maxTradeUnits.value;
  if (value > 0 && max > 0 && value > max) quantityUnits.value = max;
});

watch(
  () =>
    [
      props.lordMoney,
      props.lordFood,
      props.lordHorse,
      props.tradeUnit?.money,
      props.tradeUnit?.food,
      props.tradeUnit?.horse,
      props.tradeMode,
    ] as const,
  () => {
    resetTreasuryOverrides();
  },
);

function resolveTradeQuantityGo(): number {
  if (quantityUnits.value <= 0) return 0;
  return resolveTradeQuantityGoForMeta(quantityUnits.value, props.commodityMeta);
}

function validateTradeRequest(quantityGo: number): string | null {
  const meta = props.commodityMeta;
  if (limitPrice.value <= 0) return "请输入有效价格";

  if (side.value === "sell") {
    if (treasuryStock.value <= 0) {
      return `${meta.name}不足（可能已有卖单锁定了库存）`;
    }
    const requestedGo = quantityGo > 0 ? quantityGo : treasuryStock.value;
    if (requestedGo > treasuryStock.value) {
      return `卖出数量超过可用${meta.volumeUnitLabel}`;
    }
    return null;
  }

  if (treasuryMoney.value <= 0) return "资金不足";
  if (quantityGo <= 0) return null;

  const cost = limitPrice.value * quantityGo;
  if (cost > treasuryMoney.value) return "买入所需资金超过可用金钱";
  return null;
}

async function submitTrade() {
  if (!commodityTradeEnabled.value || props.strongholdId == null) return;
  if (props.tradeMode === "unit" && !tradeUnitId.value) return;

  const quantityGo = resolveTradeQuantityGo();
  const validationError = validateTradeRequest(quantityGo);
  if (validationError) {
    error.value = validationError;
    return;
  }

  actionLoading.value = true;
  error.value = "";
  try {
    let nextState: StrategyWorldState | null = null;
    const isHorse = props.commodityKey === "Horse";
    if (props.tradeMode === "lord") {
      if (side.value === "buy") {
        nextState = isHorse
          ? await strongholdLordSmashBuyHorse(props.strongholdId, {
              maxPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            })
          : await strongholdLordSmashBuyFood(props.strongholdId, {
              maxPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            });
      } else {
        nextState = isHorse
          ? await strongholdLordSmashSellHorse(props.strongholdId, {
              minPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            })
          : await strongholdLordSmashSellFood(props.strongholdId, {
              minPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            });
      }
    } else if (tradeUnitId.value) {
      if (side.value === "buy") {
        nextState = isHorse
          ? await unitSmashBuyHorse(tradeUnitId.value, {
              maxPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            })
          : await unitSmashBuyFood(tradeUnitId.value, {
              maxPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            });
      } else {
        nextState = isHorse
          ? await unitSmashSellHorse(tradeUnitId.value, {
              minPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            })
          : await unitSmashSellFood(tradeUnitId.value, {
              minPriceMoneyPerGo: limitPrice.value,
              quantityGo,
            });
      }
    }
    if (!nextState) return;

    applyTreasuryFromState(nextState);
    emit("traded", nextState);
    emit("refresh");
    quantityUnits.value = 0;
  } catch (e) {
    error.value = e instanceof Error ? e.message : "成交失败";
  } finally {
    actionLoading.value = false;
  }
}

async function cancelOrder(orderId: number) {
  if (props.tradeMode !== "lord" || props.strongholdId == null) return;

  actionLoading.value = true;
  error.value = "";
  cancellingOrderId.value = orderId;
  try {
    const nextState = await strongholdLordCancelMarketOrder(props.strongholdId, {
      orderId,
      commodity: props.commodityKey,
    });
    applyTreasuryFromState(nextState);
    emit("traded", nextState);
    emit("refresh");
  } catch (e) {
    error.value = e instanceof Error ? e.message : "撤单失败";
  } finally {
    cancellingOrderId.value = null;
    actionLoading.value = false;
  }
}
</script>

<template>
  <div v-loading="loading || actionLoading" class="market-commodity-pane">
    <div class="market-layout">
      <section class="market-left-pane">
        <div class="market-chart-pane">
          <StrategyMarketEchartsPanel
            :daily-bars="dailyBars"
            :display-meta="commodityMeta"
            :chart-key="commodityKey"
            :refresh-token="refreshToken"
            :active="chartActive"
          />
        </div>

        <div v-if="canTrade && tradeMode === 'lord'" class="market-orders-pane">
          <div class="market-orders-pane__title">挂单列表</div>
          <div v-if="playerOpenOrders.length === 0" class="trade-readonly-hint">
            暂无未成交挂单。
          </div>
          <div v-else class="order-list">
            <div class="order-list__head">
              <span>挂单时间</span>
              <span>方向</span>
              <span>价格（{{ commodityMeta.priceUnitLabel }}）</span>
              <span>挂单量（{{ commodityMeta.volumeUnitLabel }}）</span>
              <span>成交量（{{ commodityMeta.volumeUnitLabel }}）</span>
              <span />
            </div>
            <div v-for="order in playerOpenOrders" :key="order.id" class="order-row">
              <span class="order-time">{{ formatOrderTime(order) }}</span>
              <span class="order-side" :class="formatOrderSideClass(order.side)">
                {{ formatOrderSideLabel(order.side) }}
              </span>
              <span class="order-price">{{ formatMarketPrice(order.priceMoneyPerGo) }}</span>
              <span class="order-qty">{{ formatOrderQuantityNumber(order.quantityGo) }}</span>
              <span class="order-fill" :class="formatFillStatusClass(order)">
                {{ formatFilledVolumeText(order) }}
              </span>
              <el-button
                v-if="isOrderCancellable(order)"
                size="small"
                type="danger"
                plain
                :loading="cancellingOrderId === order.id"
                @click="cancelOrder(order.id)"
              >
                撤单
              </el-button>
              <span v-else />
            </div>
          </div>
        </div>
      </section>

      <section class="market-book-pane">
        <div class="depth-book">
          <div class="depth-header">
            <span class="depth-rank" />
            <span class="depth-price">价格/贯</span>
            <span class="depth-qty">数量/{{ commodityMeta.volumeUnitLabel }}</span>
          </div>

          <div
            v-for="(level, idx) in visibleAskRows"
            :key="`a-${idx}`"
            class="depth-row"
            :class="[
              level.priceMoneyPerGo > 0 ? 'depth-row--pickable' : 'depth-row--empty',
              level.priceMoneyPerGo > 0 ? depthRowPriceClass(level.priceMoneyPerGo, sessionPrice) : '',
            ]"
            @click="level.priceMoneyPerGo > 0 && selectAskPrice(level.priceMoneyPerGo)"
          >
            <span class="depth-rank">{{ askDepthRank(idx) }}</span>
            <template v-if="level.priceMoneyPerGo > 0">
              <span class="depth-price">{{ formatMarketPrice(level.priceMoneyPerGo) }}</span>
              <span class="depth-qty">{{ formatDepthVolume(level.quantityGo) }}</span>
            </template>
            <template v-else>
              <span class="depth-price depth-cell--empty">—</span>
              <span class="depth-qty depth-cell--empty">—</span>
            </template>
          </div>

          <div class="depth-depth-bar">
            <div
              class="depth-depth-toggle"
              role="button"
              tabindex="0"
              :title="depthExpanded ? '点击收起为 5 档' : '点击展开为 10 档'"
              @click.stop="toggleDepthExpanded"
              @keydown.enter.prevent="toggleDepthExpanded"
              @keydown.space.prevent="toggleDepthExpanded"
            >
              <span class="depth-depth-toggle__icon" :class="{ 'is-active': !depthExpanded }">▼</span>
              <span class="depth-depth-toggle__icon" :class="{ 'is-active': depthExpanded }">▲</span>
            </div>
          </div>

          <div
            v-for="(level, idx) in visibleBidRows"
            :key="`b-${idx}`"
            class="depth-row"
            :class="[
              level.priceMoneyPerGo > 0 ? 'depth-row--pickable' : 'depth-row--empty',
              level.priceMoneyPerGo > 0 ? depthRowPriceClass(level.priceMoneyPerGo, sessionPrice) : '',
            ]"
            @click="level.priceMoneyPerGo > 0 && selectBidPrice(level.priceMoneyPerGo)"
          >
            <span class="depth-rank">{{ bidDepthRank(idx) }}</span>
            <template v-if="level.priceMoneyPerGo > 0">
              <span class="depth-price">{{ formatMarketPrice(level.priceMoneyPerGo) }}</span>
              <span class="depth-qty">{{ formatDepthVolume(level.quantityGo) }}</span>
            </template>
            <template v-else>
              <span class="depth-price depth-cell--empty">—</span>
              <span class="depth-qty depth-cell--empty">—</span>
            </template>
          </div>
        </div>

        <div v-if="tradeMode === 'view'" class="trade-readonly-hint">
          个人仅可查看行情；大宗买卖须通过商队或势力交易任务。
        </div>

        <div v-else-if="!commodityMeta.tradeEnabled" class="trade-readonly-hint">
          「{{ commodityMeta.name }}」交易尚未实装。
        </div>

        <div v-else-if="canTrade" class="trade-form">
          <div class="trade-unit-hint">
            {{ treasuryLabel }}：💰{{ treasuryMoneyText }} · {{ commodityMeta.treasuryIcon }}{{ treasuryStockText }}{{ commodityMeta.volumeUnitLabel }}
          </div>

          <div class="trade-tabs">
            <button
              v-for="tab in TRADE_TABS"
              :key="tab.key"
              type="button"
              class="trade-tab"
              :class="{ active: tradeTab === tab.key }"
              @click="tradeTab = tab.key"
            >
              {{ tab.label }}
            </button>
          </div>

          <template v-if="tradeTab === 'buy' || tradeTab === 'sell'">
            <div class="trade-fields">
              <label class="trade-field">
                <span class="trade-field__label">交易价格（{{ commodityMeta.priceUnitLabel }}）</span>
                <el-input-number
                  v-model="limitPrice"
                  class="trade-field__input trade-field__input--block"
                  :min="1"
                  :step="1"
                  controls-position="right"
                />
              </label>
              <label class="trade-field">
                <span class="trade-field__label">数量（{{ commodityMeta.quantityStepLabel }}）</span>
                <el-input-number
                  v-model="quantityUnits"
                  class="trade-field__input trade-field__input--block"
                  :min="0"
                  :max="Math.max(0, maxTradeUnits)"
                  :step="1"
                  controls-position="right"
                  :disabled="maxTradeUnits <= 0"
                />
                <div class="trade-field__hint">{{ quantitySliderLabel }}</div>
                <el-slider
                  v-model="quantityUnits"
                  class="quantity-slider"
                  :min="0"
                  :max="Math.max(0, maxTradeUnits)"
                  :step="1"
                  :marks="quantityUnitMarks"
                  :disabled="maxTradeUnits <= 0"
                />
              </label>
            </div>
            <el-button type="primary" :disabled="!commodityTradeEnabled" @click="submitTrade">
              确认{{ side === "buy" ? "买入" : "卖出" }}
            </el-button>
          </template>
        </div>
      </section>
    </div>

    <p v-if="error" class="market-error">{{ error }}</p>
  </div>
</template>

<style scoped>
.market-commodity-pane {
  min-height: 480px;
}

.market-layout {
  display: grid;
  grid-template-columns: 1fr 220px;
  gap: 16px;
  min-height: 480px;
}

.market-left-pane {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 480px;
}

.market-chart-pane {
  flex: 1;
  min-height: 280px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 8px;
}

.market-orders-pane {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 10px 16px;
  max-height: 220px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.market-orders-pane__title {
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.market-book-pane {
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  gap: 10px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 10px;
}

.depth-book {
  flex: 0 0 auto;
  display: flex;
  flex-direction: column;
  font-size: 12px;
  line-height: 1.6;
}

.depth-header,
.depth-row {
  display: grid;
  grid-template-columns: 22px 1fr auto;
  gap: 6px;
  align-items: center;
}

.depth-header {
  padding: 0 2px 4px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
  border-bottom: 1px solid var(--el-border-color-lighter);
  margin-bottom: 2px;
}

.depth-row {
  padding: 1px 2px;
  min-height: 18px;
}

.depth-rank {
  text-align: center;
  font-size: 10px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
  user-select: none;
}

.depth-row--empty .depth-rank {
  color: var(--el-text-color-placeholder);
}

.depth-row--pickable {
  cursor: pointer;
  border-radius: 3px;
  padding: 1px 4px;
}

.depth-row--pickable:hover {
  background: var(--el-fill-color-light);
}

.depth-row--empty {
  color: var(--el-text-color-placeholder);
  cursor: default;
}

.depth-cell--empty {
  color: var(--el-text-color-placeholder);
  letter-spacing: 0.08em;
}

.depth-row--above {
  color: #16a34a;
}

.depth-row--below {
  color: #dc2626;
}

.depth-row--close {
  color: #6b7280;
  font-weight: 600;
}

.depth-depth-bar {
  margin: 0;
}

.depth-depth-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  height: 14px;
  margin: 2px 0;
  border-top: 1px solid var(--el-border-color);
  border-bottom: 1px solid var(--el-border-color-lighter);
  cursor: pointer;
  color: var(--el-text-color-secondary);
  font-size: 10px;
  line-height: 1;
}

.depth-depth-toggle:hover {
  color: var(--el-color-primary);
}

.depth-depth-toggle__icon {
  opacity: 0.35;
  user-select: none;
}

.depth-depth-toggle__icon.is-active {
  opacity: 1;
  color: var(--el-color-primary);
}

.depth-price {
  text-align: left;
}

.depth-qty {
  text-align: right;
}

.trade-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.trade-field__label {
  font-size: 12px;
  color: var(--el-text-color-regular);
}

.trade-field__input--block {
  width: 100%;
}

.trade-field__input--block :deep(.el-input-number) {
  width: 100%;
}

.trade-field__hint {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
}

.quantity-slider {
  padding: 0 4px 28px 0;
}

.quantity-slider :deep(.el-slider__marks-text) {
  font-size: 10px;
  cursor: pointer;
}

.trade-readonly-hint {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
}

.trade-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
  border-top: 1px solid var(--el-border-color-lighter);
  padding-top: 8px;
}

.trade-tabs {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 6px;
}

.trade-tab {
  border: 1px solid var(--el-border-color);
  background: var(--el-fill-color-blank);
  border-radius: 8px;
  padding: 8px 4px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
}

.trade-tab.active {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}

.order-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
  min-height: 0;
  overflow: auto;
  font-size: 11px;
}

.order-list__head,
.order-row {
  display: grid;
  grid-template-columns:
    minmax(76px, 1fr)
    minmax(40px, 0.55fr)
    minmax(48px, 0.65fr)
    minmax(68px, 0.85fr)
    minmax(148px, 1.35fr)
    52px;
  gap: 10px 14px;
  align-items: center;
  padding: 0 4px;
}

.order-list__head {
  color: var(--el-text-color-secondary);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-size: 11px;
}

.order-row {
  padding: 5px 6px;
  border-bottom: 1px dashed var(--el-border-color-extra-light);
}

.order-side--buy {
  color: #dc2626;
  font-weight: 600;
}

.order-side--sell {
  color: #16a34a;
  font-weight: 600;
}

.order-fill {
  font-variant-numeric: tabular-nums;
  line-height: 1.2;
  white-space: nowrap;
}

.order-fill--open {
  color: var(--el-text-color-regular);
}

.order-fill--partial {
  color: var(--el-color-warning);
}

.order-fill--filled {
  color: var(--el-color-success);
}

.order-price,
.order-qty,
.order-time {
  font-variant-numeric: tabular-nums;
}

.trade-unit-hint {
  font-size: 12px;
}

.trade-fields {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.market-error {
  margin-top: 8px;
  color: var(--el-color-danger);
  font-size: 13px;
}
</style>
