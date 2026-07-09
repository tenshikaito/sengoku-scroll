import type { StrategyMapLandmarkState, StrategyMapState } from "@/api/strategyTypes";

/** mini_kanto 静态地图元数据（地形/区域/地标），供 Mock 与 API 响应补全共用。 */
export function buildMiniKantoMapTiles(width: number, height: number): Pick<
  StrategyMapState,
  "tileTerrainNames" | "tileRegionNames" | "landmarks"
> {
  const length = width * height;
  return {
    tileTerrainNames: Array.from({ length }, () => "平地"),
    tileRegionNames: Array.from({ length }, (_, i) => {
      const x = i % width;
      if (x <= 3) return "尾张";
      if (x <= 6) return "三河";
      return "骏河";
    }),
    landmarks: [
      { id: 1, name: "热田神宫", x: 3, y: 6 },
      { id: 2, name: "富士山", x: 8, y: 8 },
      { id: 3, name: "桶狭间", x: 2, y: 5 },
    ],
  };
}

export function mapTileDataComplete(map: StrategyMapState): boolean {
  const expected = map.width * map.height;
  const hasTerrain =
    Array.isArray(map.tileTerrainNames) && map.tileTerrainNames.length === expected;
  const hasRegion =
    Array.isArray(map.tileRegionNames) &&
    map.tileRegionNames.length === expected &&
    map.tileRegionNames.some((name) => Boolean(name));
  const hasLandmarks = Array.isArray(map.landmarks) && map.landmarks.length > 0;
  return hasTerrain && hasRegion && hasLandmarks;
}

export function mergeLandmarkLists(
  existing: StrategyMapLandmarkState[] | undefined,
  defaults: StrategyMapLandmarkState[]
): StrategyMapLandmarkState[] {
  const byId = new Map<number, StrategyMapLandmarkState>();
  for (const lm of existing ?? []) byId.set(lm.id, lm);
  for (const lm of defaults) byId.set(lm.id, lm);
  return [...byId.values()].sort((a, b) => a.id - b.id);
}

/** 旧存档 / 旧 API 响应缺少地图元数据时，按剧本补全。 */
export function enrichStrategyMapState(
  scenarioId: string,
  map: StrategyMapState
): StrategyMapState {
  if (scenarioId !== "mini_kanto") return map;

  const defaults = buildMiniKantoMapTiles(map.width, map.height);
  const expected = map.width * map.height;

  const merged: StrategyMapState = {
    ...map,
    tileTerrainNames:
      map.tileTerrainNames?.length === expected
        ? map.tileTerrainNames
        : defaults.tileTerrainNames,
    tileRegionNames:
      map.tileRegionNames?.length === expected &&
      map.tileRegionNames.some((name) => Boolean(name))
        ? map.tileRegionNames
        : defaults.tileRegionNames,
    landmarks: mergeLandmarkLists(map.landmarks, defaults.landmarks),
  };

  if (mapTileDataComplete(map)) return merged;
  return merged;
}
