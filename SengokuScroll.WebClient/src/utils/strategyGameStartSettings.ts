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
    characterSharedVision: true,
    showAllyIntel: false,
    instantEventMessages: true,
  },
  Normal: {
    fogMode: "Force",
    intelMode: "ForceIntel",
    controlMode: "DirectiveOnly",
    allySharedVision: false,
    characterSharedVision: false,
    showAllyIntel: false,
    instantEventMessages: false,
  },
  Hard: {
    fogMode: "Character",
    intelMode: "ForceIntel",
    controlMode: "DirectiveOnly",
    allySharedVision: false,
    characterSharedVision: false,
    showAllyIntel: false,
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
    a.characterSharedVision === b.characterSharedVision &&
    a.showAllyIntel === b.showAllyIntel &&
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

/** 角色视野下控制模式固定为「仅角色」，同盟共享视野强制关闭。 */
export function enforceCharacterFogControl(options: GameStartOptionsState): boolean {
  if (options.fogMode !== "Character") return false;

  let changed = false;
  if (options.controlMode !== "DirectiveOnly") {
    options.controlMode = "DirectiveOnly";
    changed = true;
  }
  if (options.allySharedVision) {
    options.allySharedVision = false;
    changed = true;
  }
  if (options.characterSharedVision) {
    options.characterSharedVision = false;
    changed = true;
  }
  return changed;
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

/** 路由 state 键：仅「开局设置」确认后携带，刷新页面不会保留。 */
export const GAME_START_NAV_STATE_KEY = "gameStartSettings";

export function buildGameStartNavigationState(
  settings: GameStartSettings,
): Record<string, GameStartSettings> {
  return { [GAME_START_NAV_STATE_KEY]: settings };
}

/** 读取并消费导航传入的开局设置（刷新后为空，不会自动开局）。 */
export function takeGameStartSettingsFromNavigation(): GameStartSettings | null {
  const state = history.state as Record<string, unknown> | null | undefined;
  const raw = state?.[GAME_START_NAV_STATE_KEY];
  if (!raw || typeof raw !== "object") return null;

  const settings = raw as GameStartSettings;
  if (!settings.scenarioId || !settings.difficulty || !settings.customStartOptions) return null;
  return settings;
}
