import type { StrategyMarketDailyBar, StrategyMarketDepthLevel } from "@/api/strategy";
import { GO_PER_KOKU, MONEY_PER_KAN } from "@/utils/strategyDisplayUnits";

export type MarketKPeriod = "day" | "week" | "month" | "year";

export interface MarketChartBar {
  label: string;
  year: number;
  month: number;
  day: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volumeGo: number;
  turnoverMoney: number;
}

/** 文/合 → 贯/石（数值 1:1）。 */
export function priceToKanPerKoku(wenPerGo: number): number {
  return wenPerGo;
}

export function formatMarketPrice(wenPerGo: number): string {
  if (wenPerGo <= 0) return "";
  return String(priceToKanPerKoku(wenPerGo));
}

/** 合 → 石。 */
export function goToKoku(go: number): number {
  return go / GO_PER_KOKU;
}

export function formatMarketVolumeGo(go: number): string {
  if (go <= 0) return "";
  const koku = Math.round(goToKoku(go));
  if (koku <= 0) return "";
  return koku.toLocaleString();
}

/** 文 → 贯。 */
export function wenToKan(wen: number): number {
  return wen / MONEY_PER_KAN;
}

export function formatMarketTurnoverWen(wen: number): string {
  if (wen <= 0) return "";
  const kan = Math.round(wenToKan(wen));
  if (kan <= 0) return "";
  return kan.toLocaleString();
}

function aggregateBucket(daily: StrategyMarketDailyBar[]): MarketChartBar {
  const first = daily[0];
  const last = daily[daily.length - 1];
  return {
    label: "",
    year: last.year,
    month: last.month,
    day: last.day,
    open: first.open,
    high: Math.max(...daily.map((b) => b.high)),
    low: Math.min(...daily.map((b) => b.low)),
    close: last.close,
    volumeGo: daily.reduce((s, b) => s + b.volumeGo, 0),
    turnoverMoney: daily.reduce((s, b) => s + b.turnoverMoney, 0),
  };
}

function monthKey(bar: StrategyMarketDailyBar): string {
  return `${bar.year}-${bar.month}`;
}

function yearKey(bar: StrategyMarketDailyBar): string {
  return String(bar.year);
}

export function aggregateMarketBars(
  dailyBars: StrategyMarketDailyBar[],
  period: MarketKPeriod,
): MarketChartBar[] {
  if (dailyBars.length === 0) return [];
  if (period === "day") {
    return dailyBars.map((bar) => ({
      ...bar,
      label: `${bar.month}/${bar.day}`,
    }));
  }

  if (period === "week") {
    const buckets: StrategyMarketDailyBar[][] = [];
    for (let i = 0; i < dailyBars.length; i += 7) {
      buckets.push(dailyBars.slice(i, i + 7));
    }
    return buckets.map((bucket, idx) => {
      const bar = aggregateBucket(bucket);
      bar.label = `W${idx + 1}`;
      return bar;
    });
  }

  if (period === "month") {
    const groups = new Map<string, StrategyMarketDailyBar[]>();
    for (const bar of dailyBars) {
      const key = monthKey(bar);
      const list = groups.get(key) ?? [];
      list.push(bar);
      groups.set(key, list);
    }
    return [...groups.values()].map((bucket) => {
      const bar = aggregateBucket(bucket);
      bar.label = `${bar.month}月`;
      return bar;
    });
  }

  const groups = new Map<string, StrategyMarketDailyBar[]>();
  for (const bar of dailyBars) {
    const key = yearKey(bar);
    const list = groups.get(key) ?? [];
    list.push(bar);
    groups.set(key, list);
  }
  return [...groups.values()].map((bucket) => {
    const bar = aggregateBucket(bucket);
    bar.label = `${bar.year}年`;
    return bar;
  });
}

export function depthRowPriceClass(price: number, lastClose: number): string {
  if (price <= 0) return "";
  if (price === lastClose) return "depth-row--close";
  if (price > lastClose) return "depth-row--above";
  return "depth-row--below";
}

function padDepthLevels(
  levels: StrategyMarketDepthLevel[],
  count: number,
): StrategyMarketDepthLevel[] {
  const rows = [...levels];
  while (rows.length < count) {
    rows.push({ priceMoneyPerGo: 0, quantityGo: 0 });
  }
  return rows.slice(0, count);
}

/** 卖盘：仅高于收盘价；价高在上，卖一靠近中间。 */
export function buildAskDepthRows(
  askLevels: StrategyMarketDepthLevel[],
  lastClose: number,
  count: number,
): StrategyMarketDepthLevel[] {
  const above = askLevels
    .filter((l) => l.priceMoneyPerGo > lastClose && l.priceMoneyPerGo > 0)
    .sort((a, b) => a.priceMoneyPerGo - b.priceMoneyPerGo);
  const nearest = above.slice(-count).reverse();
  return padDepthLevels(nearest, count);
}

