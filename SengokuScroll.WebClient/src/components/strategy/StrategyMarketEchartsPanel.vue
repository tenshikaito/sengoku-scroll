<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from "vue";
import * as echarts from "echarts/core";
import { BarChart, CandlestickChart } from "echarts/charts";
import {
  DataZoomComponent,
  GridComponent,
  TooltipComponent,
} from "echarts/components";
import { CanvasRenderer } from "echarts/renderers";
import type { StrategyMarketDailyBar } from "@/api/strategy";
import type { MarketCommodityMeta } from "@/utils/strategyCommodityHelpers";
import { resolveMarketCommodityMeta } from "@/utils/strategyCommodityHelpers";
import {
  aggregateMarketBars,
  computeLatestSessionStats,
  formatMarketTurnoverWen,
  formatSignedNumber,
  formatSignedPercent,
  priceToKanPerKoku,
  sessionPriceTrendClass,
  sessionSignedTrendClass,
  type MarketKPeriod,
  wenToKan,
  goToKoku,
} from "@/utils/strategyMarketChart";

echarts.use([
  CandlestickChart,
  BarChart,
  GridComponent,
  TooltipComponent,
  DataZoomComponent,
  CanvasRenderer,
]);

const props = defineProps<{
  dailyBars: StrategyMarketDailyBar[];
  /** 对话框已展开且数据就绪后再初始化/resize 图表。 */
  active?: boolean;
  /** 快照刷新计数，成交后递增以强制同步图表/报价。 */
  refreshToken?: number;
  /** 切换交易品类时强制销毁并重建图表。 */
  chartKey?: string;
  /** 价格轴与成交量单位（来自 master 商品定义）。 */
  displayMeta?: MarketCommodityMeta;
}>();

const displayMetaResolved = computed(
  () => props.displayMeta ?? resolveMarketCommodityMeta("Food"),
);

const K_PERIOD_TABS: { key: MarketKPeriod; label: string }[] = [
  { key: "day", label: "日K" },
  { key: "week", label: "周K" },
  { key: "month", label: "月K" },
  { key: "year", label: "年K" },
];

const UP_COLOR = "#dc2626";
const DOWN_COLOR = "#16a34a";

/** 相对初版设计的默认显示放大倍数（可见 K 线根数反比、柱宽正比）。 */
const DEFAULT_DISPLAY_ZOOM = 2.5;
/** 初版默认最多可见 K 线根数（80 根 @ 1.0x）。 */
const BASE_DEFAULT_VISIBLE_BARS = 80;
const DEFAULT_VISIBLE_BARS = Math.max(
  1,
  Math.round(BASE_DEFAULT_VISIBLE_BARS / DEFAULT_DISPLAY_ZOOM),
);

/** 与 grid left/right 保持一致，用于估算 category 带宽。 */
const CHART_GRID_LEFT = 52;
const CHART_GRID_RIGHT = 18;
/** 蜡烛实体占格距比例；与 DEFAULT_DISPLAY_ZOOM 解耦，保留间隙避免粘连。 */
const CANDLE_WIDTH_RATIO = 0.62;
const MIN_CANDLE_BAR_WIDTH = 5;
const MAX_CANDLE_BAR_WIDTH = 35;
const DEFAULT_CANDLE_BAR_WIDTH = 11;

const kPeriod = ref<MarketKPeriod>("day");
const subTab = ref<"volume" | "turnover">("volume");
const zoomRange = ref({ start: 0, end: 100 });
const chartRef = ref<HTMLDivElement | null>(null);
let chart: echarts.ECharts | null = null;
let resizeObserver: ResizeObserver | null = null;

const chartBars = computed(() => aggregateMarketBars(props.dailyBars, kPeriod.value));
const sessionStats = computed(() => computeLatestSessionStats(props.dailyBars));
const sessionVolumeDisplay = computed(() => {
  const stats = sessionStats.value;
  const meta = displayMetaResolved.value;
  if (!stats) return { value: 0, label: meta.volumeUnitLabel };
  const value = meta.usesKokuVolume
    ? stats.volumeKoku
    : Math.round(props.dailyBars[props.dailyBars.length - 1]?.volumeGo ?? 0);
  return { value, label: meta.volumeUnitLabel };
});
const isReady = computed(() => props.active !== false && props.dailyBars.length > 0);

