import type {
  StrategyCharacterSummaryState,
  StrategyStrongholdState,
  StrategyWorldState,
} from "@/api/strategyTypes";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";
import {
  hasStrongholdDomesticEspionageIntel,
  hasStrongholdMilitaryEspionageIntel,
  isForeignIntelRestricted,
  isRestrictedIntelMode,
} from "@/utils/strategyIntelDisplay";

/** 开局选项是否允许在情报对话框显示「调试模式」checkbox（与 checkbox 勾选态无关）。 */
export function isIntelDebugCheckboxVisible(worldState: StrategyWorldState): boolean {
  return worldState.startOptions?.intelDebugMode !== false;
}

function resolveCharacterStronghold(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
): StrategyStrongholdState | null {
  const strongholdId = character.strongholdId ?? 0;
  if (strongholdId > 0) {
    const byId = worldState.strongholds.find((item) => item.id === strongholdId);
    if (byId) return byId;
  }

  const strongholdName = character.strongholdName?.trim();
  if (strongholdName) {
    return worldState.strongholds.find((item) => item.name?.trim() === strongholdName) ?? null;
  }

  return null;
}

function hasStrongholdEspionageIntel(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (
    hasStrongholdDomesticEspionageIntel(stronghold)
    || hasStrongholdMilitaryEspionageIntel(stronghold)
  ) {
    return true;
  }

  return (
    worldState.espionageIntel?.some(
      (entry) => entry.targetKind === "Stronghold" && entry.targetId === stronghold.id,
    ) ?? false
  );
}

/** 人物是否应在情报系统中完整展示（仅情报对话框调试 checkbox 勾选时 bypass 刺探限制）。 */
export function isPersonIntelVisible(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
  intelDebugMode = false,
): boolean {
  if (intelDebugMode) return true;
  if (!isRestrictedIntelMode(worldState)) return true;

  if (isPlayerRealmForce(character.forceId, worldState.playerForceId, worldState.forces)) {
    return true;
  }

  const stronghold = resolveCharacterStronghold(worldState, character);
  if (!stronghold) return false;

  if (isPlayerRealmForce(stronghold.forceId, worldState.playerForceId, worldState.forces)) {
    return true;
  }

  if (!isForeignIntelRestricted(worldState, stronghold.forceId)) {
    return true;
  }

  return hasStrongholdEspionageIntel(worldState, stronghold);
}
