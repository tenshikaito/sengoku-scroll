import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { isInnerVassalRealmStronghold } from "@/utils/strategyPlayerCharacter";

/** 当主可否对本家直属据点发布方针（不含旗下内藩）。 */
export function canConfigureStrongholdGovernancePolicy(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (stronghold.forceId !== worldState.playerForceId) return false;
  const force = worldState.forces.find((f) => f.id === worldState.playerForceId);
  return force?.status !== "InnerVassal";
}

/** 方针按钮置灰时的说明。 */
export function governancePolicyBlockReason(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): string {
  if (isInnerVassalRealmStronghold(worldState, stronghold)) {
    return "内藩据点方针由内藩自行决定";
  }
  return "仅本家直属据点可设置方针";
}
