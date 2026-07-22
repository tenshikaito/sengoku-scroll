import type { StrategyWorldState } from "@/api/strategy";
import { isCharacterAtLordResidence } from "@/intelDisplay/PersonLocationBehavior";
import { resolvePlayerLordCharacterId } from "@/utils/strategyPlayerCharacter";

/** 解析玩家当主居城据点。 */
export function resolveLordResidenceStronghold(
  worldState: StrategyWorldState,
) {
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

/** 当主是否位于本家居城格（与后端 StrongholdDomesticRules.IsLordAtResidence 对齐）。 */
export function isLordAtResidence(worldState: StrategyWorldState): boolean {
  const residence = resolveLordResidenceStronghold(worldState);
  if (!residence) return false;

  const characterId = resolvePlayerLordCharacterId(worldState);
  const character = characterId
    ? worldState.characters?.find((c) => c.id === characterId)
    : null;

  if (character && isCharacterAtLordResidence(worldState, character, residence)) {
    return true;
  }

  return worldState.lord.x === residence.x && worldState.lord.y === residence.y;
}

export const LORD_AT_RESIDENCE_REQUIRED_TIP = "当主须在本家居城方可下达据点指令";

export const LORD_COMMAND_STRONGHOLD_TIP =
  "当主须驻留本家居城，或亲赴目标据点格，方可下达该据点指令";
