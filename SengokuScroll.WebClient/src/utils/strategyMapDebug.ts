/** 开发环境下输出地图坐标/锚点调试日志。 */
const ENABLED = import.meta.env.DEV;

export function logStrategyMapCoords(
  event: string,
  payload: Record<string, unknown>
): void {
  if (!ENABLED) return;
  console.debug("[StrategyMapCoords]", event, payload);
}

export interface ScreenRectDebug {
  left: number;
  top: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
}

export interface HoverIntelLayoutDebugPayload {
  gridCell: { x: number; y: number };
  viewport: { width: number; height: number };
  mouse: { x: number; y: number } | null;
  cellScreen: ScreenRectDebug;
  mapPanelScreen: ScreenRectDebug;
  anchorSide: string;
  verticalAlign: string;
  rawTop: number;
  containerScreen: ScreenRectDebug | null;
  strongholdBoxScreen: ScreenRectDebug | null;
  otherBoxScreen: ScreenRectDebug | null;
  singleBoxScreen: ScreenRectDebug | null;
  placement: { left: number; top: number; side: string };
  popupSize: { width: number; height: number };
  dualLayout: boolean;
}

export function rectToScreenDebug(el: Element | null | undefined): ScreenRectDebug | null {
  if (!el) return null;
  const r = el.getBoundingClientRect();
  return {
    left: Math.round(r.left),
    top: Math.round(r.top),
    right: Math.round(r.right),
    bottom: Math.round(r.bottom),
    width: Math.round(r.width),
    height: Math.round(r.height),
  };
}

/** 悬浮情报框显示时的完整布局日志（供定位问题分析）。 */
export function logHoverIntelLayoutDebug(payload: HoverIntelLayoutDebugPayload): void {
  if (!ENABLED) return;

  const fmt = (label: string, rect: ScreenRectDebug | null) =>
    rect
      ? `${label}(L${rect.left},T${rect.top},R${rect.right},B${rect.bottom},W${rect.width},H${rect.height})`
      : `${label}=null`;

  const lines = [
    "======== HoverIntelDebug ========",
    `屏幕(浏览器视口): ${payload.viewport.width} x ${payload.viewport.height}`,
    `鼠标屏幕坐标: ${
      payload.mouse ? `(${payload.mouse.x}, ${payload.mouse.y})` : "—"
    }`,
    `地图格子(逻辑): (${payload.gridCell.x}, ${payload.gridCell.y})`,
    fmt("格子屏幕区域", payload.cellScreen),
    fmt("地图面板屏幕区域", payload.mapPanelScreen),
    `锚定方向: ${payload.anchorSide} | 竖向对齐: ${payload.verticalAlign} | 原始top: ${payload.rawTop}`,
    `容器估算尺寸: ${payload.popupSize.width} x ${payload.popupSize.height}`,
    `容器定位(fixed): left=${payload.placement.left}px top=${payload.placement.top}px`,
    fmt("悬浮层容器", payload.containerScreen),
    payload.dualLayout
      ? [
          fmt("据点悬浮框", payload.strongholdBoxScreen),
          fmt("单位/后勤悬浮框", payload.otherBoxScreen),
        ].join("\n")
      : fmt("单悬浮框", payload.singleBoxScreen),
    "=================================",
  ];

  console.info(lines.join("\n"));
  console.info("[HoverIntelDebug:json]", payload);
}
