import type { StrategyUnitState } from "@/api/strategy";
import type { ControlModeId, LordUnitControlContext } from "./types";

/** 玩家微操范围行为。 */
export abstract class ControlModeBehavior {
  abstract readonly mode: ControlModeId;

  abstract allowsDirectUnitControl(
    unit: StrategyUnitState,
    playerForceId: number,
    lord: LordUnitControlContext,
  ): boolean;
}

export class FullDirectControlModeBehavior extends ControlModeBehavior {
  readonly mode = "FullDirect" as const;

  allowsDirectUnitControl(
    unit: StrategyUnitState,
    playerForceId: number,
    _lord: LordUnitControlContext,
  ): boolean {
    return unit.forceId === playerForceId;
  }
}

export class DirectiveOnlyControlModeBehavior extends ControlModeBehavior {
  readonly mode = "DirectiveOnly" as const;

  allowsDirectUnitControl(
    unit: StrategyUnitState,
    playerForceId: number,
    lord: LordUnitControlContext,
  ): boolean {
    if (unit.forceId !== playerForceId) return false;

    const lordUnitId = lord.lordUnitId;
    if (lordUnitId != null && lordUnitId > 0 && lordUnitId === unit.id) return true;

    if (
      lord.lordCharacterId != null
      && lord.lordCharacterId > 0
      && unit.commanderId === lord.lordCharacterId
    ) {
      return true;
    }

    return lord.lordX === unit.x && lord.lordY === unit.y;
  }
}

const CONTROL_BEHAVIORS: Record<ControlModeId, ControlModeBehavior> = {
  FullDirect: new FullDirectControlModeBehavior(),
  DirectiveOnly: new DirectiveOnlyControlModeBehavior(),
};

export class ControlModeBehaviorFactory {
  static create(mode: string | undefined | null): ControlModeBehavior {
    switch (mode) {
      case "FullDirect":
        return CONTROL_BEHAVIORS.FullDirect;
      case "DirectiveOnly":
      default:
        return CONTROL_BEHAVIORS.DirectiveOnly;
    }
  }
}
