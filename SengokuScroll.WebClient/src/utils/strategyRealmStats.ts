import type {
  StrategyCharacterSummaryState,
  StrategyForceState,
  StrategyStrongholdState,
  StrategyUnitState,
} from "@/api/strategyTypes";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";

function countUniqueOfficers(
  strongholds: readonly StrategyStrongholdState[],
  units: readonly StrategyUnitState[],
  options?: { lordName?: string | null; forceId?: number }
): number {
  const { lordName, forceId } = options ?? {};
  const seen = new Set<string>();

  const addId = (id: number | null | undefined) => {
    if (id != null && id > 0) seen.add(`id:${id}`);
  };
  const addName = (name: string | null | undefined) => {
    const trimmed = name?.trim();
    if (trimmed) seen.add(`n:${trimmed}`);
  };

  if (lordName?.trim() && (forceId == null || forceId > 0)) {
    addName(lordName);
  }

  for (const s of strongholds) {
    if (forceId != null && s.forceId !== forceId) continue;
    if (s.lordId > 0) addId(s.lordId);
    else addName(s.lordName);
    addName(s.mayorName);
  }

  for (const u of units) {
    if (forceId != null && u.forceId !== forceId) continue;
    if (u.commanderId != null && u.commanderId > 0) addId(u.commanderId);
    else addName(u.commanderName);
    for (const sub of u.composition ?? []) {
      if (sub.commanderId != null && sub.commanderId > 0) addId(sub.commanderId);
      else addName(sub.commanderName);
    }
  }

  return seen.size;
}

/** 封地据点数（含旗下内藩）。 */
export function countRealmStrongholds(
  rootForceId: number,
  forces: readonly StrategyForceState[],
  strongholds: readonly StrategyStrongholdState[]
): number {
  return strongholds.filter((s) => isPlayerRealmForce(s.forceId, rootForceId, forces)).length;
}

/** 本势力据点数（不含内藩）。 */
export function countOwnStrongholds(
  forceId: number,
  strongholds: readonly StrategyStrongholdState[]
): number {
  return strongholds.filter((s) => s.forceId === forceId).length;
}

/** 封地将领数（含旗下内藩）。 */
export function countRealmCharacters(
  rootForceId: number,
  forces: readonly StrategyForceState[],
  strongholds: readonly StrategyStrongholdState[],
  units: readonly StrategyUnitState[],
  options?: {
    characters?: readonly StrategyCharacterSummaryState[];
    forceCharacterCount?: number;
    lordName?: string | null;
  }
): number {
  const { characters, forceCharacterCount, lordName } = options ?? {};

  if (characters && characters.length > 0) {
    return characters.filter((c) => isPlayerRealmForce(c.forceId, rootForceId, forces)).length;
  }

  if (forceCharacterCount != null && forceCharacterCount > 0) {
    return forceCharacterCount;
  }

  const realmStrongholds = strongholds.filter((s) =>
    isPlayerRealmForce(s.forceId, rootForceId, forces)
  );
  const realmUnits = units.filter((u) => isPlayerRealmForce(u.forceId, rootForceId, forces));
  return countUniqueOfficers(realmStrongholds, realmUnits, { lordName });
}

/** 本势力将领数（不含内藩）。 */
export function countOwnCharacters(
  forceId: number,
  strongholds: readonly StrategyStrongholdState[],
  units: readonly StrategyUnitState[],
  options?: {
    characters?: readonly StrategyCharacterSummaryState[];
    lordName?: string | null;
  }
): number {
  const { characters, lordName } = options ?? {};

  if (characters && characters.length > 0) {
    return characters.filter((c) => c.forceId === forceId).length;
  }

  const ownStrongholds = strongholds.filter((s) => s.forceId === forceId);
  const ownUnits = units.filter((u) => u.forceId === forceId);
  return countUniqueOfficers(ownStrongholds, ownUnits, { lordName, forceId });
}
