import type { StrategySupplyConvoyState, StrategyWorldState } from "@/api/strategy";
import {
  isLordLeadingUnit,
  resolvePlayerLordCharacterId,
} from "@/utils/strategyPlayerCharacter";

/** 移民队无所属势力、无指令菜单，不参与格点多选。 */
export function isMigrantConvoy(convoy: StrategySupplyConvoyState): boolean {
  return convoy.forceId === 0;
}

export type MapCellEntityKind = "unit" | "character" | "stronghold" | "convoy";

export interface MapCellEntityOption {
  kind: MapCellEntityKind;
  id: number;
  label: string;
  subtitle?: string;
}

export interface MapCellEntityPickContext {
  includeUnits: boolean;
  includeCharacters: boolean;
  includeStrongholds: boolean;
  includeConvoys: boolean;
}

/** 收集格点上可交互的地图实体（用于多选菜单）。 */
export function collectMapCellEntityOptions(
  worldState: StrategyWorldState,
  x: number,
  y: number,
  pick: MapCellEntityPickContext,
): MapCellEntityOption[] {
  const options: MapCellEntityOption[] = [];

  if (pick.includeUnits) {
    for (const unit of worldState.units) {
      if (unit.x !== x || unit.y !== y || unit.soldiers <= 0) continue;
      options.push({
        kind: "unit",
        id: unit.id,
        label: unit.name?.trim() || `部队 #${unit.id}`,
      });
    }
  }

  if (pick.includeCharacters) {
    const seenCharacterIds = new Set<number>();

    for (const character of worldState.mapCharacters ?? []) {
      if (character.x !== x || character.y !== y) continue;
      if (!character.isPlayerControlled) continue;
      seenCharacterIds.add(character.id);
      options.push({
        kind: "character",
        id: character.id,
        label: character.name?.trim() || `角色 #${character.id}`,
      });
    }

    const lordId = resolvePlayerLordCharacterId(worldState);
    if (
      lordId != null
      && !isLordLeadingUnit(worldState)
      && worldState.lord.x === x
      && worldState.lord.y === y
      && !seenCharacterIds.has(lordId)
    ) {
      options.push({
        kind: "character",
        id: lordId,
        label: worldState.lord.name?.trim() || "当主",
      });
    }
  }

  if (pick.includeStrongholds) {
    for (const stronghold of worldState.strongholds) {
      if (stronghold.x !== x || stronghold.y !== y) continue;
      options.push({
        kind: "stronghold",
        id: stronghold.id,
        label: stronghold.name?.trim() || `据点 #${stronghold.id}`,
      });
    }
  }

  if (pick.includeConvoys) {
    for (const convoy of worldState.supplyConvoys) {
      if (convoy.x !== x || convoy.y !== y) continue;
      if (isMigrantConvoy(convoy)) continue;
      options.push({
        kind: "convoy",
        id: convoy.id,
        label: convoy.name?.trim() || `运输队 #${convoy.id}`,
      });
    }
  }

  return options;
}

export { mapCellEntityKindIcon } from "@/mapCellEntity/MapCellEntityKindBehavior";
