import type { StrategyMapLandmarkState, StrategyMapState } from "@/api/strategyTypes";

export interface MapTileInfo {
  terrainName: string | null;
  regionName: string | null;
}

export function mapTileIndex(map: StrategyMapState, x: number, y: number): number {
  return y * map.width + x;
}

export function mapTileInfo(map: StrategyMapState, x: number, y: number): MapTileInfo {
  if (x < 0 || y < 0 || x >= map.width || y >= map.height) {
    return { terrainName: null, regionName: null };
  }

  const index = mapTileIndex(map, x, y);
  return {
    terrainName: map.tileTerrainNames?.[index] ?? null,
    regionName: map.tileRegionNames?.[index] ?? null,
  };
}

export function landmarkAtCell(
  map: StrategyMapState,
  x: number,
  y: number
): StrategyMapLandmarkState | null {
  return map.landmarks?.find((lm) => lm.x === x && lm.y === y) ?? null;
}
