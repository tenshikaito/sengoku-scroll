import type { ControlModeId, FogModeId, IntelModeId } from "./types";

export abstract class FogModeNormalizeBehavior {
  abstract readonly mode: FogModeId;
}

class NoneFogModeNormalizeBehavior extends FogModeNormalizeBehavior {
  readonly mode = "None";
}

class ForceFogModeNormalizeBehavior extends FogModeNormalizeBehavior {
  readonly mode = "Force";
}

class CharacterFogModeNormalizeBehavior extends FogModeNormalizeBehavior {
  readonly mode = "Character";
}

const FOG_MODE_NORMALIZE_BEHAVIORS: FogModeNormalizeBehavior[] = [
  new NoneFogModeNormalizeBehavior(),
  new ForceFogModeNormalizeBehavior(),
  new CharacterFogModeNormalizeBehavior(),
];

const DEFAULT_FOG_MODE = new ForceFogModeNormalizeBehavior();

export function normalizeFogMode(value: string | undefined | null): FogModeId {
  return FOG_MODE_NORMALIZE_BEHAVIORS.find((b) => b.mode === value)?.mode ?? DEFAULT_FOG_MODE.mode;
}

export abstract class IntelModeNormalizeBehavior {
  abstract readonly mode: IntelModeId;
}

class FullIntelModeNormalizeBehavior extends IntelModeNormalizeBehavior {
  readonly mode = "Full";
}

class ForceIntelModeNormalizeBehavior extends IntelModeNormalizeBehavior {
  readonly mode = "ForceIntel";
}

const INTEL_MODE_NORMALIZE_BEHAVIORS: IntelModeNormalizeBehavior[] = [
  new FullIntelModeNormalizeBehavior(),
  new ForceIntelModeNormalizeBehavior(),
];

const DEFAULT_INTEL_MODE = new ForceIntelModeNormalizeBehavior();

export function normalizeIntelMode(value: string | undefined | null): IntelModeId {
  return INTEL_MODE_NORMALIZE_BEHAVIORS.find((b) => b.mode === value)?.mode ?? DEFAULT_INTEL_MODE.mode;
}

export abstract class ControlModeNormalizeBehavior {
  abstract readonly mode: ControlModeId;
}

class FullDirectControlModeNormalizeBehavior extends ControlModeNormalizeBehavior {
  readonly mode = "FullDirect";
}

class DirectiveOnlyControlModeNormalizeBehavior extends ControlModeNormalizeBehavior {
  readonly mode = "DirectiveOnly";
}

const CONTROL_MODE_NORMALIZE_BEHAVIORS: ControlModeNormalizeBehavior[] = [
  new FullDirectControlModeNormalizeBehavior(),
  new DirectiveOnlyControlModeNormalizeBehavior(),
];

const DEFAULT_CONTROL_MODE = new DirectiveOnlyControlModeNormalizeBehavior();

export function normalizeControlMode(value: string | undefined | null): ControlModeId {
  return CONTROL_MODE_NORMALIZE_BEHAVIORS.find((b) => b.mode === value)?.mode ?? DEFAULT_CONTROL_MODE.mode;
}
