import type {
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyMessengerState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";

export function fogDisabled(worldState: StrategyWorldState): boolean {
  const mode = worldState.visibility?.fogMode;
  return !mode || mode === "None";
}

export function isTileExplored(worldState: StrategyWorldState, x: number, y: number): boolean {
  if (fogDisabled(worldState)) return true;
  const vis = worldState.visibility;
  if (!vis) return true;
  const idx = y * vis.mapWidth + x;
  const word = Math.floor(idx / 32);
  const bit = idx % 32;
  return (((vis.exploredBits[word] ?? 0) >>> bit) & 1) === 1;
}

export function isTileVisible(worldState: StrategyWorldState, x: number, y: number): boolean {
  if (fogDisabled(worldState)) return true;
  const vis = worldState.visibility;
  if (!vis) return true;
  return vis.visibleCells.some((c) => c.x === x && c.y === y);
}

function isOwnRealmForce(worldState: StrategyWorldState, forceId: number): boolean {
  return isPlayerRealmForce(forceId, worldState.playerForceId, worldState.forces);
}

/** 据点是否允许在悬浮框中展示（与地图绘制规则对齐）。 */
export function isStrongholdIntelVisible(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (fogDisabled(worldState)) return true;
  if (isOwnRealmForce(worldState, stronghold.forceId)) return true;
  if (stronghold.visibilityTier === "Known") return isTileExplored(worldState, stronghold.x, stronghold.y);
  return isTileVisible(worldState, stronghold.x, stronghold.y);
}

/** 单位是否允许在悬浮框中展示（mapVisible + 当前格可见）。 */
export function isUnitIntelVisible(worldState: StrategyWorldState, unit: StrategyUnitState): boolean {
  if (fogDisabled(worldState)) return true;
  if (unit.mapVisible === false) return false;
  return isTileVisible(worldState, unit.x, unit.y);
}

export function isConvoyIntelVisible(
  worldState: StrategyWorldState,
  convoy: StrategySupplyConvoyState,
): boolean {
  if (fogDisabled(worldState)) return true;
  if (isOwnRealmForce(worldState, convoy.forceId)) return true;
  return isTileVisible(worldState, convoy.x, convoy.y);
}

export function isMessengerIntelVisible(
  worldState: StrategyWorldState,
  messenger: StrategyMessengerState,
): boolean {
  if (fogDisabled(worldState)) return true;
  if (isOwnRealmForce(worldState, messenger.forceId)) return true;
  return isTileVisible(worldState, messenger.x, messenger.y);
}

export function strongholdsAtCellForIntel(
  worldState: StrategyWorldState,
  x: number,
  y: number,
): StrategyStrongholdState[] {
  return worldState.strongholds.filter(
    (s) => s.x === x && s.y === y && isStrongholdIntelVisible(worldState, s),
  );
}

export function unitsAtCellForIntel(
  worldState: StrategyWorldState,
  x: number,
  y: number,
): StrategyUnitState[] {
  return worldState.units.filter((u) => u.x === x && u.y === y && isUnitIntelVisible(worldState, u));
}

export function convoysAtCellForIntel(
  worldState: StrategyWorldState,
  x: number,
  y: number,
): StrategySupplyConvoyState[] {
  return worldState.supplyConvoys.filter(
    (c) => c.x === x && c.y === y && isConvoyIntelVisible(worldState, c),
  );
}

export function messengersAtCellForIntel(
  worldState: StrategyWorldState,
  x: number,
  y: number,
): StrategyMessengerState[] {
  return worldState.messengers.filter(
    (m) => m.x === x && m.y === y && isMessengerIntelVisible(worldState, m),
  );
}

function battlefieldAtCell(worldState: StrategyWorldState, x: number, y: number) {
  return worldState.battlefields?.find((b) => b.x === x && b.y === y) ?? null;
}

/** 当前格是否有可在悬浮框展示的情报实体。 */
export function intelEntityCountAtCell(worldState: StrategyWorldState, x: number, y: number): number {
  let count = strongholdsAtCellForIntel(worldState, x, y).length;
  const bf = battlefieldAtCell(worldState, x, y);
  if (bf && (fogDisabled(worldState) || isTileVisible(worldState, x, y))) {
    count += 1;
  } else {
    count += unitsAtCellForIntel(worldState, x, y).length;
  }
  count += convoysAtCellForIntel(worldState, x, y).length;
  count += messengersAtCellForIntel(worldState, x, y).length;
  return count;
}

/** 未探索格不展示实体悬浮情报。 */
export function canShowCellHoverIntel(worldState: StrategyWorldState, x: number, y: number): boolean {
  if (fogDisabled(worldState)) return intelEntityCountAtCell(worldState, x, y) > 0;
  if (!isTileExplored(worldState, x, y)) return false;
  return intelEntityCountAtCell(worldState, x, y) > 0;
}