function volumeGoToDisplay(go: number): number {
  const meta = displayMetaResolved.value;
  if (meta.usesKokuVolume) return Math.round(goToKoku(go));
  return go;
}

function formatVolumeGoForDisplay(go: number): string {
  const value = volumeGoToDisplay(go);
  if (value <= 0) return "0";
  return value.toLocaleString();
}

function defaultZoomEnd(): number {
  return 100;
}

function defaultZoomStart(barCount: number): number {
  return barCount > DEFAULT_VISIBLE_BARS
    ? 100 - Math.round((DEFAULT_VISIBLE_BARS / barCount) * 100)
    : 0;
}

function countVisibleBars(totalBars: number, startPct: number, endPct: number): number {
  if (totalBars <= 0) return 1;
  const span = Math.max(0, endPct - startPct) / 100;
  return Math.max(1, Math.ceil(totalBars * span));
}

/** 强制奇数像素柱宽，避免 ECharts 偶数宽蜡烛影线左右偏移。 */
function toOddPixelWidth(raw: number): number {
  let width = Math.floor(raw);
  width = Math.max(MIN_CANDLE_BAR_WIDTH, Math.min(MAX_CANDLE_BAR_WIDTH, width));
  if (width % 2 === 0) width -= 1;
  return Math.max(MIN_CANDLE_BAR_WIDTH, width);
}

function resolveChartGridWidth(): number {
  const chartWidth = chart?.getWidth() ?? chartRef.value?.clientWidth ?? 0;
  return Math.max(0, chartWidth - CHART_GRID_LEFT - CHART_GRID_RIGHT);
}

function resolveOddCandleBarWidth(visibleBarCount: number): number {
  const gridWidth = resolveChartGridWidth();
  if (gridWidth <= 0) return DEFAULT_CANDLE_BAR_WIDTH;
  const bandWidth = gridWidth / Math.max(visibleBarCount, 1);
  return toOddPixelWidth(bandWidth * CANDLE_WIDTH_RATIO);
}

function syncDefaultZoomRange() {
  zoomRange.value = {
    start: defaultZoomStart(chartBars.value.length),
    end: defaultZoomEnd(),
  };
}

function computeOddCandleBarWidth(): number {
  const totalBars = chartBars.value.length;
  if (totalBars <= 0) return DEFAULT_CANDLE_BAR_WIDTH;
  const visibleCount = countVisibleBars(
    totalBars,
    zoomRange.value.start,
    zoomRange.value.end,
  );
  return resolveOddCandleBarWidth(visibleCount);
}

function buildSubSeriesData(bars: ReturnType<typeof aggregateMarketBars>) {
  const subValues =
    subTab.value === "volume"
      ? bars.map((b) => volumeGoToDisplay(b.volumeGo))
      : bars.map((b) => Math.round(wenToKan(b.turnoverMoney)));

  const subColors = bars.map((b) => (b.close >= b.open ? UP_COLOR : DOWN_COLOR));
  return subValues.map((value, idx) => ({
    value,
    itemStyle: { color: subColors[idx] },
  }));
}

