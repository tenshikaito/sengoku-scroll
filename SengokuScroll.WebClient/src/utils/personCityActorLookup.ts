import type {
  StrategyCharacterSummaryState,
  StrategyStrongholdCityActorState,
  StrategyWorldState,
} from "@/api/strategyTypes";

export function findCharacterCityActor(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): StrategyStrongholdCityActorState | null {
  for (const stronghold of worldState.strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      if (actor.characterIds?.includes(character.id)) {
        return actor;
      }
    }
  }

  const strongholdId = character.strongholdId;
  if (strongholdId != null) {
    const stronghold = worldState.strongholds.find((item) => item.id === strongholdId);
    for (const actor of stronghold?.cityActors ?? []) {
      if (actor.characterIds?.includes(character.id)) {
        return actor;
      }
    }
  }

  return null;
}

export function findCharacterCityActorStrongholdName(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  for (const stronghold of worldState.strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      if (actor.characterIds?.includes(character.id)) {
        return stronghold.name?.trim() || "—";
      }
    }
  }

  const strongholdId = character.strongholdId ?? 0;
  if (strongholdId <= 0) return "—";
  const sh = worldState.strongholds.find((s) => s.id === strongholdId);
  return sh?.name?.trim() || "—";
}
