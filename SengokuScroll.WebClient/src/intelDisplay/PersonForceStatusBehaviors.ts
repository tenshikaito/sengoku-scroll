import type { StrategyCharacterSummaryState, StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";
import { organizationRoleLabelAtIndex } from "@/intelDisplay/OrganizationRoleLabelBehavior";
import { findCharacterCityActor } from "@/utils/personCityActorLookup";
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
  abstract matches(
    worldState: StrategyWorldState,
    character: StrategyCharacterSummaryState,
    stronghold?: StrategyStrongholdState,
  ): boolean;

  resolveLabel(
    _worldState: StrategyWorldState,
    _character: StrategyCharacterSummaryState,
    _stronghold?: StrategyStrongholdState,
  ): string {
    return this.defaultLabel;
  }

  protected get defaultLabel(): string {
    return "—";
  }
}

class PlayerLordPersonRoleBehavior extends PersonRoleLabelBehavior {
  protected override get defaultLabel(): string {
    return "当主";
  }

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
  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState, stronghold?: StrategyStrongholdState): boolean {
    if (!stronghold) return false;
    const name = character.name?.trim();
    return Boolean(
      name
      && (stronghold.lordId === character.id || stronghold.lordName?.trim() === name),
    );
  }

  override resolveLabel(_worldState: StrategyWorldState, _character: StrategyCharacterSummaryState, stronghold?: StrategyStrongholdState): string {
    return stronghold?.isLordResidence ? "当主" : "领主";
  }
}

class MayorPersonRoleBehavior extends PersonRoleLabelBehavior {
  protected override get defaultLabel(): string {
    return "代官";
  }

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState, stronghold?: StrategyStrongholdState): boolean {
    const name = character.name?.trim();
    return Boolean(name && stronghold?.mayorName?.trim() === name);
  }
}

class OrganizationCityActorPersonRoleBehavior extends PersonRoleLabelBehavior {
  matches(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    const actor = findCharacterCityActor(worldState, character);
    return actor?.kind === "Merchant" || actor?.kind === "Religion";
  }

  override resolveLabel(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): string {
    const actor = findCharacterCityActor(worldState, character);
    if (!actor || (actor.kind !== "Merchant" && actor.kind !== "Religion")) return "—";
    const ids = actor.characterIds ?? [];
    const index = ids.indexOf(character.id);
    if (index >= 0) return organizationRoleLabelAtIndex(actor.kind, index);
    return organizationRoleLabelAtIndex(actor.kind, 0);
  }
}

class UnitLocationPersonRoleBehavior extends PersonRoleLabelBehavior {
  protected override get defaultLabel(): string {
    return "将";
  }

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    return character.locationType === "Unit";
  }
}

class PrisonerPersonRoleBehavior extends PersonRoleLabelBehavior {
  protected override get defaultLabel(): string {
    return "俘虏";
  }

  matches(_worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    return character.forceStatus === "Prisoner";
  }
}

class TaskPersonRoleBehavior extends PersonRoleLabelBehavior {
  protected override get defaultLabel(): string {
    return "奉行";
  }

  matches(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): boolean {
    if (character.forceStatus !== "Task") return false;
    const actor = findCharacterCityActor(worldState, character);
    return actor?.kind !== "Merchant" && actor?.kind !== "Religion";
  }
}

const PERSON_ROLE_BEHAVIORS: PersonRoleLabelBehavior[] = [
  new PlayerLordPersonRoleBehavior(),
  new StrongholdLordPersonRoleBehavior(),
  new MayorPersonRoleBehavior(),
  new OrganizationCityActorPersonRoleBehavior(),
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
    return behavior.resolveLabel(worldState, character, stronghold);
  }

  return "—";
}
