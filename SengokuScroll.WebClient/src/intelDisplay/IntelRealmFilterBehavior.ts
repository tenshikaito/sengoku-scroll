import type { StrategyForceState } from "@/api/strategy";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";

/** 情报列表势力范围筛选。 */
export type IntelRealmFilterMode = "all" | "realm" | "homeOnly";

export const INTEL_REALM_FILTER_OPTIONS: {
  value: IntelRealmFilterMode;
  label: string;
}[] = [
  { value: "all", label: "显示全部势力" },
  { value: "realm", label: "显示自势力" },
  { value: "homeOnly", label: "仅显示本家势力" },
];

export abstract class IntelRealmFilterBehavior {
  abstract readonly mode: IntelRealmFilterMode;

  abstract matches(
    forceId: number,
    playerForceId: number,
    forces: readonly StrategyForceState[],
  ): boolean;
}

class AllIntelRealmFilterBehavior extends IntelRealmFilterBehavior {
  readonly mode = "all" as const;

  matches(): boolean {
    return true;
  }
}

class HomeOnlyIntelRealmFilterBehavior extends IntelRealmFilterBehavior {
  readonly mode = "homeOnly" as const;

  matches(forceId: number, playerForceId: number): boolean {
    return forceId === playerForceId;
  }
}

class RealmIntelRealmFilterBehavior extends IntelRealmFilterBehavior {
  readonly mode = "realm" as const;

  matches(forceId: number, playerForceId: number, forces: readonly StrategyForceState[]): boolean {
    return isPlayerRealmForce(forceId, playerForceId, forces);
  }
}

const INTEL_REALM_FILTER_BEHAVIORS: Record<IntelRealmFilterMode, IntelRealmFilterBehavior> = {
  all: new AllIntelRealmFilterBehavior(),
  homeOnly: new HomeOnlyIntelRealmFilterBehavior(),
  realm: new RealmIntelRealmFilterBehavior(),
};

export function matchesIntelRealmFilter(
  forceId: number,
  playerForceId: number,
  forces: readonly StrategyForceState[],
  mode: IntelRealmFilterMode,
): boolean {
  return INTEL_REALM_FILTER_BEHAVIORS[mode]?.matches(forceId, playerForceId, forces) ?? true;
}
