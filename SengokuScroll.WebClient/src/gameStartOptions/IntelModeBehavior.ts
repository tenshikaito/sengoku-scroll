import type { GameStartOptionsState, StrategyWorldState } from "@/api/strategyTypes";
import { isPlayerRealmForce, resolveRealmRootId } from "@/utils/mapEntityColors";
import type { IntelModeId } from "./types";

/** 情报档位行为。 */
export abstract class IntelModeBehavior {
  abstract readonly mode: IntelModeId;
  abstract readonly restricted: boolean;
  abstract readonly showAllyIntelOption: boolean;

  abstract isForeignIntelRestricted(
    worldState: StrategyWorldState,
    forceId: number,
  ): boolean;
}

export class FullIntelModeBehavior extends IntelModeBehavior {
  readonly mode = "Full" as const;
  readonly restricted = false;
  readonly showAllyIntelOption = false;

  isForeignIntelRestricted(): boolean {
    return false;
  }
}

function resolveShowAllyIntel(worldState: StrategyWorldState): boolean {
  return (
    worldState.startOptions?.showAllyIntel
    ?? worldState.visibility?.showAllyIntel
    ?? false
  );
}

/** 开启显示同盟情报时：与同盟 Realm 共享具体数值；外藩除外。 */
export function isAllyIntelVisible(
  worldState: StrategyWorldState,
  forceId: number,
): boolean {
  if (!resolveShowAllyIntel(worldState)) return false;

  const force = worldState.forces.find((f) => f.id === forceId);
  if (!force || force.status === "OuterVassal") return false;

  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  const targetRoot = resolveRealmRootId(forceId, worldState.forces);
  if (playerRoot === targetRoot) return false;

  const diplomacy = worldState.diplomacies.find((d) => d.targetForceId === targetRoot);
  return diplomacy?.relation === "Allied";
}

export class ForceIntelModeBehavior extends IntelModeBehavior {
  readonly mode = "ForceIntel" as const;
  readonly restricted = true;
  readonly showAllyIntelOption = true;

  isForeignIntelRestricted(worldState: StrategyWorldState, forceId: number): boolean {
    if (isPlayerRealmForce(forceId, worldState.playerForceId, worldState.forces)) {
      return false;
    }
    if (isAllyIntelVisible(worldState, forceId)) return false;
    return true;
  }
}

const INTEL_BEHAVIORS: Record<IntelModeId, IntelModeBehavior> = {
  Full: new FullIntelModeBehavior(),
  ForceIntel: new ForceIntelModeBehavior(),
};

export class IntelModeBehaviorFactory {
  static create(mode: string | undefined | null): IntelModeBehavior {
    switch (mode) {
      case "Full":
        return INTEL_BEHAVIORS.Full;
      case "ForceIntel":
      default:
        return INTEL_BEHAVIORS.ForceIntel;
    }
  }
}

export function resolveIntelModeFromOptions(options: GameStartOptionsState): string {
  return IntelModeBehaviorFactory.create(options.intelMode).mode;
}
