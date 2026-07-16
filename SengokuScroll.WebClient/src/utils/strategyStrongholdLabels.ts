import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";

/** 是否「居城」：当主居城，或已任命领主（lordId>0）之据点。 */
export function isGovernanceResidence(
  stronghold: StrategyStrongholdState,
  worldState?: StrategyWorldState
): boolean {
  if (stronghold.isLordResidence) return true;
  if (stronghold.lordId > 0) return true;
  if (!worldState) return false;

  const force = worldState.forces.find((f) => f.id === stronghold.forceId);
  if (force && (force.lordResidenceStrongholdId ?? 0) > 0) {
    return stronghold.id === force.lordResidenceStrongholdId;
  }

  const residenceName = worldState.lord?.residenceStrongholdName?.trim();
  return Boolean(
    residenceName &&
      stronghold.forceId === worldState.playerForceId &&
      stronghold.name === residenceName
  );
}

/** 据点名称后缀：居城 / 直辖；虚构据点追加「·虚构」。 */
export function strongholdGovernanceBadge(
  stronghold: StrategyStrongholdState,
  worldState?: StrategyWorldState
): string {
  const base = isGovernanceResidence(stronghold, worldState) ? "居城" : "直辖";
  return stronghold.isHistorical === false ? `${base}·虚构` : base;
}

/** @deprecated 使用 isGovernanceResidence */
export const resolveIsLordResidence = isGovernanceResidence;
