import type { StrategyMarketDepthLevel, StrategyMarketSnapshot } from "@/api/strategy";

/** 卖盘：后端价高在上；UI 仅取靠近中线的 N 档（数组末尾）。 */
export function sliceAskRowsForDisplay(
  levels: StrategyMarketDepthLevel[],
  count: number,
  sessionPrice = 0,
  closeLevelQuantityGo = 0,
): StrategyMarketDepthLevel[] {
  let active = levels.filter((l) => l.priceMoneyPerGo > 0);
  if (closeLevelQuantityGo <= 0 && sessionPrice > 0) {
    active = active.filter((l) => l.priceMoneyPerGo !== sessionPrice);
  }
  if (active.length <= count) return active;
  return active.slice(-count);
}

/** 买盘：后端价高在上；UI 取顶部 N 档（买一靠中线）。 */
export function sliceBidRowsForDisplay(
  levels: StrategyMarketDepthLevel[],
  count: number,
  sessionPrice = 0,
  closeLevelQuantityGo = 0,
): StrategyMarketDepthLevel[] {
  let active = levels.filter((l) => l.priceMoneyPerGo > 0);
  if (closeLevelQuantityGo <= 0 && sessionPrice > 0) {
    active = active.filter((l) => l.priceMoneyPerGo !== sessionPrice);
  }
  return active.slice(0, count);
}

const EMPTY_DEPTH_LEVEL: StrategyMarketDepthLevel = {
  priceMoneyPerGo: 0,
  quantityGo: 0,
};

function cloneEmptyDepthLevel(): StrategyMarketDepthLevel {
  return { ...EMPTY_DEPTH_LEVEL };
}

/** 卖盘 UI 固定 N 行：不足时在远离中线一侧（上方）补空行。 */
export function padAskRowsForDisplay(
  rows: StrategyMarketDepthLevel[],
  count: number,
): StrategyMarketDepthLevel[] {
  if (count <= 0) return [];
  const trimmed = rows.slice(-count);
  if (trimmed.length >= count) return trimmed;

  const padding = Array.from({ length: count - trimmed.length }, cloneEmptyDepthLevel);
  return [...padding, ...trimmed];
}

/** 买盘 UI 固定 N 行：不足时在远离中线一侧（下方）补空行。 */
export function padBidRowsForDisplay(
  rows: StrategyMarketDepthLevel[],
  count: number,
): StrategyMarketDepthLevel[] {
  if (count <= 0) return [];
  const trimmed = rows.slice(0, count);
  if (trimmed.length >= count) return trimmed;

  const padding = Array.from({ length: count - trimmed.length }, cloneEmptyDepthLevel);
  return [...trimmed, ...padding];
}

function activeBookLevels(
  levels: StrategyMarketDepthLevel[],
  sessionPrice = 0,
  closeLevelQuantityGo = 0,
): StrategyMarketDepthLevel[] {
  let active = levels.filter((l) => l.priceMoneyPerGo > 0);
  if (closeLevelQuantityGo <= 0 && sessionPrice > 0) {
    active = active.filter((l) => l.priceMoneyPerGo !== sessionPrice);
  }
  return active;
}

/** 限价买入：累计卖盘价 ≤ limitPrice 的全部挂单量（合）。 */
export function sumAskVolumeUpToPrice(
  levels: StrategyMarketDepthLevel[],
  limitPrice: number,
  sessionPrice = 0,
  closeLevelQuantityGo = 0,
): number {
  if (limitPrice <= 0) return 0;
  return activeBookLevels(levels, sessionPrice, closeLevelQuantityGo)
    .filter((l) => l.priceMoneyPerGo <= limitPrice)
    .reduce((sum, l) => sum + l.quantityGo, 0);
}

/** 限价卖出：累计买盘价 ≥ limitPrice 的全部挂单量（合）。 */
export function sumBidVolumeFromPrice(
  levels: StrategyMarketDepthLevel[],
  limitPrice: number,
  sessionPrice = 0,
  closeLevelQuantityGo = 0,
): number {
  if (limitPrice <= 0) return 0;
  return activeBookLevels(levels, sessionPrice, closeLevelQuantityGo)
    .filter((l) => l.priceMoneyPerGo >= limitPrice)
    .reduce((sum, l) => sum + l.quantityGo, 0);
}

function formatLevelRows(levels: StrategyMarketDepthLevel[]): string {
  return levels
    .filter((l) => l.priceMoneyPerGo > 0)
    .map((l) => `${l.priceMoneyPerGo}@${l.quantityGo}`)
    .join(", ");
}

/** 开发态：输出盘口快照与 UI 裁剪结果，便于对照 DOM。 */
export function logMarketBookSnapshot(
  tag: string,
  snapshot: StrategyMarketSnapshot,
  depthCount: number,
): void {
  if (!import.meta.env.DEV) return;

  const asksRaw = snapshot.askLevels ?? [];
  const bidsRaw = snapshot.bidLevels ?? [];
  const asksUi = padAskRowsForDisplay(
    sliceAskRowsForDisplay(
      asksRaw,
      depthCount,
      snapshot.sessionPriceMoneyPerGo,
      snapshot.closeLevelQuantityGo,
    ),
    depthCount,
  );
  const bidsUi = padBidRowsForDisplay(
    sliceBidRowsForDisplay(
      bidsRaw,
      depthCount,
      snapshot.sessionPriceMoneyPerGo,
      snapshot.closeLevelQuantityGo,
    ),
    depthCount,
  );

  console.groupCollapsed(`[market-book] ${tag} · ${snapshot.strongholdName} (#${snapshot.strongholdId})`);
  console.log(
    "quote",
    snapshot.sessionPriceMoneyPerGo,
    "side",
    snapshot.bookQuoteSide,
    "bestBid",
    snapshot.bestBidPriceMoneyPerGo,
    "bestAsk",
    snapshot.bestAskPriceMoneyPerGo,
    "closeQty",
    snapshot.closeLevelQuantityGo,
  );
  console.log("ask raw", formatLevelRows(asksRaw), `(rows=${asksRaw.length})`);
  console.log("ask ui", formatLevelRows(asksUi), `(depth=${depthCount})`);
  console.log("bid raw", formatLevelRows(bidsRaw), `(rows=${bidsRaw.length})`);
  console.log("bid ui", formatLevelRows(bidsUi), `(depth=${depthCount})`);
  console.groupEnd();
}
