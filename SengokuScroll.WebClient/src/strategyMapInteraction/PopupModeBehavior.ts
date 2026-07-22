import type { StrategyMapPopupMode } from "./types";

export type CornerHintMode = "moveSelect" | "attackSelect" | "mergeSelect" | "splitSelect";

export type MenuPopupMode = Exclude<
  StrategyMapPopupMode,
  "none" | "entityPicker" | CornerHintMode
>;

export abstract class MapPopupModeBehavior {
  abstract readonly mode: StrategyMapPopupMode;
  abstract readonly usesCorner: boolean;
  abstract readonly cornerHintMode: CornerHintMode | null;
  abstract readonly menuPopupMode: MenuPopupMode | null;
}

class NonePopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "none" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = null;
}

class EntityPickerPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "entityPicker" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = null;
}

class MoveSelectPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "moveSelect" as const;
  readonly usesCorner = true;
  readonly cornerHintMode = "moveSelect" as const;
  readonly menuPopupMode = null;
}

class AttackSelectPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "attackSelect" as const;
  readonly usesCorner = true;
  readonly cornerHintMode = "attackSelect" as const;
  readonly menuPopupMode = null;
}

class MergeSelectPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "mergeSelect" as const;
  readonly usesCorner = true;
  readonly cornerHintMode = "mergeSelect" as const;
  readonly menuPopupMode = null;
}

class SplitSelectPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "splitSelect" as const;
  readonly usesCorner = true;
  readonly cornerHintMode = "splitSelect" as const;
  readonly menuPopupMode = null;
}

class CommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "command" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "command" as const;
}

class ForeignCommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "foreignCommand" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "foreignCommand" as const;
}

class CharacterCommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "characterCommand" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "characterCommand" as const;
}

class StrongholdCommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "strongholdCommand" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "strongholdCommand" as const;
}

class ForeignStrongholdCommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "foreignStrongholdCommand" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "foreignStrongholdCommand" as const;
}

class ConvoyCommandPopupModeBehavior extends MapPopupModeBehavior {
  readonly mode = "convoyCommand" as const;
  readonly usesCorner = false;
  readonly cornerHintMode = null;
  readonly menuPopupMode = "convoyCommand" as const;
}

const POPUP_MODE_BEHAVIORS: MapPopupModeBehavior[] = [
  new NonePopupModeBehavior(),
  new EntityPickerPopupModeBehavior(),
  new MoveSelectPopupModeBehavior(),
  new AttackSelectPopupModeBehavior(),
  new MergeSelectPopupModeBehavior(),
  new SplitSelectPopupModeBehavior(),
  new CommandPopupModeBehavior(),
  new ForeignCommandPopupModeBehavior(),
  new CharacterCommandPopupModeBehavior(),
  new StrongholdCommandPopupModeBehavior(),
  new ForeignStrongholdCommandPopupModeBehavior(),
  new ConvoyCommandPopupModeBehavior(),
];

const DEFAULT_POPUP_MODE = new NonePopupModeBehavior();

export function resolveMapPopupModeBehavior(
  mode: StrategyMapPopupMode | string | null | undefined,
): MapPopupModeBehavior {
  return POPUP_MODE_BEHAVIORS.find((b) => b.mode === mode) ?? DEFAULT_POPUP_MODE;
}

export function popupUsesCorner(mode: StrategyMapPopupMode | string | null | undefined): boolean {
  return resolveMapPopupModeBehavior(mode).usesCorner;
}

export function resolveCornerHintMode(mode: StrategyMapPopupMode | string | null | undefined): CornerHintMode {
  return resolveMapPopupModeBehavior(mode).cornerHintMode ?? "moveSelect";
}

export function resolveMenuPopupMode(
  mode: StrategyMapPopupMode | string | null | undefined,
): MenuPopupMode | null {
  return resolveMapPopupModeBehavior(mode).menuPopupMode;
}

export interface PrimaryPopupEntityNameContext {
  lordName?: string | null;
  strongholdName?: string | null;
  fallbackName?: string | null;
}

export function resolvePrimaryPopupEntityName(
  mode: MenuPopupMode | null,
  ctx: PrimaryPopupEntityNameContext,
): string | undefined {
  switch (mode) {
    case "characterCommand":
      return ctx.lordName ?? "当主";
    case "strongholdCommand":
    case "foreignStrongholdCommand":
      return ctx.strongholdName ?? undefined;
    default:
      return ctx.fallbackName ?? undefined;
  }
}

export type IntelMainTabId = "force" | "stronghold" | "person";

export function resolveIntelMainTabForMenuPopup(mode: MenuPopupMode | null): IntelMainTabId | null {
  if (mode === "characterCommand") return "person";
  if (mode === "strongholdCommand" || mode === "foreignStrongholdCommand") return "stronghold";
  return null;
}