function buildOption(): echarts.EChartsCoreOption {
  const bars = chartBars.value;
  const labels = bars.map((b) => b.label);
  const meta = displayMetaResolved.value;
  const candleData = bars.map((b) => [
    priceToKanPerKoku(b.open),
    priceToKanPerKoku(b.close),
    priceToKanPerKoku(b.low),
    priceToKanPerKoku(b.high),
  ]);

  const subName =
    subTab.value === "volume"
      ? `成交量（${meta.volumeUnitLabel}）`
      : "成交额（贯）";
  const { start, end } = zoomRange.value;
  const barWidth = resolveOddCandleBarWidth(countVisibleBars(bars.length, start, end));

  return {
    animation: false,
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "cross" },
      formatter(params: unknown) {
        const rows = Array.isArray(params) ? params : [params];
        const first = rows[0] as { dataIndex?: number } | undefined;
        const idx = first?.dataIndex ?? 0;
        const bar = bars[idx];
        if (!bar) return "";
        const lines = [
          labels[idx],
          `开 ${priceToKanPerKoku(bar.open)} ${meta.priceUnitLabel}`,
          `高 ${priceToKanPerKoku(bar.high)} ${meta.priceUnitLabel}`,
          `低 ${priceToKanPerKoku(bar.low)} ${meta.priceUnitLabel}`,
          `收 ${priceToKanPerKoku(bar.close)} ${meta.priceUnitLabel}`,
          `量 ${formatVolumeGoForDisplay(bar.volumeGo)} ${meta.volumeUnitLabel}`,
          `额 ${formatMarketTurnoverWen(bar.turnoverMoney)} 贯`,
        ];
        return lines.join("<br/>");
      },
    },
    axisPointer: { link: [{ xAxisIndex: "all" }] },
    grid: [
      { left: 52, right: 18, top: 12, height: "58%" },
      { left: 52, right: 18, top: "74%", height: "18%" },
    ],
    xAxis: [
      {
        type: "category",
        data: labels,
        boundaryGap: true,
        axisLine: { onZero: false },
        splitLine: { show: false },
        axisLabel: { show: false },
        min: "dataMin",
        max: "dataMax",
      },
      {
        type: "category",
        gridIndex: 1,
        data: labels,
        boundaryGap: true,
        axisLine: { onZero: false },
        axisTick: { show: false },
        splitLine: { show: false },
        axisLabel: { fontSize: 10 },
      },
    ],
    yAxis: [
      {
        scale: true,
        splitArea: { show: true },
        axisLabel: { fontSize: 10 },
        name: meta.priceUnitLabel,
        nameTextStyle: { fontSize: 10 },
      },
      {
        scale: true,
        gridIndex: 1,
        splitNumber: 2,
        axisLabel: { fontSize: 10 },
        name: subName,
        nameTextStyle: { fontSize: 10 },
      },
    ],
    dataZoom: [
      {
        type: "inside",
        xAxisIndex: [0, 1],
        start,
        end,
      },
      {
        show: bars.length > 30,
        xAxisIndex: [0, 1],
        type: "slider",
        bottom: 0,
        height: 18,
        start,
        end,
      },
    ],
    series: [
      {
        type: "candlestick",
        data: candleData,
        barWidth,
        itemStyle: {
          color: UP_COLOR,
          color0: DOWN_COLOR,
          borderColor: UP_COLOR,
          borderColor0: DOWN_COLOR,
        },
      },
      {
        type: "bar",
        xAxisIndex: 1,
        yAxisIndex: 1,
        data: buildSubSeriesData(bars),
        barWidth,
      },
    ],
  };
}

function updateBarWidthsOnly() {
  if (!chart || !isReady.value || chartBars.value.length === 0) return;
  const barWidth = computeOddCandleBarWidth();
  chart.setOption(
    {
      series: [{ barWidth }, { barWidth }],
    },
    false,
  );
}

function captureZoomRange() {
  if (!chart) return;
  const option = chart.getOption();
  const dataZoom = option.dataZoom as Array<{ start?: number; end?: number }> | undefined;
  const primary = dataZoom?.[0];
  if (primary && typeof primary.start === "number" && typeof primary.end === "number") {
    zoomRange.value = { start: primary.start, end: primary.end };
  }
}

function onChartDataZoom() {
  captureZoomRange();
  updateBarWidthsOnly();
}

function ensureChart() {
  if (!chartRef.value || chart) return;
  chart = echarts.init(chartRef.value);
  chart.on("dataZoom", onChartDataZoom);
}

function disposeChart() {
  chart?.off("dataZoom", onChartDataZoom);
  chart?.dispose();
  chart = null;
  zoomRange.value = { start: 0, end: 100 };
  kPeriod.value = "day";
  subTab.value = "volume";
}

