import type { StrategyMapMasterState } from "@/api/strategyTypes";

function pick(obj: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (key in obj) return obj[key];
  }
  return undefined;
}

function safeInt(value: unknown, fallback = 0): number {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function requiredString(value: unknown, fallback: string): string {
  return typeof value === "string" && value.trim() ? value : fallback;
}

function intArray(raw: unknown): number[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((v) => safeInt(v));
}

export function normalizeStrategyMapMaster(raw: unknown): StrategyMapMasterState {
  if (!raw || typeof raw !== "object") {
    throw new Error("地图主数据响应为空");
  }

  const o = raw as Record<string, unknown>;
  const width = safeInt(pick(o, "width", "Width"), 10);
  const height = safeInt(pick(o, "height", "Height"), 10);

  const terrainsRaw = pick(o, "terrains", "Terrains");
  const regionsRaw = pick(o, "regions", "Regions");
  const roadTypesRaw = pick(o, "roadTypes", "RoadTypes");
  const roadCellsRaw = pick(o, "roadCells", "RoadCells");
  const landmarksRaw = pick(o, "landmarks", "Landmarks");

  return {
    scenarioId: requiredString(pick(o, "scenarioId", "ScenarioId"), "mini_kanto"),
    name: requiredString(pick(o, "name", "Name"), "策略地图"),
    width,
    height,
    terrains: Array.isArray(terrainsRaw)
      ? terrainsRaw.map((item) => {
          const t = item as Record<string, unknown>;
          return {
            id: safeInt(pick(t, "id", "Id"), 1),
            key: requiredString(pick(t, "key", "Key"), "plain"),
            name: requiredString(pick(t, "name", "Name"), "平地"),
            movementCost: safeInt(pick(t, "movementCost", "MovementCost"), 2),
          };
        })
      : [],
    regions: Array.isArray(regionsRaw)
      ? regionsRaw.map((item) => {
          const r = item as Record<string, unknown>;
          return {
            id: safeInt(pick(r, "id", "Id")),
            key: requiredString(pick(r, "key", "Key"), ""),
            name: requiredString(pick(r, "name", "Name"), ""),
          };
        })
      : [],
    roadTypes: Array.isArray(roadTypesRaw)
      ? roadTypesRaw.map((item) => {
          const r = item as Record<string, unknown>;
          return {
            id: safeInt(pick(r, "id", "Id"), 1),
            key: requiredString(pick(r, "key", "Key"), "highway"),
            name: requiredString(pick(r, "name", "Name"), "官道"),
            speedBonus: safeInt(pick(r, "speedBonus", "SpeedBonus")),
            movementCost: safeInt(pick(r, "movementCost", "MovementCost"), 1),
          };
        })
      : [],
    terrainIds: intArray(pick(o, "terrainIds", "TerrainIds")),
    regionIds: intArray(pick(o, "regionIds", "RegionIds")),
    roadCells: Array.isArray(roadCellsRaw)
      ? roadCellsRaw.map((cell) => {
          const c = cell as Record<string, unknown>;
          return {
            x: safeInt(pick(c, "x", "X")),
            y: safeInt(pick(c, "y", "Y")),
            typeId: safeInt(pick(c, "typeId", "TypeId"), 1),
            typeName: requiredString(pick(c, "typeName", "TypeName"), "官道"),
            level: safeInt(pick(c, "level", "Level"), safeInt(pick(c, "typeId", "TypeId"), 1)),
            speedBonus: safeInt(pick(c, "speedBonus", "SpeedBonus")),
            movementCost: safeInt(pick(c, "movementCost", "MovementCost"), 1),
          };
        })
      : [],
    landmarks: Array.isArray(landmarksRaw)
      ? landmarksRaw.map((lm) => {
          const item = lm as Record<string, unknown>;
          const id = safeInt(pick(item, "id", "Id"));
          return {
            id,
            name: requiredString(pick(item, "name", "Name"), `地标 #${id}`),
            x: safeInt(pick(item, "x", "X")),
            y: safeInt(pick(item, "y", "Y")),
          };
        })
      : [],
  };
}
