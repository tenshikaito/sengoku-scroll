import type { StrategyCharacterSummaryState, StrategyStrongholdState, StrategyWorldState } from "@/api/strategyTypes";

export abstract class PersonLocationLabelBehavior {
  abstract readonly locationType: string;
  abstract resolve(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): string;
}

export abstract class PersonLocationResidenceBehavior {
  abstract readonly locationType: string;

  abstract isAtResidence(
    worldState: StrategyWorldState,
    character: StrategyCharacterSummaryState,
    residence: StrategyStrongholdState,
  ): boolean;
}

class StrongholdPersonLocationBehavior extends PersonLocationLabelBehavior {
  readonly locationType = "Stronghold";

  resolve(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): string {
    const strongholdId = character.strongholdId ?? 0;
    if (strongholdId <= 0) return "—";
    const sh = worldState.strongholds.find((s) => s.id === strongholdId);
    return sh?.name?.trim() || "—";
  }
}

class StrongholdPersonResidenceBehavior extends PersonLocationResidenceBehavior {
  readonly locationType = "Stronghold";

  isAtResidence(
    _worldState: StrategyWorldState,
    character: StrategyCharacterSummaryState,
    residence: StrategyStrongholdState,
  ): boolean {
    return character.strongholdId === residence.id;
  }
}

class UnitPersonLocationBehavior extends PersonLocationLabelBehavior {
  readonly locationType = "Unit";

  resolve(worldState: StrategyWorldState, character: StrategyCharacterSummaryState): string {
    const unit = worldState.units.find(
      (u) =>
        u.commanderId === character.id ||
        u.composition?.some((sub) => sub.commanderId === character.id),
    );
    return unit?.name?.trim() || "—";
  }
}

class UnitPersonResidenceBehavior extends PersonLocationResidenceBehavior {
  readonly locationType = "Unit";

  isAtResidence(
    worldState: StrategyWorldState,
    _character: StrategyCharacterSummaryState,
    residence: StrategyStrongholdState,
  ): boolean {
    const unitId = worldState.lord.unitId;
    const unit = unitId != null ? worldState.units.find((u) => u.id === unitId) : null;
    return unit != null && unit.x === residence.x && unit.y === residence.y;
  }
}

class MapPersonLocationBehavior extends PersonLocationLabelBehavior {
  readonly locationType = "Map";

  resolve(): string {
    return "地图";
  }
}

class MapPersonResidenceBehavior extends PersonLocationResidenceBehavior {
  readonly locationType = "Map";

  isAtResidence(
    worldState: StrategyWorldState,
    _character: StrategyCharacterSummaryState,
    residence: StrategyStrongholdState,
  ): boolean {
    return worldState.lord.x === residence.x && worldState.lord.y === residence.y;
  }
}

const PERSON_LOCATION_BEHAVIORS: PersonLocationLabelBehavior[] = [
  new StrongholdPersonLocationBehavior(),
  new UnitPersonLocationBehavior(),
  new MapPersonLocationBehavior(),
];

const PERSON_RESIDENCE_BEHAVIORS: PersonLocationResidenceBehavior[] = [
  new StrongholdPersonResidenceBehavior(),
  new UnitPersonResidenceBehavior(),
  new MapPersonResidenceBehavior(),
];

export function personLocationLabel(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
): string {
  const behavior = PERSON_LOCATION_BEHAVIORS.find((b) => b.locationType === character.locationType);
  if (behavior) return behavior.resolve(worldState, character);
  return character.locationType?.trim() ? character.locationType : "—";
}

export function isCharacterAtLordResidence(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
  residence: StrategyStrongholdState,
): boolean {
  const behavior = PERSON_RESIDENCE_BEHAVIORS.find((b) => b.locationType === character.locationType);
  return behavior?.isAtResidence(worldState, character, residence) ?? false;
}
