import type {
  MapPoint,
  StrategyForceState,
  StrategyLordState,
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategySubUnitState,
  StrategyInTransitSupply,
  StrategyUnitState,
  StrategyWorldState,
  StrategyMapLandmarkState,
} from "@/api/strategyTypes";
import { enrichStrategyMapState } from "@/utils/strategyMapDefaults";

function pick<T>(obj: Record<string, unknown>, camel: string, pascal: string): unknown {
  return obj[camel] ?? obj[pascal];
}

export function safeInt(value: unknown, fallback = 0): number {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function optionalString(value: unknown): string | null {
  if (value == null) return null;
  const s = String(value).trim();
  if (!s || s === "undefined" || s === "null") return null;
  return s;
}

function requiredString(value: unknown, fallback: string): string {
  return optionalString(value) ?? fallback;
}

function normalizeMapPoint(raw: unknown): MapPoint {
  const p = (raw ?? {}) as Record<string, unknown>;
  return { x: safeInt(pick(p, "x", "X")), y: safeInt(pick(p, "y", "Y")) };
}

function normalizeLord(raw: unknown): StrategyLordState {
  const l = (raw ?? {}) as Record<string, unknown>;
  const unitIdRaw = pick(l, "unitId", "UnitId");
  return {
    name: requiredString(pick(l, "name", "Name"), "当主"),
    unitId: unitIdRaw == null ? null : safeInt(unitIdRaw, 0) || null,
    x: safeInt(pick(l, "x", "X")),
    y: safeInt(pick(l, "y", "Y")),
  };
}

function normalizeSubUnit(raw: unknown): StrategySubUnitState {
  const s = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(s, "id", "Id"));
  return {
    id,
    typeId: safeInt(pick(s, "typeId", "TypeId"), 1),
    typeName: requiredString(pick(s, "typeName", "TypeName"), `兵种 #${id}`),
    soldiers: safeInt(pick(s, "soldiers", "Soldiers")),
    ratioPercent: safeInt(pick(s, "ratioPercent", "RatioPercent")),
    commanderId:
      pick(s, "commanderId", "CommanderId") == null
        ? null
        : safeInt(pick(s, "commanderId", "CommanderId"), 0) || null,
    commanderName: optionalString(pick(s, "commanderName", "CommanderName")),
  };
}

function normalizeInTransitSupply(raw: unknown): StrategyInTransitSupply {
  const s = (raw ?? {}) as Record<string, unknown>;
  return {
    convoyId: safeInt(pick(s, "convoyId", "ConvoyId")),
    cargoFoodGo: safeInt(pick(s, "cargoFoodGo", "CargoFoodGo")),
    estimatedDays: safeInt(pick(s, "estimatedDays", "EstimatedDays"), 1),
    isDeceived: Boolean(pick(s, "isDeceived", "IsDeceived")),
    originStrongholdName: optionalString(pick(s, "originStrongholdName", "OriginStrongholdName")),
  };
}

