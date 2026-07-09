import type { StrategyDiplomacyState, StrategyForceState, StrategyWorldState } from "@/api/strategyTypes";
import { getForceColor, getRealmColor } from "@/components/strategy/forceColors";

/** 地图实体着色模式。 */
export type StrategyMapColorMode = "Force" | "Realm" | "Diplomacy";

/** 外交视角下的关系档位（含内藩归入宗主）。 */
export type StrategyDiplomacyMapStance = "Self" | "Allied" | "Enemy" | "NonHostile";

const DIPLOMACY_MAP_COLORS: Record<StrategyDiplomacyMapStance, number> = {
  Self: 0x2563eb,
  Allied: 0x16a34a,
  Enemy: 0xdc2626,
  NonHostile: 0xf97316,
};

/** 沿内藩链上溯至封地/宗主根势力 Id。 */
export function resolveRealmRootId(
  forceId: number,
  forces: readonly StrategyForceState[]
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

/** 是否与玩家同一势力范围（本家 + 旗下内藩）。 */
export function isPlayerRealmForce(
  forceId: number,
  playerForceId: number,
  forces: readonly StrategyForceState[]
): boolean {
  return resolveRealmRootId(forceId, forces) === resolveRealmRootId(playerForceId, forces);
}

function lookupDiplomaticRelation(
  playerRootId: number,
  targetRootId: number,
  diplomacies: readonly StrategyDiplomacyState[]
): string | null {
  if (playerRootId === targetRootId) return "Self";

  const entry = diplomacies.find((d) => d.targetForceId === targetRootId);
  return entry?.relation ?? null;
}

/** 外交地图视角：蓝=自势力（含内藩），绿=同盟（含内藩），红=敌对（含内藩），橘=不敌对。 */
export function resolveDiplomacyMapStance(
  entityForceId: number,
  world: Pick<StrategyWorldState, "playerForceId" | "forces" | "diplomacies">
): StrategyDiplomacyMapStance {
  const playerRoot = resolveRealmRootId(world.playerForceId, world.forces);
  const entityRoot = resolveRealmRootId(entityForceId, world.forces);

  if (playerRoot === entityRoot) return "Self";

  const relation = lookupDiplomaticRelation(playerRoot, entityRoot, world.diplomacies);
  if (relation === "Allied") return "Allied";
  if (relation === "Enemy") return "Enemy";
  return "NonHostile";
}

/** 解析据点/单位等在地图上的填充色（Pixi 0xRRGGBB）。 */
export function resolveEntityMapColor(
  entityForceId: number,
  world: StrategyWorldState,
  mode: StrategyMapColorMode
): number {
  switch (mode) {
    case "Force":
      return getForceColor(entityForceId);
    case "Realm":
      return getRealmColor(resolveRealmRootId(entityForceId, world.forces), world.forces);
    case "Diplomacy":
      return DIPLOMACY_MAP_COLORS[resolveDiplomacyMapStance(entityForceId, world)];
    default:
      return getForceColor(entityForceId);
  }
}

export function diplomacyMapColorCss(stance: StrategyDiplomacyMapStance): string {
  const color = DIPLOMACY_MAP_COLORS[stance];
  return `#${color.toString(16).padStart(6, "0")}`;
}
