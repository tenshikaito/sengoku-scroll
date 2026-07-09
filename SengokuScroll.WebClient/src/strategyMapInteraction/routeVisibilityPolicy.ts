import type { StrategyUnitState } from "@/api/strategyTypes";

/**
 * 地图上「移动路径线」可见范围。
 * M2 默认仅自势力；难度/设置扩展见 docs/strategy-development-plan.md §6.6。
 */
export type RouteVisibilityPolicy =
  | "playerControlledOnly"
  | "playerAndAllies"
  | "allForces";

/** M2 默认：不显示敌/中立势力单位路径。 */
export const DEFAULT_ROUTE_VISIBILITY_POLICY: RouteVisibilityPolicy = "playerControlledOnly";

export interface RouteVisibilityContext {
  policy: RouteVisibilityPolicy;
  /** 玩家当前操控势力 Id（剧本加载后由 API 确定，M2 固定织田=1）。 */
  playerForceId: number;
  /** 同盟势力 Id；M3 外交实装前为空。 */
  allyForceIds?: readonly number[];
}

/** 单位路径是否应在地图上绘制。 */
export function isUnitRouteVisible(unit: StrategyUnitState, ctx: RouteVisibilityContext): boolean {
  switch (ctx.policy) {
    case "allForces":
      return true;
    case "playerAndAllies": {
      const allies = ctx.allyForceIds ?? [];
      return unit.forceId === ctx.playerForceId || allies.includes(unit.forceId);
    }
    case "playerControlledOnly":
    default:
      return unit.forceId === ctx.playerForceId;
  }
}

export function filterUnitsForRouteDisplay(
  units: readonly StrategyUnitState[],
  ctx: RouteVisibilityContext
): StrategyUnitState[] {
  return units.filter((u) => isUnitRouteVisible(u, ctx));
}
