import type { GameStartOptionsState } from "@/api/strategyTypes";

export type StrategyDifficultyId = "Easy" | "Normal" | "Hard" | "Custom";

export type PresetDifficultyId = Exclude<StrategyDifficultyId, "Custom">;

export interface GameStartSettings {
  scenarioId: string;
  difficulty: StrategyDifficultyId;
  customStartOptions: GameStartOptionsState;
}

export const GAME_START_PRESETS: Record<PresetDifficultyId, GameStartOptionsState> = {
  Easy: {
    fogMode: "None",
    intelMode: "Full",
    controlMode: "FullDirect",
    allySharedVision: true,
    instantEventMessages: true,
  },
  Normal: {
    fogMode: "Force",
    intelMode: "ForceIntel",
    controlMode: "DirectiveOnly",
    allySharedVision: false,
    instantEventMessages: false,
  },
  Hard: {
    fogMode: "Character",
    intelMode: "ForceIntel",
    controlMode: "DirectiveOnly",
    allySharedVision: false,
    instantEventMessages: false,
  },
};

export function cloneGameStartOptions(options: GameStartOptionsState): GameStartOptionsState {
  return { ...options };
}

export function gameStartOptionsEqual(
  a: GameStartOptionsState,
  b: GameStartOptionsState,
): boolean {
  return (
    a.fogMode === b.fogMode &&
    a.intelMode === b.intelMode &&
    a.controlMode === b.controlMode &&
    a.allySharedVision === b.allySharedVision &&
    a.instantEventMessages === b.instantEventMessages
  );
}

/** 若选项与某一预设完全一致则返回该难度，否则为 Custom。 */
export function resolveDifficultyFromOptions(
  options: GameStartOptionsState,
): StrategyDifficultyId {
  for (const difficulty of ["Easy", "Normal", "Hard"] as const) {
    if (gameStartOptionsEqual(options, GAME_START_PRESETS[difficulty])) {
      return difficulty;
    }
  }
  return "Custom";
}

/** 角色视野下控制模式固定为「仅角色」。 */
export function enforceCharacterFogControl(options: GameStartOptionsState): boolean {
  if (options.fogMode === "Character" && options.controlMode !== "DirectiveOnly") {
    options.controlMode = "DirectiveOnly";
    return true;
  }
  return false;
}

const STORAGE_KEY = "sengoku.strategy.gameStartSettings";

export function readGameStartSettings(): GameStartSettings | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as GameStartSettings;
  } catch {
    return null;
  }
}

export function writeGameStartSettings(settings: GameStartSettings): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
}
