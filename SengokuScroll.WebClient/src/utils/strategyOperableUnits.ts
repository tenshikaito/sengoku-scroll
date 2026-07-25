import type {
  StrategyUnitRosterEntry,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";

export type OperableUnitRef =
  | { kind: "map"; unit: StrategyUnitState }
  | { kind: "roster"; unit: StrategyUnitRosterEntry };

/** 本家可操作兵队：地图可见 + 迷雾外 roster（互斥去重）。 */
export function listOperableUnits(worldState: StrategyWorldState): OperableUnitRef[] {
  const playerForceId = worldState.playerForceId;
  const mapUnitIds = new Set<number>();

  const mapUnits = worldState.units
    .filter((u) => u.forceId === playerForceId && u.mapVisible !== false)
    .map((unit) => {
      mapUnitIds.add(unit.id);
      return { kind: "map" as const, unit };
    });

  const rosterUnits = (worldState.ownUnitRoster ?? [])
    .filter((u) => u.forceId === playerForceId && !mapUnitIds.has(u.id))
    .map((unit) => ({ kind: "roster" as const, unit }));

  return [...mapUnits, ...rosterUnits];
}

export function findOperableUnit(
  worldState: StrategyWorldState,
  unitId: number,
): OperableUnitRef | null {
  const mapUnit = worldState.units.find((u) => u.id === unitId);
  if (mapUnit) return { kind: "map", unit: mapUnit };
  const rosterUnit = worldState.ownUnitRoster?.find((u) => u.id === unitId);
  if (rosterUnit) return { kind: "roster", unit: rosterUnit };
  return null;
}

export function isMapOperableUnit(
  ref: OperableUnitRef | null,
): ref is { kind: "map"; unit: StrategyUnitState } {
  return ref?.kind === "map";
}

export function resolveOperableUnitLocation(
  entry: OperableUnitRef,
): { x: number; y: number } {
  return { x: entry.unit.x, y: entry.unit.y };
}

export function resolveOperableUnitStrongholdId(entry: OperableUnitRef): number | null {
  const id =
    entry.kind === "map"
      ? entry.unit.locationStrongholdId
      : entry.unit.locationStrongholdId;
  return id && id > 0 ? id : null;
}

export function resolveOperableUnitStrongholdName(
  worldState: StrategyWorldState,
  entry: OperableUnitRef,
): string | null {
  const strongholdId = resolveOperableUnitStrongholdId(entry);
  if (strongholdId == null) return null;
  return worldState.strongholds.find((s) => s.id === strongholdId)?.name ?? null;
}

/** 侧栏 roster 条目转为指令菜单所需的最小 StrategyUnitState。 */
export function operableUnitAsMapState(
  _worldState: StrategyWorldState,
  entry: OperableUnitRef,
): StrategyUnitState {
  if (entry.kind === "map") return entry.unit;

  const roster = entry.unit;
  return {
    id: roster.id,
    name: roster.name,
    forceId: roster.forceId,
    x: roster.x,
    y: roster.y,
    soldiers: roster.soldiers,
    food: 0,
    ap: roster.ap,
    movement: 0,
    status: roster.status,
    directive: roster.directive,
    stance: "Normal",
    siegeMode: "None",
    directiveTargetId: 0,
    targetUnitId: 0,
    route: [],
    morale: 0,
    training: 0,
    cultureName: "",
    religionName: "",
    money: 0,
    composition: [],
    supplyStatus: roster.supplyStatus,
    foodDaysRemaining: 0,
    inTransitSupplies: [],
    commanderName: roster.commanderName,
    mapVisible: false,
    inStronghold: roster.inStronghold === true,
    homeStrongholdId: roster.homeStrongholdId,
    locationStrongholdId: roster.locationStrongholdId,
  };
}
