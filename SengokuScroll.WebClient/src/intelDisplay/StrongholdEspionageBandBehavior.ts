export type StrongholdEspionageBandSource = {
  espionageSoldiersBand?: string | null;
  espionageMoraleBand?: string | null;
  espionageTrainingBand?: string | null;
  espionagePopulationBand?: string | null;
  espionageFoodBand?: string | null;
  espionageMoneyBand?: string | null;
};

export abstract class StrongholdEspionageBandBehavior {
  abstract readonly label: string;
  abstract resolveBand(stronghold: StrongholdEspionageBandSource): string | null | undefined;
}

class SoldiersEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "兵力";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionageSoldiersBand;
  }
}

class MoraleEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "士气";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionageMoraleBand;
  }
}

class TrainingEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "训练度";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionageTrainingBand;
  }
}

class PopulationEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "人口";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionagePopulationBand;
  }
}

class ScaleEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "规模";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionagePopulationBand;
  }
}

class FoodEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "粮食";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionageFoodBand;
  }
}

class MoneyEspionageBandBehavior extends StrongholdEspionageBandBehavior {
  readonly label = "金钱";
  resolveBand(stronghold: StrongholdEspionageBandSource) {
    return stronghold.espionageMoneyBand;
  }
}

const STRONGHOLD_ESPIONAGE_BAND_BEHAVIORS: StrongholdEspionageBandBehavior[] = [
  new SoldiersEspionageBandBehavior(),
  new MoraleEspionageBandBehavior(),
  new TrainingEspionageBandBehavior(),
  new PopulationEspionageBandBehavior(),
  new ScaleEspionageBandBehavior(),
  new FoodEspionageBandBehavior(),
  new MoneyEspionageBandBehavior(),
];

export function resolveStrongholdEspionageBand(
  stronghold: StrongholdEspionageBandSource,
  label: string,
): string | null | undefined {
  return STRONGHOLD_ESPIONAGE_BAND_BEHAVIORS.find((b) => b.label === label)?.resolveBand(stronghold);
}
