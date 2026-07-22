import type { GameStartOptionsState } from "@/api/strategyTypes";
import type {
  FogModeId,
  GameStartOptionUiRules,
} from "./types";

/** 地图视野模式行为（UI 可见性 + 选项约束 + 运行时迷雾开关）。 */
export abstract class FogModeBehavior {
  abstract readonly mode: FogModeId;
  abstract readonly fogDisabled: boolean;
  abstract readonly uiRules: Omit<
    GameStartOptionUiRules,
    "showAllyIntel" | "showControlMode" | "controlModeLockedHint"
  > & {
    showControlMode: boolean;
    controlModeLockedHint: string | null;
  };

  /** 修正与当前迷雾模式冲突的开局选项；返回是否有变更。 */
  applyConstraints(_options: GameStartOptionsState): boolean {
    return false;
  }
}

export class NoFogModeBehavior extends FogModeBehavior {
  readonly mode = "None" as const;
  readonly fogDisabled = true;
  readonly uiRules = {
    showAllySharedVision: false,
    showCharacterSharedVision: false,
    showControlMode: true,
    controlModeLockedHint: null,
  };
}

export class ForceFogModeBehavior extends FogModeBehavior {
  readonly mode = "Force" as const;
  readonly fogDisabled = false;
  readonly uiRules = {
    showAllySharedVision: true,
    showCharacterSharedVision: true,
    showControlMode: true,
    controlModeLockedHint: null,
  };
}

export class CharacterFogModeBehavior extends FogModeBehavior {
  readonly mode = "Character" as const;
  readonly fogDisabled = false;
  readonly uiRules = {
    showAllySharedVision: false,
    showCharacterSharedVision: false,
    showControlMode: false,
    controlModeLockedHint: "角色视野下固定为「仅角色」。",
  };

  override applyConstraints(options: GameStartOptionsState): boolean {
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
}

const FOG_BEHAVIORS: Record<FogModeId, FogModeBehavior> = {
  None: new NoFogModeBehavior(),
  Force: new ForceFogModeBehavior(),
  Character: new CharacterFogModeBehavior(),
};

export class FogModeBehaviorFactory {
  static create(mode: string | undefined | null): FogModeBehavior {
    switch (mode) {
      case "None":
        return FOG_BEHAVIORS.None;
      case "Character":
        return FOG_BEHAVIORS.Character;
      case "Force":
      default:
        return FOG_BEHAVIORS.Force;
    }
  }
}

export function applyAllFogConstraints(options: GameStartOptionsState): boolean {
  return FogModeBehaviorFactory.create(options.fogMode).applyConstraints(options);
}