function normalizeUnit(raw: unknown, lord: StrategyLordState): StrategyUnitState {
  const u = raw as Record<string, unknown>;
  const id = safeInt(pick(u, "id", "Id"));
  const x = safeInt(pick(u, "x", "X"));
  const y = safeInt(pick(u, "y", "Y"));

  let commanderName = optionalString(pick(u, "commanderName", "CommanderName"));
  if (!commanderName && lord.unitId != null && lord.unitId === id) {
    commanderName = lord.name;
  }

  const routeRaw = pick(u, "route", "Route");
  const route = Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [];

  return {
    id,
    name: requiredString(pick(u, "name", "Name"), `部队 #${id}`),
    forceId: safeInt(pick(u, "forceId", "ForceId"), 1),
    x,
    y,
    soldiers: safeInt(pick(u, "soldiers", "Soldiers")),
    food: safeInt(pick(u, "food", "Food")),
    ap: safeInt(pick(u, "ap", "Ap"), safeInt(pick(u, "movement", "Movement"), 10)),
    movement: safeInt(pick(u, "movement", "Movement"), 10),
    status: requiredString(pick(u, "status", "Status"), "Waiting"),
    directive: requiredString(pick(u, "directive", "Directive"), "Move"),
    route,
    commanderName,
    commanderId: pick(u, "commanderId", "CommanderId") == null
      ? null
      : safeInt(pick(u, "commanderId", "CommanderId"), 0) || null,
    morale: safeInt(pick(u, "morale", "Morale"), 75),
    training: safeInt(pick(u, "training", "Training"), 70),
    cultureName: requiredString(pick(u, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(u, "religionName", "ReligionName"), "神道"),
    money: safeInt(pick(u, "money", "Money")),
    composition: Array.isArray(pick(u, "composition", "Composition"))
      ? (pick(u, "composition", "Composition") as unknown[]).map(normalizeSubUnit)
      : [],
    supplyStatus: requiredString(pick(u, "supplyStatus", "SupplyStatus"), "Sufficient"),
    foodDaysRemaining: safeInt(pick(u, "foodDaysRemaining", "FoodDaysRemaining")),
    inTransitSupplies: Array.isArray(pick(u, "inTransitSupplies", "InTransitSupplies"))
      ? (pick(u, "inTransitSupplies", "InTransitSupplies") as unknown[]).map(normalizeInTransitSupply)
      : [],
  };
}

function normalizeStronghold(
  raw: unknown,
  lord: StrategyLordState,
  playerForceId: number
): StrategyStrongholdState {
  const s = raw as Record<string, unknown>;
  const id = safeInt(pick(s, "id", "Id"));
  const x = safeInt(pick(s, "x", "X"));
  const y = safeInt(pick(s, "y", "Y"));
  const forceId = safeInt(pick(s, "forceId", "ForceId"), 1);
  const lordId = safeInt(pick(s, "lordId", "LordId"));
  const isDirectRule =
    pick(s, "isDirectRule", "IsDirectRule") == null
      ? lordId === 0
      : Boolean(pick(s, "isDirectRule", "IsDirectRule"));

  let lordName = optionalString(pick(s, "lordName", "LordName"));
  if (!lordName) {
    lordName = isDirectRule
      ? forceId === playerForceId
        ? lord.name
        : "当主"
      : `领主 #${lordId}`;
  }

  return {
    id,
    name: requiredString(pick(s, "name", "Name"), `据点 #${id}`),
    forceId,
    x,
    y,
    food: safeInt(pick(s, "food", "Food")),
    population: safeInt(pick(s, "population", "Population")),
    lordId,
    isDirectRule,
    lordName,
    mayorName: optionalString(pick(s, "mayorName", "MayorName")),
    morale: safeInt(pick(s, "morale", "Morale"), 80),
    training: safeInt(pick(s, "training", "Training"), 65),
    cultureName: requiredString(pick(s, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(s, "religionName", "ReligionName"), "神道"),
    money: safeInt(pick(s, "money", "Money")),
    pollTaxRate: safeInt(pick(s, "pollTaxRate", "PollTaxRate"), 10),
    agricultureTaxRate: safeInt(pick(s, "agricultureTaxRate", "AgricultureTaxRate"), 25),
    commerceTaxRate: safeInt(pick(s, "commerceTaxRate", "CommerceTaxRate"), 12),
    tariffTaxRate: safeInt(pick(s, "tariffTaxRate", "TariffTaxRate"), 8),
  };
}

function normalizeConvoy(raw: unknown): StrategySupplyConvoyState {
  const c = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(c, "id", "Id"));
  const food = safeInt(pick(c, "food", "Food"), safeInt(pick(c, "cargoFoodGo", "CargoFoodGo")));
  const routeRaw = pick(c, "route", "Route");
  return {
    id,
    name: requiredString(pick(c, "name", "Name"), `粮运队 #${id}`),
    forceId: safeInt(pick(c, "forceId", "ForceId"), 1),
    x: safeInt(pick(c, "x", "X")),
    y: safeInt(pick(c, "y", "Y")),
    isMilitary: false,
    commanderName: optionalString(pick(c, "commanderName", "CommanderName")),
    commanderId:
      pick(c, "commanderId", "CommanderId") == null
        ? null
        : safeInt(pick(c, "commanderId", "CommanderId"), 0) || null,
    soldiers: safeInt(pick(c, "soldiers", "Soldiers")),
    porterCount: safeInt(pick(c, "porterCount", "PorterCount")),
    escortSoldierCount: safeInt(pick(c, "escortSoldierCount", "EscortSoldierCount")),
    food,
    cargoFoodGo: safeInt(pick(c, "cargoFoodGo", "CargoFoodGo"), food),
    ap: safeInt(pick(c, "ap", "Ap")),
    movement: safeInt(pick(c, "movement", "Movement"), 4),
    status: requiredString(pick(c, "status", "Status"), "Moving"),
    directive: requiredString(pick(c, "directive", "Directive"), "Support"),
    route: Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [],
    morale: safeInt(pick(c, "morale", "Morale"), 75),
    training: safeInt(pick(c, "training", "Training"), 65),
    cultureName: requiredString(pick(c, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(c, "religionName", "ReligionName"), "神道"),
    money: safeInt(pick(c, "money", "Money")),
    targetUnitId: safeInt(pick(c, "targetUnitId", "TargetUnitId")),
    targetUnitName: optionalString(pick(c, "targetUnitName", "TargetUnitName")),
    originStrongholdId: safeInt(pick(c, "originStrongholdId", "OriginStrongholdId")),
    originStrongholdName: optionalString(pick(c, "originStrongholdName", "OriginStrongholdName")),
    isReturningToOrigin: Boolean(pick(c, "isReturningToOrigin", "IsReturningToOrigin")),
  };
}

function normalizeMessenger(raw: unknown): StrategyMessengerState {
  const m = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(m, "id", "Id"));
  const routeRaw = pick(m, "route", "Route");
  const pending = pick(m, "pendingDirective", "PendingDirective");
  const courierCount = safeInt(pick(m, "courierCount", "CourierCount"), 2);
  const escortSoldierCount = safeInt(pick(m, "escortSoldierCount", "EscortSoldierCount"), 8);
  const soldiersRaw = pick(m, "soldiers", "Soldiers");
  return {
    id,
    name: requiredString(pick(m, "name", "Name"), `信使 #${id}`),
    forceId: safeInt(pick(m, "forceId", "ForceId"), 1),
    x: safeInt(pick(m, "x", "X")),
    y: safeInt(pick(m, "y", "Y")),
    isMilitary: false,
    soldiers: soldiersRaw == null ? courierCount + escortSoldierCount : safeInt(soldiersRaw),
    courierCount,
    escortSoldierCount,
    ap: safeInt(pick(m, "ap", "Ap")),
    movement: safeInt(pick(m, "movement", "Movement"), 6),
    status: requiredString(pick(m, "status", "Status"), "Moving"),
    payloadType: requiredString(pick(m, "payloadType", "PayloadType"), "PolicyChange"),
    directive: requiredString(pick(m, "directive", "Directive"), "PolicyChange"),
    route: Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [],
    morale: safeInt(pick(m, "morale", "Morale"), 80),
    training: safeInt(pick(m, "training", "Training"), 70),
    cultureName: requiredString(pick(m, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(m, "religionName", "ReligionName"), "神道"),
    money: safeInt(pick(m, "money", "Money")),
    targetUnitId: safeInt(pick(m, "targetUnitId", "TargetUnitId")),
    targetUnitName: optionalString(pick(m, "targetUnitName", "TargetUnitName")),
    originStrongholdId: safeInt(pick(m, "originStrongholdId", "OriginStrongholdId")),
    originStrongholdName: optionalString(pick(m, "originStrongholdName", "OriginStrongholdName")),
    pendingDirective: pending == null ? null : String(pending),
  };
}

/** 规范化 API/Mock 世界状态，补齐缺失情报字段并兼容 PascalCase。 */
export function normalizeStrategyWorldState(raw: unknown): StrategyWorldState {
  if (!raw || typeof raw !== "object") {
    throw new Error("无效的策略世界状态");
  }

  const o = raw as Record<string, unknown>;
  const lord = normalizeLord(pick(o, "lord", "Lord"));
  const mapRaw = (pick(o, "map", "Map") ?? {}) as Record<string, unknown>;
  const dateRaw = (pick(o, "date", "Date") ?? {}) as Record<string, unknown>;

  const unitsRaw = pick(o, "units", "Units");
  const strongholdsRaw = pick(o, "strongholds", "Strongholds");
  const forcesRaw = pick(o, "forces", "Forces");
  const convoysRaw = pick(o, "supplyConvoys", "SupplyConvoys");
  const messengersRaw = pick(o, "messengers", "Messengers");
  const diplomaciesRaw = pick(o, "diplomacies", "Diplomacies");

  const playerForceId = safeInt(pick(o, "playerForceId", "PlayerForceId"), 1);
  const scenarioId = requiredString(pick(o, "scenarioId", "ScenarioId"), "mini_kanto");

  const width = safeInt(pick(mapRaw, "width", "Width"), 10);
  const height = safeInt(pick(mapRaw, "height", "Height"), 10);
  const tileTerrainRaw = pick(mapRaw, "tileTerrainNames", "TileTerrainNames");
  const tileRegionRaw = pick(mapRaw, "tileRegionNames", "TileRegionNames");
  const landmarksRaw = pick(mapRaw, "landmarks", "Landmarks");

  const map = enrichStrategyMapState(scenarioId, {
    name: requiredString(pick(mapRaw, "name", "Name"), "策略地图"),
    width,
    height,
    roadCells: Array.isArray(pick(mapRaw, "roadCells", "RoadCells"))
      ? (pick(mapRaw, "roadCells", "RoadCells") as unknown[]).map((c) => {
          const cell = c as Record<string, unknown>;
          return {
            x: safeInt(pick(cell, "x", "X")),
            y: safeInt(pick(cell, "y", "Y")),
            typeId: safeInt(pick(cell, "typeId", "TypeId"), 1),
            typeName: requiredString(pick(cell, "typeName", "TypeName"), "官道"),
            level: safeInt(pick(cell, "level", "Level"), safeInt(pick(cell, "typeId", "TypeId"), 1)),
            speedBonus: safeInt(pick(cell, "speedBonus", "SpeedBonus")),
            movementCost: safeInt(pick(cell, "movementCost", "MovementCost"), 1),
          };
        })
      : [],
    tileTerrainNames: Array.isArray(tileTerrainRaw)
      ? tileTerrainRaw.map((v) => requiredString(v, "平地"))
      : undefined,
    tileRegionNames: Array.isArray(tileRegionRaw)
      ? tileRegionRaw.map((v) => optionalString(v))
      : undefined,
    landmarks: Array.isArray(landmarksRaw)
      ? landmarksRaw.map((lm) => {
          const item = lm as Record<string, unknown>;
          const id = safeInt(pick(item, "id", "Id"));
          return {
            id,
            name: requiredString(pick(item, "name", "Name"), `地标 #${id}`),
            x: safeInt(pick(item, "x", "X")),
            y: safeInt(pick(item, "y", "Y")),
          } satisfies StrategyMapLandmarkState;
        })
      : undefined,
  });

  return {
    scenarioId,
    playerForceId,
    lord,
    map,
    date: {
      year: safeInt(pick(dateRaw, "year", "Year"), 1560),
      month: safeInt(pick(dateRaw, "month", "Month"), 1),
      day: safeInt(pick(dateRaw, "day", "Day"), 1),
    },
    forces: Array.isArray(forcesRaw)
      ? forcesRaw.map((f) => {
          const force = f as Record<string, unknown>;
          const suzerainRaw = pick(force, "suzerainForceId", "SuzerainForceId");
          return {
            id: safeInt(pick(force, "id", "Id")),
            name: requiredString(pick(force, "name", "Name"), "未知势力"),
            food: safeInt(pick(force, "food", "Food")),
            money: safeInt(pick(force, "money", "Money")),
            status: requiredString(pick(force, "status", "Status"), "Independence"),
            suzerainForceId:
              suzerainRaw == null ? null : safeInt(suzerainRaw, 0) || null,
          } satisfies StrategyForceState;
        })
      : [],
    strongholds: Array.isArray(strongholdsRaw)
      ? strongholdsRaw.map((s) => normalizeStronghold(s, lord, playerForceId))
      : [],
    units: Array.isArray(unitsRaw) ? unitsRaw.map((u) => normalizeUnit(u, lord)) : [],
    supplyConvoys: Array.isArray(convoysRaw) ? convoysRaw.map(normalizeConvoy) : [],
    messengers: Array.isArray(messengersRaw) ? messengersRaw.map(normalizeMessenger) : [],
    diplomacies: Array.isArray(diplomaciesRaw)
      ? diplomaciesRaw.map((d) => {
          const row = d as Record<string, unknown>;
          return {
            targetForceId: safeInt(pick(row, "targetForceId", "TargetForceId")),
            relation: requiredString(pick(row, "relation", "Relation"), "Neutral"),
          };
        })
      : [],
  };
}
