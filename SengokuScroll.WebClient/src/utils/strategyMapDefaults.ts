import type {
  StrategyMapLandmarkState,
  StrategyMapMasterState,
  StrategyRoadCellState,
} from "@/api/strategyTypes";

function miniKantoTerrainId(x: number, y: number): number {
  if (y >= 18 || (x >= 17 && y >= 16)) return 4;
  if (14 <= x && x <= 18 && 14 <= y && y <= 17) return 5;
  if (x === 16 && y === 16) return 5;
  if ((y === 8 || y === 12) && x >= 2 && x <= 16) return 1;
  if (7 <= x && x <= 13 && (x + y) % 5 === 0) return 2;
  if (x <= 6 && y <= 3) return 3;
  return 1;
}

function miniKantoRegionId(x: number): number {
  if (x <= 6) return 1;
  if (x <= 13) return 2;
  return 3;
}

function buildRoadCells(): StrategyRoadCellState[] {
  const cells: StrategyRoadCellState[] = [];
  for (let x = 2; x <= 10; x++) {
    cells.push({
      x,
      y: 8,
      typeId: 1,
      typeName: "官道",
      level: 1,
      speedBonus: 1,
      movementCost: 1,
    });
  }
  for (let y = 4; y <= 8; y++) {
    cells.push({
      x: 8,
      y,
      typeId: 1,
      typeName: "官道",
      level: 1,
      speedBonus: 1,
      movementCost: 1,
    });
  }
  return cells;
}

const MINI_KANTO_LANDMARKS: StrategyMapLandmarkState[] = [
  { id: 1, name: "热田神宫", x: 6, y: 12 },
  { id: 2, name: "富士山", x: 16, y: 16 },
  { id: 3, name: "桶狭间", x: 4, y: 10 },
];

/** mini_kanto 地图静态主数据（Mock / Live 回退共用）。 */
export function buildMiniKantoMapMaster(
  width = 20,
  height = 20
): StrategyMapMasterState {
  const terrainIds: number[] = [];
  const regionIds: number[] = [];
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      terrainIds.push(miniKantoTerrainId(x, y));
      regionIds.push(miniKantoRegionId(x));
    }
  }

  return {
    scenarioId: "mini_kanto",
    name: "迷你关东试玩",
    width,
    height,
    terrains: [
      { id: 1, key: "plain", name: "平地", movementCost: 2 },
      { id: 2, key: "forest", name: "森林", movementCost: 3 },
      { id: 3, key: "hill", name: "丘陵", movementCost: 3 },
      { id: 4, key: "water", name: "水域", movementCost: 4 },
      { id: 5, key: "mountain", name: "山地", movementCost: 4 },
    ],
    regions: [
      { id: 1, key: "owari", name: "尾张" },
      { id: 2, key: "mikawa", name: "三河" },
      { id: 3, key: "suruga", name: "骏河" },
    ],
    roadTypes: [
      { id: 1, key: "highway", name: "官道", speedBonus: 1, movementCost: 1 },
    ],
    terrainIds,
    regionIds,
    roadCells: buildRoadCells(),
    landmarks: MINI_KANTO_LANDMARKS,
  };
}

export function mapMasterMatchesScenario(
  master: StrategyMapMasterState | null,
  scenarioId: string,
  width: number,
  height: number
): boolean {
  if (!master) return false;
  return (
    master.scenarioId === scenarioId &&
    master.width === width &&
    master.height === height &&
    master.terrainIds.length === width * height
  );
}
