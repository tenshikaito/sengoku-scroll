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

export function matchesIntelRealmFilter(
  forceId: number,
  playerForceId: number,
  forces: readonly StrategyForceState[],
  mode: IntelRealmFilterMode
): boolean {
  if (mode === "all") return true;
  if (mode === "homeOnly") return forceId === playerForceId;
  return isPlayerRealmForce(forceId, playerForceId, forces);
}
