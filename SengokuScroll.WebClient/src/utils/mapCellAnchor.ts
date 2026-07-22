import { placementForAnchorSide } from "@/mapCellAnchor/AnchorSidePlacementBehavior";
import type {
  AnchorSide,
  AnchoredPlacement,
  AnchoredPosition,
  AnchorVerticalAlign,
  PanelRect,
  PopupSize,
} from "@/mapCellAnchor/types";

export type {
  AnchorSide,
  AnchorVerticalAlign,
  AnchoredPlacement,
  AnchoredPosition,
  PanelRect,
  PopupSize,
} from "@/mapCellAnchor/types";

/**
 * 将悬浮框锚定在格块旁。
 * @param referenceRect 用于判断格块在可视区域的方位（通常为地图区域）；省略时用 panel。
 */
export function resolveAnchoredPanelPlacement(
  cell: PanelRect,
  panel: PanelRect,
  popup: PopupSize,
  gap = 4,
  referenceRect?: PanelRect
): AnchoredPlacement {
  const ref = referenceRect ?? panel;
  const order = preferredSideOrder(cell, ref);

  let best: AnchoredPlacement | null = null;
  let bestScore = Number.POSITIVE_INFINITY;

  for (const side of order) {
    const finalized = finalizePlacement(cell, panel, popup, gap, side);
    const raw = placementForSide(cell, popup, gap, side);
    const overflow = measureOverflow(finalized.left, finalized.top, popup, panel);
    const shift = measureShiftPenalty(side, raw, finalized);
    const orderIdx = order.indexOf(side);
    const horizontalBonus = side === "left" || side === "right" ? -1000 : 0;
    const score = overflow * 10_000 + shift * 100 + orderIdx + horizontalBonus;
    if (score < bestScore) {
      bestScore = score;
      best = finalized;
    }
    if (overflow === 0 && shift === 0 && orderIdx === 0) break;
  }

  return best ?? finalizePlacement(cell, panel, popup, gap, order[0]!);
}

/** 固定锚定方向，仅按实测尺寸重算坐标（避免双框宽度变化导致左右侧偏移）。 */
export function resolveAnchoredPanelPlacementForSide(
  cell: PanelRect,
  panel: PanelRect,
  popup: PopupSize,
  side: AnchorSide,
  gap = 4
): AnchoredPlacement {
  return finalizePlacement(cell, panel, popup, gap, side);
}

/** @deprecated 使用 resolveAnchoredPanelPlacement 获取 side。 */
export function resolveAnchoredPanelPosition(
  cell: PanelRect,
  panel: PanelRect,
  popup: PopupSize,
  gap = 4
): AnchoredPosition {
  const { left, top } = resolveAnchoredPanelPlacement(cell, panel, popup, gap);
  return { left, top };
}

function placementForSide(
  cell: PanelRect,
  popup: PopupSize,
  gap: number,
  side: AnchorSide
): { left: number; top: number } {
  return placementForAnchorSide(cell, popup, gap, side);
}

function finalizePlacement(
  cell: PanelRect,
  panel: PanelRect,
  popup: PopupSize,
  gap: number,
  side: AnchorSide
): AnchoredPlacement {
  const raw = placementForSide(cell, popup, gap, side);
  let left = raw.left;
  let top = raw.top;
  let verticalAlign: AnchorVerticalAlign = "start";

  if (side === "left" || side === "right") {
    // 左右锚定：默认与格块顶对齐；若底部超出视口则改为底对齐格块，避免整体上移产生空隙
    if (top + popup.height > panel.height) {
      top = cell.top + cell.height - popup.height;
      verticalAlign = "end";
    }
    if (top < 0) {
      top = 0;
      verticalAlign = "start";
    }
    left = clamp(left, 0, Math.max(0, panel.width - popup.width));
  } else {
    left = clamp(left, 0, Math.max(0, panel.width - popup.width));
    top = clamp(top, 0, Math.max(0, panel.height - popup.height));
  }

  return {
    side,
    left,
    top,
    rawTop: raw.top,
    verticalAlign,
  };
}

/**
 * 按格块相对参考区域四方向的剩余空间排序锚定侧。
 * 右下区域格块 → 左侧空间最大 → 优先 left。
 */
function preferredSideOrder(cell: PanelRect, ref: PanelRect): AnchorSide[] {
  const cellRight = cell.left + cell.width;
  const cellBottom = cell.top + cell.height;
  const refRight = ref.left + ref.width;
  const refBottom = ref.top + ref.height;

  const ranked: { side: AnchorSide; slack: number }[] = [
    { side: "right", slack: refRight - cellRight },
    { side: "left", slack: cell.left - ref.left },
    { side: "bottom", slack: refBottom - cellBottom },
    { side: "top", slack: cell.top - ref.top },
  ];

  ranked.sort((a, b) => b.slack - a.slack);
  return ranked.map((item) => item.side);
}

/** 计算位置修正惩罚；左右底对齐不计竖向惩罚，上下大幅水平偏移加重惩罚。 */
function measureShiftPenalty(
  side: AnchorSide,
  raw: { left: number; top: number },
  finalized: { left: number; top: number; verticalAlign: AnchorVerticalAlign }
): number {
  const hShift = Math.abs(finalized.left - raw.left);
  const vShift = Math.abs(finalized.top - raw.top);

  if (side === "left" || side === "right") {
    if (finalized.verticalAlign === "end") {
      return hShift;
    }
    return hShift + vShift;
  }

  // 上下锚定若水平方向被 clamp 推开，说明并不真正适合该侧
  return hShift + vShift + (hShift > 48 ? hShift * 2 : 0);
}

function measureOverflow(
  left: number,
  top: number,
  popup: PopupSize,
  panel: PanelRect
): number {
  return (
    Math.max(0, -left) +
    Math.max(0, left + popup.width - panel.width) +
    Math.max(0, -top) +
    Math.max(0, top + popup.height - panel.height)
  );
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max);
}
