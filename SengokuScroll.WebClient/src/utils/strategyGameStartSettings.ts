import type { GameStartOptionsState } from "@/api/strategyTypes";
import { enforceCharacterFogControl as applyFogConstraints } from "@/gameStartOptions/GameStartOptionsProfile";

export type StrategyDifficultyId = "Easy" | "Normal" | "Hard" | "Custom";

export type PresetDifficultyId = Exclude<StrategyDifficultyId, "Custom">;

/** 与难度预设无关；后期可通过暗门单独控制。 */
export const DEFAULT_INTEL_DEBUG_MODE = true;

/** 开局界面是否显示情报调试开关（后期改暗门触发）。 */
export const INTEL_DEBUG_START_OPTION_VISIBLE = true;

/** 参与难度匹配的开局选项（不含情报调试）。 */
export type DifficultyBoundStartOptions = Omit<GameStartOptionsState, "intelDebugMode">;

export interface GameStartSettings {
  scenarioId: string;
  difficulty: StrategyDifficultyId;
  customStartOptions: DifficultyBoundStartOptions;
  intelDebugMode: boolean;
}

export const GAME_START_PRESETS: Record<PresetDifficultyId, DifficultyBoundStartOptions> = {
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

export function cloneDifficultyStartOptions(
  options: DifficultyBoundStartOptions,
): DifficultyBoundStartOptions {
  return { ...options };
}

/** @deprecated 使用 cloneDifficultyStartOptions */
export function cloneGameStartOptions(options: DifficultyBoundStartOptions): DifficultyBoundStartOptions {
  return cloneDifficultyStartOptions(options);
}

export function gameStartOptionsEqual(
  a: DifficultyBoundStartOptions,
  b: DifficultyBoundStartOptions,
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

/** 组装 load API 所需的开局选项（调试模式独立传入）。 */
export function buildLoadStartOptions(settings: GameStartSettings): GameStartOptionsState {
  const intelDebugMode = settings.intelDebugMode;
  if (settings.difficulty === "Custom") {
    return {
      ...cloneDifficultyStartOptions(settings.customStartOptions),
      intelDebugMode,
    };
  }

  return {
    ...GAME_START_PRESETS[settings.difficulty],
    intelDebugMode,
  };
}

/** @deprecated 使用 buildLoadStartOptions */
export function resolveLoadCustomStartOptions(
  settings: GameStartSettings,
): GameStartOptionsState {
  return buildLoadStartOptions(settings);
}

/** 若选项与某一预设完全一致则返回该难度，否则为 Custom。 */
export function resolveDifficultyFromOptions(
  options: DifficultyBoundStartOptions,
): StrategyDifficultyId {
  for (const difficulty of ["Easy", "Normal", "Hard"] as const) {
    if (gameStartOptionsEqual(options, GAME_START_PRESETS[difficulty])) {
      return difficulty;
    }
  }
  return "Custom";
}

/** 角色视野下控制模式固定为「仅角色」，同盟共享视野强制关闭。 */
export function enforceCharacterFogControl(options: DifficultyBoundStartOptions): boolean {
  return applyFogConstraints(options as GameStartOptionsState);
}

const STORAGE_KEY = "sengoku.strategy.gameStartSettings";

export function readGameStartSettings(): GameStartSettings | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<GameStartSettings> & {
      customStartOptions?: Partial<GameStartOptionsState>;
    };
    if (!parsed.scenarioId || !parsed.customStartOptions) return null;

    const legacyDebug = parsed.customStartOptions.intelDebugMode;
    const { intelDebugMode: _ignored, ...customStartOptions } = parsed.customStartOptions;

    return {
      scenarioId: parsed.scenarioId,
      difficulty: parsed.difficulty ?? "Normal",
      customStartOptions: customStartOptions as DifficultyBoundStartOptions,
      intelDebugMode: parsed.intelDebugMode ?? legacyDebug ?? DEFAULT_INTEL_DEBUG_MODE,
    };
  } catch {
    return null;
  }
}

export function writeGameStartSettings(settings: GameStartSettings): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
}
