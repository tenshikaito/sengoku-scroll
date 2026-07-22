import type { StrategyCharacterSummaryState, StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";
import { personLocationLabel } from "@/intelDisplay/PersonLocationBehavior";

export abstract class PersonForceStatusCommandTargetBehavior {
  abstract readonly forceStatus: string;

  abstract resolve(
    worldState: StrategyWorldState,
    character: StrategyCharacterSummaryState,
  ): string;
}

class TaskPersonForceStatusCommandTargetBehavior extends PersonForceStatusCommandTargetBehavior {
  readonly forceStatus = "Task";

  resolve(): string {
    return "任务中";
  }
}

class UnitActionPersonForceStatusCommandTargetBehavior extends PersonForceStatusCommandTargetBehavior {
  readonly forceStatus = "UnitAction";

  resolve(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): string {
    return personLocationLabel(worldState, character);
  }
}

class PrisonerPersonForceStatusCommandTargetBehavior extends PersonForceStatusCommandTargetBehavior {
  readonly forceStatus = "Prisoner";

  resolve(): string {
    return "—";
  }
}

const COMMAND_TARGET_BEHAVIORS: PersonForceStatusCommandTargetBehavior[] = [
  new TaskPersonForceStatusCommandTargetBehavior(),
  new UnitActionPersonForceStatusCommandTargetBehavior(),
  new PrisonerPersonForceStatusCommandTargetBehavior(),
];

export function personCommandTarget(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
): string {
  const behavior = COMMAND_TARGET_BEHAVIORS.find((b) => b.forceStatus === character.forceStatus);
  if (behavior) return behavior.resolve(worldState, character);
  return "—";
}

export abstract class PersonRoleLabelBehavior {
  abstract   matches(
    _worldState: StrategyWorldState,
    character: StrategyCharacterSummaryState,
    stronghold?: StrategyStrongholdState,
  ): boolean;

  abstract readonly label: string;
}

class PlayerLordPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "当主";

  matches(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    const name = character.name?.trim();
    return Boolean(
      name
      && character.forceId === worldState.playerForceId
      && name === worldState.lord.name?.trim(),
    );
  }
}

class StrongholdLordPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "领主";

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState, stronghold?: StrategyStrongholdState): boolean {
    if (!stronghold) return false;
    const name = character.name?.trim();
    return Boolean(
      name
      && (stronghold.lordId === character.id || stronghold.lordName?.trim() === name),
    );
  }

  resolveLabel(stronghold?: StrategyStrongholdState): string {
    return stronghold?.isLordResidence ? "当主" : "领主";
  }
}

class MayorPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "代官";

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState, stronghold?: StrategyStrongholdState): boolean {
    const name = character.name?.trim();
    return Boolean(name && stronghold?.mayorName?.trim() === name);
  }
}

class UnitLocationPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "将";

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    return character.locationType === "Unit";
  }
}

class PrisonerPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "俘虏";

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    return character.forceStatus === "Prisoner";
  }
}

class TaskPersonRoleBehavior extends PersonRoleLabelBehavior {
  readonly label = "奉行";

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    return character.forceStatus === "Task";
  }
}

const PERSON_ROLE_BEHAVIORS: PersonRoleLabelBehavior[] = [
  new PlayerLordPersonRoleBehavior(),
  new StrongholdLordPersonRoleBehavior(),
  new MayorPersonRoleBehavior(),
  new UnitLocationPersonRoleBehavior(),
  new PrisonerPersonRoleBehavior(),
  new TaskPersonRoleBehavior(),
];

export function personRoleLabel(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
): string {
  const name = character.name?.trim();
  if (!name) return "—";

  const strongholdId = character.strongholdId ?? 0;
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);

  for (const behavior of PERSON_ROLE_BEHAVIORS) {
    if (!behavior.matches(worldState, character, stronghold)) continue;
    if (behavior instanceof StrongholdLordPersonRoleBehavior) {
      return behavior.resolveLabel(stronghold);
    }
    return behavior.label;
  }

  return "—";
}
