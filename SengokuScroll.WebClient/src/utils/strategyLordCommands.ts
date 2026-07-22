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

/** 当主是否位于本家居城格（与后端 StrongholdDomesticRules.IsLordAtResidence 对齐）。 */
export function isLordAtResidence(worldState: StrategyWorldState): boolean {
  const residence = resolveLordResidenceStronghold(worldState);
  if (!residence) return false;

  const characterId = worldState.lord.characterId;
  const character = characterId
    ? worldState.characters?.find((c) => c.id === characterId)
    : null;

  if (character) {
    if (character.locationType === "Stronghold") {
      return character.strongholdId === residence.id;
    }

    if (character.locationType === "Unit") {
      const unitId = worldState.lord.unitId;
      const unit = unitId != null ? worldState.units.find((u) => u.id === unitId) : null;
      if (unit) {
        return unit.x === residence.x && unit.y === residence.y;
      }
    }

    if (character.locationType === "Map") {
      return worldState.lord.x === residence.x && worldState.lord.y === residence.y;
    }
  }

  return worldState.lord.x === residence.x && worldState.lord.y === residence.y;
}

export const LORD_AT_RESIDENCE_REQUIRED_TIP = "当主须在本家居城方可下达据点指令";

export const LORD_COMMAND_STRONGHOLD_TIP =
  "当主须驻留本家居城，或亲赴目标据点格，方可下达该据点指令";