/** 买盘：价不高于会话现价的买单在分割线下方展示（同价归分割线）；价高在上，买一靠近中间。 */
export function buildBidDepthRows(
  bidLevels: StrategyMarketDepthLevel[],
  sessionPrice: number,
  count: number,
): StrategyMarketDepthLevel[] {
  const bids = bidLevels
    .filter(
      (l) =>
        l.priceMoneyPerGo > 0
        && (sessionPrice <= 0 || l.priceMoneyPerGo !== sessionPrice),
    )
    .sort((a, b) => b.priceMoneyPerGo - a.priceMoneyPerGo);
  return padDepthLevels(bids.slice(0, count), count);
}

/** 合并 K 线与快照的会话现价（用于盘口中线与报价表）。 */
export function resolveSessionPrice(
  dailyBars: StrategyMarketDailyBar[],
  snapshotLastClose: number,
): number {
  const barClose = dailyBars.length > 0 ? dailyBars[dailyBars.length - 1].close : 0;
  if (barClose > 0) return barClose;
  return snapshotLastClose > 0 ? snapshotLastClose : 0;
}

/** 低于收盘价的挂卖单（限价卖单），展示在买盘下方。 */
export function buildBelowCloseAskRows(
  askLevels: StrategyMarketDepthLevel[],
  lastClose: number,
  count: number,
): StrategyMarketDepthLevel[] {
  const rows = askLevels
    .filter((l) => l.priceMoneyPerGo > 0 && l.priceMoneyPerGo < lastClose)
    .sort((a, b) => b.priceMoneyPerGo - a.priceMoneyPerGo);
  return rows.slice(0, count);
}

/** 高于收盘价的挂买单（限价买单），展示在卖盘上方。 */
export function buildAboveCloseBidRows(
  bidLevels: StrategyMarketDepthLevel[],
  lastClose: number,
  count: number,
): StrategyMarketDepthLevel[] {
  const rows = bidLevels
    .filter((l) => l.priceMoneyPerGo > lastClose && l.priceMoneyPerGo > 0)
    .sort((a, b) => a.priceMoneyPerGo - b.priceMoneyPerGo);
  return rows.slice(0, count);
}

export interface MarketSessionStats {
  dateLabel: string;
  current: number;
  open: number;
  high: number;
  low: number;
  prevClose: number;
  change: number;
  changePct: number;
  amplitudePct: number;
  volumeKoku: number;
  turnoverKan: number;
}

/** 最新一日行情摘要（用于图表旁报价表）。 */
export function computeLatestSessionStats(
  dailyBars: StrategyMarketDailyBar[],
): MarketSessionStats | null {
  if (dailyBars.length === 0) return null;

  const latest = dailyBars[dailyBars.length - 1];
  const prevBar = dailyBars.length > 1 ? dailyBars[dailyBars.length - 2] : latest;
  const prevClose = prevBar.close;
  const change = latest.close - prevClose;
  const changePct = prevClose > 0 ? (change / prevClose) * 100 : 0;
  const amplitudePct =
    prevClose > 0 ? ((latest.high - latest.low) / prevClose) * 100 : 0;

  return {
    dateLabel: `${latest.month}/${latest.day}`,
    current: priceToKanPerKoku(latest.close),
    open: priceToKanPerKoku(latest.open),
    high: priceToKanPerKoku(latest.high),
    low: priceToKanPerKoku(latest.low),
    prevClose: priceToKanPerKoku(prevClose),
    change: priceToKanPerKoku(change),
    changePct,
    amplitudePct,
    volumeKoku: Math.round(goToKoku(latest.volumeGo)),
    turnoverKan: Math.round(wenToKan(latest.turnoverMoney)),
  };
}

export function sessionPriceTrendClass(value: number, base: number): string {
  if (value > base) return "market-stat--up";
  if (value < base) return "market-stat--down";
  return "market-stat--flat";
}

export function sessionSignedTrendClass(value: number): string {
  if (value > 0) return "market-stat--up";
  if (value < 0) return "market-stat--down";
  return "market-stat--flat";
}

export function formatSignedNumber(value: number, fractionDigits = 0): string {
  if (!Number.isFinite(value)) return "—";
  const rounded =
    fractionDigits > 0 ? value.toFixed(fractionDigits) : String(Math.round(value));
  if (value > 0) return `+${rounded}`;
  return rounded;
}

export function formatSignedPercent(value: number): string {
  if (!Number.isFinite(value)) return "—";
  return `${formatSignedNumber(value, 2)}%`;
}
