import type { GameStartOptionsState, StrategyWorldState } from "@/api/strategyTypes";

export type FogModeId = "None" | "Force" | "Character";
export type IntelModeId = "Full" | "ForceIntel";
export type ControlModeId = "FullDirect" | "DirectiveOnly";

export interface GameStartOptionUiRules {
  showAllySharedVision: boolean;
  showCharacterSharedVision: boolean;
  showAllyIntel: boolean;
  showControlMode: boolean;
  controlModeLockedHint: string | null;
}

export interface LordUnitControlContext {
  lordUnitId: number | null | undefined;
  lordCharacterId: number | null | undefined;
  lordX: number;
  lordY: number;
  lordCharacterLocationType?: string | null;
}

export interface ResolvedStartOptions extends GameStartOptionsState {
  difficulty?: string;
}

export function resolveOptionsFromWorldState(
  worldState: StrategyWorldState,
): ResolvedStartOptions {
  const start = worldState.startOptions;
  const vis = worldState.visibility;
  return {
    fogMode: start?.fogMode ?? vis?.fogMode ?? "Force",
    intelMode: start?.intelMode ?? vis?.intelMode ?? "ForceIntel",
    controlMode: start?.controlMode ?? vis?.controlMode ?? "DirectiveOnly",
    allySharedVision: start?.allySharedVision ?? vis?.allySharedVision ?? false,
    characterSharedVision:
      start?.characterSharedVision ?? vis?.characterSharedVision ?? false,
    showAllyIntel: start?.showAllyIntel ?? vis?.showAllyIntel ?? false,
    instantEventMessages:
      start?.instantEventMessages ?? vis?.instantEventMessages ?? false,
    difficulty: worldState.difficulty,
  };
}

export {
  normalizeFogMode,
  normalizeIntelMode,
  normalizeControlMode,
} from "./StartOptionNormalizeBehaviors";
