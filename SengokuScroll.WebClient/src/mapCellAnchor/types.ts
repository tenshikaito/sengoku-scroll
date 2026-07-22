export interface PanelRect {
  left: number;
  top: number;
  width: number;
  height: number;
}

export interface PopupSize {
  width: number;
  height: number;
}

export type AnchorSide = "right" | "left" | "bottom" | "top";

/** 左右锚定时，悬浮框与格块的竖向对齐方式。 */
export type AnchorVerticalAlign = "start" | "end";

export interface AnchoredPosition {
  left: number;
  top: number;
}

export interface AnchoredPlacement extends AnchoredPosition {
  side: AnchorSide;
  /** 左右锚定时：start=顶对齐格块，end=底对齐格块。 */
  verticalAlign: AnchorVerticalAlign;
  /** 未经视口修正的原始 top（调试用）。 */
  rawTop: number;
}
