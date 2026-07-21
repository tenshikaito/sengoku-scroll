/** 主地图视口在世界像素坐标系中的矩形（与 StrategyMapCanvas TILE_SIZE 对齐）。 */
export interface MapViewportWorldRect {
  x: number;
  y: number;
  width: number;
  height: number;
  mapWidthPx: number;
  mapHeightPx: number;
}

export interface MinimapNavigatePayload {
  worldX: number;
  worldY: number;
}
