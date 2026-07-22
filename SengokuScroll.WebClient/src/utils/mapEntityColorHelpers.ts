import type { StrategyDiplomacyState, StrategyForceState, StrategyWorldState } from "@/api/strategyTypes";

/** 地图实体着色模式。 */
export type StrategyMapColorMode = "Force" | "Realm" | "Diplomacy";

/** 外交视角下的关系档位（含内藩归入宗主）。 */
export type StrategyDiplomacyMapStance = "Self" | "Allied" | "Enemy" | "NonHostile";

export const DIPLOMACY_MAP_COLORS: Record<StrategyDiplomacyMapStance, number> = {
  Self: 0x2563eb,
  Allied: 0x16a34a,
  Enemy: 0xdc2626,
  NonHostile: 0xf97316,
};

export function resolveRealmRootId(
  forceId: number,
  forces: readonly StrategyForceState[],
): number {
  const byId = new Map(forces.map((f) => [f.id, f]));
  let current = forceId;
  const visited = new Set<number>();

  while (true) {
    if (visited.has(current)) return current;
    visited.add(current);

    const force = byId.get(current);
    if (!force) return current;

    if (force.status === "InnerVassal" && force.suzerainForceId != null) {
      current = force.suzerainForceId;
      continue;
    }

    return current;
  }
}

export function isPlayerRealmForce(
  forceId: number,
  playerForceId: number,
  forces: readonly StrategyForceState[],
): boolean {
  return resolveRealmRootId(forceId, forces) === resolveRealmRootId(playerForceId, forces);
}

function lookupDiplomaticRelation(
  playerRootId: number,
  targetRootId: number,
  diplomacies: readonly StrategyDiplomacyState[],
): string | null {
  if (playerRootId === targetRootId) return "Self";

  const entry = diplomacies.find((d) => d.targetForceId === targetRootId);
  return entry?.relation ?? null;
}

export function resolveDiplomacyMapStance(
  entityForceId: number,
  world: Pick<StrategyWorldState, "playerForceId" | "forces" | "diplomacies">,
): StrategyDiplomacyMapStance {
  const playerRoot = resolveRealmRootId(world.playerForceId, world.forces);
  const entityRoot = resolveRealmRootId(entityForceId, world.forces);

  if (playerRoot === entityRoot) return "Self";

  const relation = lookupDiplomaticRelation(playerRoot, entityRoot, world.diplomacies);
  if (relation === "Allied") return "Allied";
  if (relation === "Enemy") return "Enemy";
  return "NonHostile";
}

export function diplomacyMapColorCss(stance: StrategyDiplomacyMapStance): string {
  const color = DIPLOMACY_MAP_COLORS[stance];
  return `#${color.toString(16).padStart(6, "0")}`;
}
