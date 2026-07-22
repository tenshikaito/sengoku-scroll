import type { GameStartOptionsState, StrategyWorldState } from "@/api/strategyTypes";
import { ControlModeBehaviorFactory } from "./ControlModeBehavior";
import {
  applyAllFogConstraints,
  FogModeBehaviorFactory,
} from "./FogModeBehavior";
import { InstantEventBehaviorFactory } from "./InstantEventBehavior";
import { IntelModeBehaviorFactory } from "./IntelModeBehavior";
import type {
  ControlModeBehavior,
} from "./ControlModeBehavior";
import type { FogModeBehavior } from "./FogModeBehavior";
import type { InstantEventBehavior } from "./InstantEventBehavior";
import type { IntelModeBehavior } from "./IntelModeBehavior";
import type {
  GameStartOptionUiRules,
  LordUnitControlContext,
  ResolvedStartOptions,
} from "./types";
import { resolveOptionsFromWorldState } from "./types";

/** 本局开局选项的统一策略入口。 */
export class GameStartOptionsProfile {
  readonly options: GameStartOptionsState;
  readonly difficulty?: string;
  readonly fog: FogModeBehavior;
  readonly intel: IntelModeBehavior;
  readonly control: ControlModeBehavior;
  readonly instantEvents: InstantEventBehavior;

  constructor(options: GameStartOptionsState, difficulty?: string) {
    this.options = options;
    this.difficulty = difficulty;
    this.fog = FogModeBehaviorFactory.create(options.fogMode);
    this.intel = IntelModeBehaviorFactory.create(options.intelMode);
    this.control = ControlModeBehaviorFactory.create(options.controlMode);
    this.instantEvents = InstantEventBehaviorFactory.create(
      difficulty,
      options.instantEventMessages,
    );
  }

  static fromOptions(
    options: GameStartOptionsState,
    difficulty?: string,
  ): GameStartOptionsProfile {
    const cloned = { ...options };
    applyAllFogConstraints(cloned);
    return new GameStartOptionsProfile(cloned, difficulty);
  }

  static fromWorldState(worldState: StrategyWorldState): GameStartOptionsProfile {
    const resolved = resolveOptionsFromWorldState(worldState);
    const { difficulty, ...options } = resolved as ResolvedStartOptions;
    return GameStartOptionsProfile.fromOptions(options, difficulty);
  }

  get uiRules(): GameStartOptionUiRules {
    return {
      showAllySharedVision: this.fog.uiRules.showAllySharedVision,
      showCharacterSharedVision: this.fog.uiRules.showCharacterSharedVision,
      showAllyIntel: this.intel.showAllyIntelOption,
      showControlMode: this.fog.uiRules.showControlMode,
      controlModeLockedHint: this.fog.uiRules.controlModeLockedHint,
    };
  }

  fogDisabled(): boolean {
    return this.fog.fogDisabled;
  }

  isCharacterFogMode(): boolean {
    return this.fog.mode === "Character" || this.difficulty === "Hard";
  }

  isRestrictedIntelMode(): boolean {
    return this.intel.restricted;
  }

  isForeignIntelRestricted(worldState: StrategyWorldState, forceId: number): boolean {
    return this.intel.isForeignIntelRestricted(worldState, forceId);
  }

  allowsDirectUnitControl(
    unit: Parameters<ControlModeBehavior["allowsDirectUnitControl"]>[0],
    playerForceId: number,
    lord: LordUnitControlContext,
  ): boolean {
    return this.control.allowsDirectUnitControl(unit, playerForceId, lord);
  }

  shouldShowInstantEventSummary(): boolean {
    return this.instantEvents.enabled;
  }

  applyConstraints(): boolean {
    return this.fog.applyConstraints(this.options);
  }
}

export function enforceCharacterFogControl(options: GameStartOptionsState): boolean {
  return applyAllFogConstraints(options);
}

export function resolveGameStartOptionUiRules(
  options: GameStartOptionsState,
): GameStartOptionUiRules {
  return GameStartOptionsProfile.fromOptions(options).uiRules;
}
