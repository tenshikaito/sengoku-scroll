import type { StrategyWorldState } from "@/api/strategy";
import { getForceColor, getRealmColor } from "@/components/strategy/forceColors";
import {
  DIPLOMACY_MAP_COLORS,
  diplomacyMapColorCss,
  resolveDiplomacyMapStance,
  resolveRealmRootId,
  type StrategyDiplomacyMapStance,
  type StrategyMapColorMode,
} from "@/utils/mapEntityColorHelpers";

export abstract class MapColorModeBehavior {
  abstract readonly mode: StrategyMapColorMode;

  abstract resolveColor(entityForceId: number, world: StrategyWorldState): number;
}

class ForceMapColorBehavior extends MapColorModeBehavior {
  readonly mode = "Force" as const;

  resolveColor(entityForceId: number, _world: StrategyWorldState): number {
    return getForceColor(entityForceId);
  }
}

class RealmMapColorBehavior extends MapColorModeBehavior {
  readonly mode = "Realm" as const;

  resolveColor(entityForceId: number, world: StrategyWorldState): number {
    return getRealmColor(resolveRealmRootId(entityForceId, world.forces), world.forces);
  }
}

class DiplomacyMapColorBehavior extends MapColorModeBehavior {
  readonly mode = "Diplomacy" as const;

  resolveColor(entityForceId: number, world: StrategyWorldState): number {
    return DIPLOMACY_MAP_COLORS[resolveDiplomacyMapStance(entityForceId, world)];
  }
}

const BEHAVIORS: Record<StrategyMapColorMode, MapColorModeBehavior> = {
  Force: new ForceMapColorBehavior(),
  Realm: new RealmMapColorBehavior(),
  Diplomacy: new DiplomacyMapColorBehavior(),
};

export class MapColorModeBehaviorFactory {
  static create(mode: StrategyMapColorMode): MapColorModeBehavior {
    return BEHAVIORS[mode] ?? BEHAVIORS.Force;
  }
}

export function resolveEntityMapColor(
  entityForceId: number,
  world: StrategyWorldState,
  mode: StrategyMapColorMode,
): number {
  return MapColorModeBehaviorFactory.create(mode).resolveColor(entityForceId, world);
}

export { diplomacyMapColorCss, type StrategyDiplomacyMapStance, type StrategyMapColorMode };