function renderFull(resetZoom = true) {
  if (!isReady.value || !chartRef.value) return;
  ensureChart();
  if (!chart) return;
  if (resetZoom) syncDefaultZoomRange();
  chart.setOption(buildOption(), true);
  chart.resize();
  captureZoomRange();
  updateBarWidthsOnly();
}

function updateSubSeriesOnly() {
  if (!chart || !isReady.value || chartBars.value.length === 0) return;
  captureZoomRange();
  const bars = chartBars.value;
  const meta = displayMetaResolved.value;
  const subName =
    subTab.value === "volume"
      ? `成交量（${meta.volumeUnitLabel}）`
      : "成交额（贯）";
  const barWidth = computeOddCandleBarWidth();
  chart.setOption(
    {
      yAxis: [{}, { name: subName }],
      series: [{ barWidth }, { data: buildSubSeriesData(bars), barWidth }],
      dataZoom: [
        { start: zoomRange.value.start, end: zoomRange.value.end },
        { start: zoomRange.value.start, end: zoomRange.value.end },
      ],
    },
    false,
  );
}

function updateChartDataOnly() {
  if (!chart || !isReady.value || chartBars.value.length === 0) return;
  captureZoomRange();
  const bars = chartBars.value;
  const labels = bars.map((b) => b.label);
  const meta = displayMetaResolved.value;
  const candleData = bars.map((b) => [
    priceToKanPerKoku(b.open),
    priceToKanPerKoku(b.close),
    priceToKanPerKoku(b.low),
    priceToKanPerKoku(b.high),
  ]);
  const subName =
    subTab.value === "volume"
      ? `成交量（${meta.volumeUnitLabel}）`
      : "成交额（贯）";
  const barWidth = computeOddCandleBarWidth();
  chart.setOption(
    {
      xAxis: [{ data: labels }, { data: labels }],
      yAxis: [{ name: meta.priceUnitLabel }, { name: subName }],
      series: [
        { data: candleData, barWidth },
        { data: buildSubSeriesData(bars), barWidth },
      ],
      dataZoom: [
        { start: zoomRange.value.start, end: zoomRange.value.end },
        { start: zoomRange.value.start, end: zoomRange.value.end },
      ],
    },
    false,
  );
}

function scheduleRender(resetZoom = false) {
  void nextTick(() => {
    requestAnimationFrame(() => {
      renderFull(resetZoom);
      requestAnimationFrame(() => {
        if (!chart || !isReady.value) return;
        chart.resize();
        captureZoomRange();
        updateBarWidthsOnly();
      });
    });
  });
}

function bindResizeObserver() {
  if (!chartRef.value || resizeObserver) return;
  resizeObserver = new ResizeObserver(() => {
    if (!isReady.value) return;
    chart?.resize();
    updateBarWidthsOnly();
  });
  resizeObserver.observe(chartRef.value);
}

watch(
  isReady,
  (ready) => {
    if (!ready) return;
    bindResizeObserver();
    scheduleRender(true);
  },
  { immediate: true },
);

watch(kPeriod, () => scheduleRender(true));
watch(
  () => props.chartKey,
  () => {
    disposeChart();
    if (isReady.value) scheduleRender(true);
  },
);
watch(
  () => props.dailyBars,
  () => updateChartDataOnly(),
  { deep: true },
);
watch(() => props.refreshToken, () => updateChartDataOnly());
watch(displayMetaResolved, () => scheduleRender(true));
watch(subTab, updateSubSeriesOnly);

onBeforeUnmount(() => {
  disposeChart();
  resizeObserver?.disconnect();
  resizeObserver = null;
});
</script>

