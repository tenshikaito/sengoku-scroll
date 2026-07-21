import type { StrategyMapMasterState } from "@/api/strategyTypes";

export interface MapTileInfo {
  terrainName: string | null;
  regionName: string | null;
}

export function mapTileIndex(map: Pick<StrategyMapMasterState, "width">, x: number, y: number): number {
  return y * map.width + x;
}

export function mapTileInfo(
  mapMaster: StrategyMapMasterState,
  x: number,
  y: number
): MapTileInfo {
  if (x < 0 || y < 0 || x >= mapMaster.width || y >= mapMaster.height) {
    return { terrainName: null, regionName: null };
  }

  const index = mapTileIndex(mapMaster, x, y);
  const terrainId = mapMaster.terrainIds[index] ?? 0;
  const regionId = mapMaster.regionIds[index] ?? 0;

  const terrain = mapMaster.terrains.find((t) => t.id === terrainId);
  const region = regionId > 0 ? mapMaster.regions.find((r) => r.id === regionId) : undefined;

  return {
    terrainName: terrain?.name ?? (terrainId > 0 ? `地形#${terrainId}` : null),
    regionName: region?.name ?? null,
  };
}

export function landmarkAtCell(
  mapMaster: StrategyMapMasterState,
  x: number,
  y: number
) {
  return mapMaster.landmarks?.find((lm) => lm.x === x && lm.y === y) ?? null;
}

export function roadAtCell(
  mapMaster: StrategyMapMasterState,
  x: number,
  y: number
) {
  return mapMaster.roadCells?.find((r) => r.x === x && r.y === y) ?? null;
}
