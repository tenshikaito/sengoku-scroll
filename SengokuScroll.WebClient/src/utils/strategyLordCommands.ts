import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";

/** 解析玩家当主居城据点。 */
export function resolveLordResidenceStronghold(
  worldState: StrategyWorldState,
): StrategyStrongholdState | null {
  const playerForceId = worldState.playerForceId;
  const force = worldState.forces.find((f) => f.id === playerForceId);
  if (force?.lordResidenceStrongholdId) {
    return worldState.strongholds.find((s) => s.id === force.lordResidenceStrongholdId) ?? null;
  }
  return (
    worldState.strongholds.find(
      (s) => s.forceId === playerForceId && s.isLordResidence,
    ) ?? null
  );
}

/** 当主是否位于本家居城格。 */
export function isLordAtResidence(worldState: StrategyWorldState): boolean {
  const residence = resolveLordResidenceStronghold(worldState);
  if (!residence) return false;
  const { lord } = worldState;
  return lord.x === residence.x && lord.y === residence.y;
}

export const LORD_AT_RESIDENCE_REQUIRED_TIP = "当主须在本家居城方可下达据点指令";