<template>
  <div class="market-echarts-panel">
    <div class="chart-toolbar">
      <div class="chart-toolbar__tabs">
        <div class="chart-tab-row">
          <button
            v-for="tab in K_PERIOD_TABS"
            :key="tab.key"
            type="button"
            class="chart-tab"
            :class="{ active: kPeriod === tab.key }"
            @click="kPeriod = tab.key"
          >
            {{ tab.label }}
          </button>
        </div>

        <div class="chart-tab-row chart-tab-row--sub">
          <button
            type="button"
            class="chart-tab"
            :class="{ active: subTab === 'volume' }"
            @click="subTab = 'volume'"
          >
            成交量
          </button>
          <button
            type="button"
            class="chart-tab"
            :class="{ active: subTab === 'turnover' }"
            @click="subTab = 'turnover'"
          >
            成交额
          </button>
        </div>
      </div>

      <div v-if="sessionStats" class="chart-toolbar__stats">
        <div class="session-stats-title">{{ sessionStats.dateLabel }} 行情</div>
        <div class="session-stats-grid">
          <div class="stat-item">
            <span class="stat-label">现价</span>
            <span
              class="stat-value"
              :class="sessionPriceTrendClass(sessionStats.current, sessionStats.prevClose)"
            >
              {{ sessionStats.current }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">涨跌</span>
            <span class="stat-value" :class="sessionSignedTrendClass(sessionStats.change)">
              {{ formatSignedNumber(sessionStats.change) }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">涨幅</span>
            <span class="stat-value" :class="sessionSignedTrendClass(sessionStats.changePct)">
              {{ formatSignedPercent(sessionStats.changePct) }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">振幅</span>
            <span class="stat-value">{{ sessionStats.amplitudePct.toFixed(2) }}%</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">开盘</span>
            <span
              class="stat-value"
              :class="sessionPriceTrendClass(sessionStats.open, sessionStats.prevClose)"
            >
              {{ sessionStats.open }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">最高</span>
            <span
              class="stat-value"
              :class="sessionPriceTrendClass(sessionStats.high, sessionStats.prevClose)"
            >
              {{ sessionStats.high }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">最低</span>
            <span
              class="stat-value"
              :class="sessionPriceTrendClass(sessionStats.low, sessionStats.prevClose)"
            >
              {{ sessionStats.low }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">昨收</span>
            <span class="stat-value market-stat--flat">{{ sessionStats.prevClose }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">总量</span>
            <span class="stat-value">
              {{ sessionVolumeDisplay.value.toLocaleString() }} {{ sessionVolumeDisplay.label }}
            </span>
          </div>
          <div class="stat-item">
            <span class="stat-label">金额</span>
            <span class="stat-value">{{ sessionStats.turnoverKan.toLocaleString() }} 贯</span>
          </div>
        </div>
      </div>
    </div>

    <div ref="chartRef" class="market-echarts-panel__chart" />
    <el-empty v-if="!dailyBars.length" class="market-echarts-panel__empty" description="暂无 K 线数据" :image-size="64" />
  </div>
</template>

<style scoped>
.market-echarts-panel {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-height: 420px;
  position: relative;
}

.chart-toolbar {
  display: flex;
  gap: 12px;
  align-items: stretch;
}

.chart-toolbar__tabs {
  flex: 0 0 auto;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.chart-toolbar__stats {
  flex: 1;
  min-width: 0;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 6px 8px;
  background: var(--el-fill-color-blank);
}

.session-stats-title {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-bottom: 6px;
}

.session-stats-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  column-gap: 16px;
  row-gap: 4px;
  font-size: 11px;
}

.stat-item {
  display: grid;
  grid-template-columns: 3.2em 1fr;
  column-gap: 10px;
  align-items: baseline;
  min-width: 0;
}

.stat-label {
  color: var(--el-text-color-secondary);
  font-weight: 500;
  white-space: nowrap;
}

.stat-value {
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.market-stat--up {
  color: #dc2626;
}

.market-stat--down {
  color: #16a34a;
}

.market-stat--flat {
  color: var(--el-text-color-regular);
}

.chart-tab-row {
  display: flex;
  gap: 6px;
}

.chart-tab-row--sub {
  margin-bottom: 2px;
}

.chart-tab {
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-blank);
  border-radius: 4px;
  padding: 4px 10px;
  font-size: 12px;
  cursor: pointer;
}

.chart-tab.active {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}

.market-echarts-panel__chart {
  width: 100%;
  height: 380px;
  flex: 1;
}

.market-echarts-panel__empty {
  position: absolute;
  inset: 88px 0 0;
  pointer-events: none;
}
</style>
