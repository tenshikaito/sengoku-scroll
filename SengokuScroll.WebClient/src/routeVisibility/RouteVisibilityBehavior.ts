import type { StrategyUnitState } from "@/api/strategyTypes";

export type RouteVisibilityPolicyId =
  | "playerControlledOnly"
  | "playerAndAllies"
  | "allForces";

export interface RouteVisibilityContext {
  policy: RouteVisibilityPolicyId;
  playerForceId: number;
  allyForceIds?: readonly number[];
}

export abstract class RouteVisibilityBehavior {
  abstract readonly policy: RouteVisibilityPolicyId;

  abstract isUnitRouteVisible(unit: StrategyUnitState, ctx: RouteVisibilityContext): boolean;
}

class PlayerControlledOnlyBehavior extends RouteVisibilityBehavior {
  readonly policy = "playerControlledOnly" as const;

  isUnitRouteVisible(unit: StrategyUnitState, ctx: RouteVisibilityContext): boolean {
    return unit.forceId === ctx.playerForceId;
  }
}

class PlayerAndAlliesBehavior extends RouteVisibilityBehavior {
  readonly policy = "playerAndAllies" as const;

  isUnitRouteVisible(unit: StrategyUnitState, ctx: RouteVisibilityContext): boolean {
    const allies = ctx.allyForceIds ?? [];
    return unit.forceId === ctx.playerForceId || allies.includes(unit.forceId);
  }
}

class AllForcesBehavior extends RouteVisibilityBehavior {
  readonly policy = "allForces" as const;

  isUnitRouteVisible(): boolean {
    return true;
  }
}

const BEHAVIORS: Record<RouteVisibilityPolicyId, RouteVisibilityBehavior> = {
  playerControlledOnly: new PlayerControlledOnlyBehavior(),
  playerAndAllies: new PlayerAndAlliesBehavior(),
  allForces: new AllForcesBehavior(),
};

export class RouteVisibilityBehaviorFactory {
  static create(policy: RouteVisibilityPolicyId): RouteVisibilityBehavior {
    return BEHAVIORS[policy] ?? BEHAVIORS.playerControlledOnly;
  }
}

export function isUnitRouteVisible(unit: StrategyUnitState, ctx: RouteVisibilityContext): boolean {
  return RouteVisibilityBehaviorFactory.create(ctx.policy).isUnitRouteVisible(unit, ctx);
}

export function filterUnitsForRouteDisplay(
  units: readonly StrategyUnitState[],
  ctx: RouteVisibilityContext,
): StrategyUnitState[] {
  return units.filter((u) => isUnitRouteVisible(u, ctx));
}

export const DEFAULT_ROUTE_VISIBILITY_POLICY: RouteVisibilityPolicyId = "playerControlledOnly";

export type { RouteVisibilityContext as RouteVisibilityPolicyContext };
export type RouteVisibilityPolicy = RouteVisibilityPolicyId;
