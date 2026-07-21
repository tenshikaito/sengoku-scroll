import type {
  MapPoint,
  StrategyCharacterSummaryState,
  StrategyMasterDataEntry,
  StrategyMasterDataSnapshot,
  StrategyDefenseFacilityState,
  StrategyForceState,
  StrategyLordState,
  StrategyMapCharacterState,
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategySubUnitState,
  StrategyInTransitSupply,
  StrategyUnitState,
  StrategyWorldState,
  StrategyBattlefieldState,
  StrategyBattlefieldParticipant,
  StrategyVisibilityState,
  GameStartOptionsState,
  StrategyEspionageIntelEntry,
} from "@/api/strategyTypes";

function pick(obj: Record<string, unknown>, camel: string, pascal: string): unknown {
  return obj[camel] ?? obj[pascal];
}

export function safeInt(value: unknown, fallback = 0): number {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function optionalString(value: unknown): string | null {
  if (value == null) return null;
  const s = String(value).trim();
  if (!s || s === "undefined" || s === "null") return null;
  return s;
}

function requiredString(value: unknown, fallback: string): string {
  return optionalString(value) ?? fallback;
}

function normalizeMapPoint(raw: unknown): MapPoint {
  const p = (raw ?? {}) as Record<string, unknown>;
  return { x: safeInt(pick(p, "x", "X")), y: safeInt(pick(p, "y", "Y")) };
}

function normalizeLord(raw: unknown): StrategyLordState {
  const l = (raw ?? {}) as Record<string, unknown>;
  const unitIdRaw = pick(l, "unitId", "UnitId");
  return {
    name: requiredString(pick(l, "name", "Name"), "当主"),
    unitId: unitIdRaw == null ? null : safeInt(unitIdRaw, 0) || null,
    x: safeInt(pick(l, "x", "X")),
    y: safeInt(pick(l, "y", "Y")),
    residenceStrongholdName: optionalString(
      pick(l, "residenceStrongholdName", "ResidenceStrongholdName")
    ),
  };
}

function normalizeSubUnit(raw: unknown): StrategySubUnitState {
  const s = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(s, "id", "Id"));
  return {
    id,
    typeId: safeInt(pick(s, "typeId", "TypeId"), 1),
    typeName: requiredString(pick(s, "typeName", "TypeName"), `兵种 #${id}`),
    soldiers: safeInt(pick(s, "soldiers", "Soldiers")),
    ratioPercent: safeInt(pick(s, "ratioPercent", "RatioPercent")),
    commanderId:
      pick(s, "commanderId", "CommanderId") == null
        ? null
        : safeInt(pick(s, "commanderId", "CommanderId"), 0) || null,
    commanderName: optionalString(pick(s, "commanderName", "CommanderName")),
  };
}

function normalizeInTransitSupply(raw: unknown): StrategyInTransitSupply {
  const s = (raw ?? {}) as Record<string, unknown>;
  return {
    convoyId: safeInt(pick(s, "convoyId", "ConvoyId")),
    cargoFoodGo: safeInt(pick(s, "cargoFoodGo", "CargoFoodGo")),
    estimatedDays: safeInt(pick(s, "estimatedDays", "EstimatedDays"), 1),
    isDeceived: Boolean(pick(s, "isDeceived", "IsDeceived")),
    originStrongholdName: optionalString(pick(s, "originStrongholdName", "OriginStrongholdName")),
  };
}

function normalizeUnit(raw: unknown, lord: StrategyLordState): StrategyUnitState {
  const u = raw as Record<string, unknown>;
  const id = safeInt(pick(u, "id", "Id"));
  const x = safeInt(pick(u, "x", "X"));
  const y = safeInt(pick(u, "y", "Y"));

  let commanderName = optionalString(pick(u, "commanderName", "CommanderName"));
  if (!commanderName && lord.unitId != null && lord.unitId === id) {
    commanderName = lord.name;
  }

  const routeRaw = pick(u, "route", "Route");
  const route = Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [];

  return {
    id,
    name: requiredString(pick(u, "name", "Name"), `部队 #${id}`),
    forceId: safeInt(pick(u, "forceId", "ForceId"), 1),
    x,
    y,
    soldiers: safeInt(pick(u, "soldiers", "Soldiers")),
    food: safeInt(pick(u, "food", "Food")),
    ap: safeInt(pick(u, "ap", "Ap"), safeInt(pick(u, "movement", "Movement"), 10)),
    movement: safeInt(pick(u, "movement", "Movement"), 10),
    status: requiredString(pick(u, "status", "Status"), "Waiting"),
    directive: requiredString(pick(u, "directive", "Directive"), "Move"),
    stance: requiredString(pick(u, "stance", "Stance"), "Normal"),
    siegeMode: requiredString(pick(u, "siegeMode", "SiegeMode"), "None"),
    directiveTargetId: safeInt(pick(u, "directiveTargetId", "DirectiveTargetId")),
    targetStrongholdName: optionalString(pick(u, "targetStrongholdName", "TargetStrongholdName")),
    targetUnitId: safeInt(pick(u, "targetUnitId", "TargetUnitId")),
    targetUnitName: optionalString(pick(u, "targetUnitName", "TargetUnitName")),
    battlefieldId: safeInt(pick(u, "battlefieldId", "BattlefieldId"), 0) || undefined,
    route,
    commanderName,
    commanderId: pick(u, "commanderId", "CommanderId") == null
      ? null
      : safeInt(pick(u, "commanderId", "CommanderId"), 0) || null,
    morale: safeInt(pick(u, "morale", "Morale"), 75),
    training: safeInt(pick(u, "training", "Training"), 70),
    cultureName: requiredString(pick(u, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(u, "religionName", "ReligionName"), "神道教"),
    money: safeInt(pick(u, "money", "Money")),
    composition: Array.isArray(pick(u, "composition", "Composition"))
      ? (pick(u, "composition", "Composition") as unknown[]).map(normalizeSubUnit)
      : [],
    supplyStatus: requiredString(pick(u, "supplyStatus", "SupplyStatus"), "Sufficient"),
    foodDaysRemaining: safeInt(pick(u, "foodDaysRemaining", "FoodDaysRemaining")),
    inTransitSupplies: Array.isArray(pick(u, "inTransitSupplies", "InTransitSupplies"))
      ? (pick(u, "inTransitSupplies", "InTransitSupplies") as unknown[]).map(normalizeInTransitSupply)
      : [],
    mapVisible: pick(u, "mapVisible", "MapVisible") === false ? false : true,
    soldiersDisplay: optionalString(pick(u, "soldiersDisplay", "SoldiersDisplay")),
    moraleBand: optionalString(pick(u, "moraleBand", "MoraleBand")),
    trainingBand: optionalString(pick(u, "trainingBand", "TrainingBand")),
  };
}

function normalizeRosterUnit(raw: unknown) {
  const u = raw as Record<string, unknown>;
  return {
    id: safeInt(pick(u, "id", "Id")),
    name: requiredString(pick(u, "name", "Name"), "部队"),
    forceId: safeInt(pick(u, "forceId", "ForceId"), 1),
    x: safeInt(pick(u, "x", "X")),
    y: safeInt(pick(u, "y", "Y")),
    soldiers: safeInt(pick(u, "soldiers", "Soldiers")),
    status: requiredString(pick(u, "status", "Status"), "Waiting"),
    directive: requiredString(pick(u, "directive", "Directive"), "Move"),
    ap: safeInt(pick(u, "ap", "Ap"), 5),
    supplyStatus: requiredString(pick(u, "supplyStatus", "SupplyStatus"), "Sufficient"),
    commanderName: optionalString(pick(u, "commanderName", "CommanderName")),
    offMap: pick(u, "offMap", "OffMap") !== false,
  };
}

function normalizeStronghold(
  raw: unknown,
  lord: StrategyLordState,
  playerForceId: number
): StrategyStrongholdState {
  const s = raw as Record<string, unknown>;
  const id = safeInt(pick(s, "id", "Id"));
  const x = safeInt(pick(s, "x", "X"));
  const y = safeInt(pick(s, "y", "Y"));
  const forceId = safeInt(pick(s, "forceId", "ForceId"), 1);
  const lordId = safeInt(pick(s, "lordId", "LordId"));
  const isDirectRule =
    pick(s, "isDirectRule", "IsDirectRule") == null
      ? lordId === 0
      : Boolean(pick(s, "isDirectRule", "IsDirectRule"));

  let lordName = optionalString(pick(s, "lordName", "LordName"));
  if (!lordName) {
    lordName = isDirectRule
      ? forceId === playerForceId
        ? lord.name
        : "当主"
      : `领主 #${lordId}`;
  }

  return {
    id,
    name: requiredString(pick(s, "name", "Name"), `据点 #${id}`),
    typeId: safeInt(pick(s, "typeId", "TypeId"), 1),
    typeName: requiredString(pick(s, "typeName", "TypeName"), "平城"),
    forceId,
    x,
    y,
    food: safeInt(pick(s, "food", "Food")),
    population: safeInt(pick(s, "population", "Population")),
    stability: safeInt(pick(s, "stability", "Stability"), 50),
    popularFeelings: safeInt(pick(s, "popularFeelings", "PopularFeelings"), 50),
    isLordResidence: Boolean(pick(s, "isLordResidence", "IsLordResidence")),
    lordId,
    isDirectRule,
    lordName,
    mayorName: optionalString(pick(s, "mayorName", "MayorName")),
    morale: safeInt(pick(s, "morale", "Morale"), 80),
    training: safeInt(pick(s, "training", "Training"), 65),
    cultureName: requiredString(pick(s, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(s, "religionName", "ReligionName"), "神道教"),
    money: safeInt(pick(s, "money", "Money")),
    garrisonSoldiers: safeInt(pick(s, "garrisonSoldiers", "GarrisonSoldiers")),
    garrisonWounded: safeInt(pick(s, "garrisonWounded", "GarrisonWounded"), 0),
    pollTaxRate: safeInt(pick(s, "pollTaxRate", "PollTaxRate"), 10),
    agricultureTaxRate: safeInt(pick(s, "agricultureTaxRate", "AgricultureTaxRate"), 25),
    commerceTaxRate: safeInt(pick(s, "commerceTaxRate", "CommerceTaxRate"), 12),
    tariffTaxRate: safeInt(pick(s, "tariffTaxRate", "TariffTaxRate"), 8),
    isHistorical: pick(s, "isHistorical", "IsHistorical") !== false,
    defense: safeInt(pick(s, "defense", "Defense")),
    defenseFacilities: normalizeDefenseFacilities(pick(s, "defenseFacilities", "DefenseFacilities")),
    siegeThreat: optionalString(pick(s, "siegeThreat", "SiegeThreat")) ?? null,
    visibilityTier: optionalString(pick(s, "visibilityTier", "VisibilityTier")),
    espionageSoldiersBand: optionalString(pick(s, "espionageSoldiersBand", "EspionageSoldiersBand")),
    espionageMoraleBand: optionalString(pick(s, "espionageMoraleBand", "EspionageMoraleBand")),
    espionageTrainingBand: optionalString(pick(s, "espionageTrainingBand", "EspionageTrainingBand")),
    espionagePopulationBand: optionalString(pick(s, "espionagePopulationBand", "EspionagePopulationBand")),
    espionageFoodBand: optionalString(pick(s, "espionageFoodBand", "EspionageFoodBand")),
    espionageMoneyBand: optionalString(pick(s, "espionageMoneyBand", "EspionageMoneyBand")),
  };
}

function normalizeEspionageIntel(raw: unknown): StrategyEspionageIntelEntry {
  const row = (raw ?? {}) as Record<string, unknown>;
  return {
    targetKind: requiredString(pick(row, "targetKind", "TargetKind"), "Stronghold"),
    targetId: safeInt(pick(row, "targetId", "TargetId")),
    scope: requiredString(pick(row, "scope", "Scope"), "Both"),
    precision: requiredString(pick(row, "precision", "Precision"), "Fuzzy"),
    expiresYear: safeInt(pick(row, "expiresYear", "ExpiresYear")),
    expiresMonth: safeInt(pick(row, "expiresMonth", "ExpiresMonth"), 1),
    expiresDay: safeInt(pick(row, "expiresDay", "ExpiresDay"), 1),
  };
}

function normalizeVisibility(raw: unknown): StrategyVisibilityState | undefined {
  if (!raw || typeof raw !== "object") return undefined;
  const v = raw as Record<string, unknown>;
  const cellsRaw = pick(v, "visibleCells", "VisibleCells");
  const bitsRaw = pick(v, "exploredBits", "ExploredBits");
  const knownRaw = pick(v, "knownStrongholdIds", "KnownStrongholdIds");
  return {
    fogMode: requiredString(pick(v, "fogMode", "FogMode"), "Force"),
    intelMode: requiredString(pick(v, "intelMode", "IntelMode"), "ForceIntel"),
    controlMode: requiredString(pick(v, "controlMode", "ControlMode"), "DirectiveOnly"),
    instantEventMessages: pick(v, "instantEventMessages", "InstantEventMessages") === true,
    allySharedVision: pick(v, "allySharedVision", "AllySharedVision") === true,
    mapWidth: safeInt(pick(v, "mapWidth", "MapWidth"), 0),
    mapHeight: safeInt(pick(v, "mapHeight", "MapHeight"), 0),
    exploredBits: Array.isArray(bitsRaw) ? bitsRaw.map((b) => safeInt(b)) : [],
    visibleCells: Array.isArray(cellsRaw) ? cellsRaw.map(normalizeMapPoint) : [],
    knownStrongholdIds: Array.isArray(knownRaw) ? knownRaw.map((id) => safeInt(id)) : [],
  };
}

function normalizeStartOptions(raw: unknown): GameStartOptionsState | undefined {
  if (!raw || typeof raw !== "object") return undefined;
  const o = raw as Record<string, unknown>;
  return {
    fogMode: requiredString(pick(o, "fogMode", "FogMode"), "Force"),
    intelMode: requiredString(pick(o, "intelMode", "IntelMode"), "ForceIntel"),
    controlMode: requiredString(pick(o, "controlMode", "ControlMode"), "DirectiveOnly"),
    allySharedVision: pick(o, "allySharedVision", "AllySharedVision") === true,
    instantEventMessages: pick(o, "instantEventMessages", "InstantEventMessages") === true,
  };
}

function normalizeDefenseFacilities(raw: unknown): StrategyDefenseFacilityState[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const f = item as Record<string, unknown>;
    const typeId = safeInt(pick(f, "typeId", "TypeId"));
    return {
      typeId,
      name: requiredString(pick(f, "name", "Name"), `设施 #${typeId}`),
      category: requiredString(pick(f, "category", "Category"), "Defender"),
      level: safeInt(pick(f, "level", "Level"), 1),
      defense: safeInt(pick(f, "defense", "Defense")),
    };
  });
}

function normalizeBattlefieldParticipant(raw: unknown): StrategyBattlefieldParticipant {
  const row = (raw ?? {}) as Record<string, unknown>;
  const forceId = safeInt(pick(row, "forceId", "ForceId"));
  return {
    forceId,
    forceName: requiredString(pick(row, "forceName", "ForceName"), `势力 #${forceId}`),
    soldiers: safeInt(pick(row, "soldiers", "Soldiers")),
    morale: safeInt(pick(row, "morale", "Morale")),
    money: safeInt(pick(row, "money", "Money")),
    food: safeInt(pick(row, "food", "Food")),
  };
}

function normalizeBattlefield(raw: unknown): StrategyBattlefieldState {
  const row = (raw ?? {}) as Record<string, unknown>;
  const unitIdsRaw = pick(row, "unitIds", "UnitIds");
  const participantsRaw = pick(row, "participants", "Participants");
  const soldierTotal = safeInt(pick(row, "soldierTotal", "SoldierTotal"));
  const aggressorRaw = pick(row, "aggressorSoldierTotal", "AggressorSoldierTotal");
  return {
    id: safeInt(pick(row, "id", "Id")),
    x: safeInt(pick(row, "x", "X")),
    y: safeInt(pick(row, "y", "Y")),
    kind: requiredString(pick(row, "kind", "Kind"), "Field"),
    standoffDays: safeInt(pick(row, "standoffDays", "StandoffDays")),
    soldierTotal,
    aggressorSoldierTotal: safeInt(aggressorRaw, soldierTotal),
    participants: Array.isArray(participantsRaw)
      ? participantsRaw.map(normalizeBattlefieldParticipant)
      : [],
    unitIds: Array.isArray(unitIdsRaw) ? unitIdsRaw.map((id) => safeInt(id)) : [],
  };
}

function normalizeConvoy(raw: unknown): StrategySupplyConvoyState {
  const c = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(c, "id", "Id"));
  const food = safeInt(pick(c, "food", "Food"), safeInt(pick(c, "cargoFoodGo", "CargoFoodGo")));
  const routeRaw = pick(c, "route", "Route");
  return {
    id,
    name: requiredString(pick(c, "name", "Name"), `粮运队 #${id}`),
    forceId: safeInt(pick(c, "forceId", "ForceId"), 1),
    x: safeInt(pick(c, "x", "X")),
    y: safeInt(pick(c, "y", "Y")),
    isMilitary: false,
    commanderName: optionalString(pick(c, "commanderName", "CommanderName")),
    commanderId:
      pick(c, "commanderId", "CommanderId") == null
        ? null
        : safeInt(pick(c, "commanderId", "CommanderId"), 0) || null,
    soldiers: safeInt(pick(c, "soldiers", "Soldiers")),
    porterCount: safeInt(pick(c, "porterCount", "PorterCount")),
    escortSoldierCount: safeInt(pick(c, "escortSoldierCount", "EscortSoldierCount")),
    food,
    cargoFoodGo: safeInt(pick(c, "cargoFoodGo", "CargoFoodGo"), food),
    ap: safeInt(pick(c, "ap", "Ap")),
    movement: safeInt(pick(c, "movement", "Movement"), 4),
    status: requiredString(pick(c, "status", "Status"), "Moving"),
    directive: requiredString(pick(c, "directive", "Directive"), "Support"),
    route: Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [],
    morale: safeInt(pick(c, "morale", "Morale"), 75),
    training: safeInt(pick(c, "training", "Training"), 65),
    cultureName: requiredString(pick(c, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(c, "religionName", "ReligionName"), "神道教"),
    money: safeInt(pick(c, "money", "Money")),
    targetUnitId: safeInt(pick(c, "targetUnitId", "TargetUnitId")),
    targetUnitName: optionalString(pick(c, "targetUnitName", "TargetUnitName")),
    originStrongholdId: safeInt(pick(c, "originStrongholdId", "OriginStrongholdId")),
    originStrongholdName: optionalString(pick(c, "originStrongholdName", "OriginStrongholdName")),
    isReturningToOrigin: Boolean(pick(c, "isReturningToOrigin", "IsReturningToOrigin")),
  };
}

function normalizeMessenger(raw: unknown): StrategyMessengerState {
  const m = (raw ?? {}) as Record<string, unknown>;
  const id = safeInt(pick(m, "id", "Id"));
  const routeRaw = pick(m, "route", "Route");
  const pending = pick(m, "pendingDirective", "PendingDirective");
  const courierCount = safeInt(pick(m, "courierCount", "CourierCount"), 2);
  const escortSoldierCount = safeInt(pick(m, "escortSoldierCount", "EscortSoldierCount"), 8);
  const soldiersRaw = pick(m, "soldiers", "Soldiers");
  return {
    id,
    name: requiredString(pick(m, "name", "Name"), `信使 #${id}`),
    forceId: safeInt(pick(m, "forceId", "ForceId"), 1),
    x: safeInt(pick(m, "x", "X")),
    y: safeInt(pick(m, "y", "Y")),
    isMilitary: false,
    soldiers: soldiersRaw == null ? courierCount + escortSoldierCount : safeInt(soldiersRaw),
    courierCount,
    escortSoldierCount,
    ap: safeInt(pick(m, "ap", "Ap")),
    movement: safeInt(pick(m, "movement", "Movement"), 6),
    status: requiredString(pick(m, "status", "Status"), "Moving"),
    payloadType: requiredString(pick(m, "payloadType", "PayloadType"), "PolicyChange"),
    directive: requiredString(pick(m, "directive", "Directive"), "PolicyChange"),
    route: Array.isArray(routeRaw) ? routeRaw.map(normalizeMapPoint) : [],
    morale: safeInt(pick(m, "morale", "Morale"), 80),
    training: safeInt(pick(m, "training", "Training"), 70),
    cultureName: requiredString(pick(m, "cultureName", "CultureName"), "日本"),
    religionName: requiredString(pick(m, "religionName", "ReligionName"), "神道教"),
    money: safeInt(pick(m, "money", "Money")),
    targetUnitId: safeInt(pick(m, "targetUnitId", "TargetUnitId")),
    targetUnitName: optionalString(pick(m, "targetUnitName", "TargetUnitName")),
    originStrongholdId: safeInt(pick(m, "originStrongholdId", "OriginStrongholdId")),
    originStrongholdName: optionalString(pick(m, "originStrongholdName", "OriginStrongholdName")),
    pendingDirective: pending == null ? null : String(pending),
  };
}

function normalizeMapCharacter(raw: unknown): StrategyMapCharacterState {
  const row = (raw ?? {}) as Record<string, unknown>;
  return {
    id: safeInt(pick(row, "id", "Id")),
    name: requiredString(pick(row, "name", "Name"), "—"),
    forceId: safeInt(pick(row, "forceId", "ForceId")),
    x: safeInt(pick(row, "x", "X")),
    y: safeInt(pick(row, "y", "Y")),
    mapVisible: pick(row, "mapVisible", "MapVisible") as boolean | undefined,
  };
}

function normalizeCharacter(raw: unknown): StrategyCharacterSummaryState {
  const row = (raw ?? {}) as Record<string, unknown>;
  const personalityRaw = pick(row, "personality", "Personality") as Record<string, unknown> | undefined;
  const proficiencyRaw = pick(row, "proficiency", "Proficiency") as Record<string, unknown> | undefined;

  return {
    id: safeInt(pick(row, "id", "Id")),
    forceId: safeInt(pick(row, "forceId", "ForceId")),
    name: pick(row, "name", "Name") as string | undefined,
    strongholdId: safeInt(pick(row, "strongholdId", "StrongholdId"), 0) || undefined,
    leaderId: safeInt(pick(row, "leaderId", "LeaderId"), 0) || undefined,
    locationType: pick(row, "locationType", "LocationType") as string | undefined,
    forceStatus: pick(row, "forceStatus", "ForceStatus") as string | undefined,
    leadership: safeInt(pick(row, "leadership", "Leadership"), 0) || undefined,
    power: safeInt(pick(row, "power", "Power"), 0) || undefined,
    politics: safeInt(pick(row, "politics", "Politics"), 0) || undefined,
    strategy: safeInt(pick(row, "strategy", "Strategy"), 0) || undefined,
    charm: safeInt(pick(row, "charm", "Charm"), 0) || undefined,
    cultureName: optionalString(pick(row, "cultureName", "CultureName")) ?? undefined,
    religionName: optionalString(pick(row, "religionName", "ReligionName")) ?? undefined,
    yearsInForce: safeInt(pick(row, "yearsInForce", "YearsInForce"), 0) || undefined,
    sex: optionalString(pick(row, "sex", "Sex")) ?? undefined,
    age: safeInt(pick(row, "age", "Age"), 0) || undefined,
    personality: personalityRaw
      ? {
          temper: safeInt(pick(personalityRaw, "temper", "Temper"), 0) || undefined,
          courage: safeInt(pick(personalityRaw, "courage", "Courage"), 0) || undefined,
          principle: safeInt(pick(personalityRaw, "principle", "Principle"), 0) || undefined,
          action: safeInt(pick(personalityRaw, "action", "Action"), 0) || undefined,
          friendship: safeInt(pick(personalityRaw, "friendship", "Friendship"), 0) || undefined,
          ambition: safeInt(pick(personalityRaw, "ambition", "Ambition"), 0) || undefined,
          hobby: safeInt(pick(personalityRaw, "hobby", "Hobby"), 0) || undefined,
          desire: safeInt(pick(personalityRaw, "desire", "Desire"), 0) || undefined,
          drinking: safeInt(pick(personalityRaw, "drinking", "Drinking"), 0) || undefined,
          fortune: safeInt(pick(personalityRaw, "fortune", "Fortune"), 0) || undefined,
        }
      : undefined,
    proficiency: proficiencyRaw
      ? {
          infantry: safeInt(pick(proficiencyRaw, "infantry", "Infantry"), 0) || undefined,
          ride: safeInt(pick(proficiencyRaw, "ride", "Ride"), 0) || undefined,
          archery: safeInt(pick(proficiencyRaw, "archery", "Archery"), 0) || undefined,
          firelock: safeInt(pick(proficiencyRaw, "firelock", "Firelock"), 0) || undefined,
          sealing: safeInt(pick(proficiencyRaw, "sealing", "Sealing"), 0) || undefined,
          military: safeInt(pick(proficiencyRaw, "military", "Military"), 0) || undefined,
          fighting: safeInt(pick(proficiencyRaw, "fighting", "Fighting"), 0) || undefined,
          spy: safeInt(pick(proficiencyRaw, "spy", "Spy"), 0) || undefined,
          agriculture: safeInt(pick(proficiencyRaw, "agriculture", "Agriculture"), 0) || undefined,
          commerce: safeInt(pick(proficiencyRaw, "commerce", "Commerce"), 0) || undefined,
          construct: safeInt(pick(proficiencyRaw, "construct", "Construct"), 0) || undefined,
          smelt: safeInt(pick(proficiencyRaw, "smelt", "Smelt"), 0) || undefined,
          eloquence: safeInt(pick(proficiencyRaw, "eloquence", "Eloquence"), 0) || undefined,
          court: safeInt(pick(proficiencyRaw, "court", "Court"), 0) || undefined,
          sociality: safeInt(pick(proficiencyRaw, "sociality", "Sociality"), 0) || undefined,
          healing: safeInt(pick(proficiencyRaw, "healing", "Healing"), 0) || undefined,
        }
      : undefined,
    isDead: Boolean(pick(row, "isDead", "IsDead")),
    isSick: Boolean(pick(row, "isSick", "IsSick")),
    birthType: optionalString(pick(row, "birthType", "BirthType")) ?? undefined,
    taskRemainingDays: (() => {
      const raw = pick(row, "taskRemainingDays", "TaskRemainingDays");
      if (raw == null) return null;
      const n = safeInt(raw, -1);
      return n >= 0 ? n : null;
    })(),
    loyalty: safeInt(pick(row, "loyalty", "Loyalty"), 0) || undefined,
  };
}

function normalizeMasterDataEntry(raw: unknown): StrategyMasterDataEntry {
  const row = (raw ?? {}) as Record<string, unknown>;
  const fieldsRaw = pick(row, "fields", "Fields");
  let fields: Record<string, string> | undefined;
  if (fieldsRaw && typeof fieldsRaw === "object" && !Array.isArray(fieldsRaw)) {
    fields = Object.fromEntries(
      Object.entries(fieldsRaw as Record<string, unknown>).map(([key, value]) => [
        key,
        value == null || value === "" ? "—" : String(value),
      ])
    );
  }

  return {
    id: safeInt(pick(row, "id", "Id")),
    name: requiredString(pick(row, "name", "Name"), "—"),
    group: optionalString(pick(row, "group", "Group")),
    description: optionalString(pick(row, "description", "Description")),
    extra: optionalString(pick(row, "extra", "Extra")),
    fields,
  };
}

function normalizeMasterDataList(raw: unknown): StrategyMasterDataEntry[] {
  if (!Array.isArray(raw)) return [];
  return raw.map(normalizeMasterDataEntry);
}

function normalizeMasterData(raw: unknown): StrategyMasterDataSnapshot | undefined {
  if (!raw || typeof raw !== "object") return undefined;
  const row = raw as Record<string, unknown>;
  return {
    cultureGroups: normalizeMasterDataList(pick(row, "cultureGroups", "CultureGroups")),
    cultures: normalizeMasterDataList(pick(row, "cultures", "Cultures")),
    religionGroups: normalizeMasterDataList(pick(row, "religionGroups", "ReligionGroups")),
    religions: normalizeMasterDataList(pick(row, "religions", "Religions")),
    strongholdTypes: normalizeMasterDataList(pick(row, "strongholdTypes", "StrongholdTypes")),
    defenseFacilityTypes: normalizeMasterDataList(
      pick(row, "defenseFacilityTypes", "DefenseFacilityTypes")
    ),
    unitTypes: normalizeMasterDataList(pick(row, "unitTypes", "UnitTypes")),
    characterDefinitions: normalizeMasterDataList(
      pick(row, "characterDefinitions", "CharacterDefinitions")
    ),
    terrains: normalizeMasterDataList(pick(row, "terrains", "Terrains")),
    climates: normalizeMasterDataList(pick(row, "climates", "Climates")),
    weathers: normalizeMasterDataList(pick(row, "weathers", "Weathers")),
    regions: normalizeMasterDataList(pick(row, "regions", "Regions")),
    roads: normalizeMasterDataList(pick(row, "roads", "Roads")),
    landmarks: normalizeMasterDataList(pick(row, "landmarks", "Landmarks")),
    terrainVegetationFeatures: normalizeMasterDataList(
      pick(row, "terrainVegetationFeatures", "TerrainVegetationFeatures")
    ),
    terrainSurfaceFeatures: normalizeMasterDataList(
      pick(row, "terrainSurfaceFeatures", "TerrainSurfaceFeatures")
    ),
    enums: normalizeMasterDataList(pick(row, "enums", "Enums")),
  };
}

/** 规范化 API/Mock 世界状态，补齐缺失情报字段并兼容 PascalCase。 */
export function normalizeStrategyWorldState(raw: unknown): StrategyWorldState {
  if (!raw || typeof raw !== "object") {
    throw new Error("无效的策略世界状态");
  }

  const o = raw as Record<string, unknown>;
  const lord = normalizeLord(pick(o, "lord", "Lord"));
  const mapRaw = (pick(o, "map", "Map") ?? {}) as Record<string, unknown>;
  const dateRaw = (pick(o, "date", "Date") ?? {}) as Record<string, unknown>;

  const unitsRaw = pick(o, "units", "Units");
  const rosterRaw = pick(o, "ownUnitRoster", "OwnUnitRoster");
  const strongholdsRaw = pick(o, "strongholds", "Strongholds");
  const forcesRaw = pick(o, "forces", "Forces");
  const convoysRaw = pick(o, "supplyConvoys", "SupplyConvoys");
  const messengersRaw = pick(o, "messengers", "Messengers");
  const charactersRaw = pick(o, "characters", "Characters");
  const mapCharactersRaw = pick(o, "mapCharacters", "MapCharacters");
  const espionageIntelRaw = pick(o, "espionageIntel", "EspionageIntel");
  const diplomaciesRaw = pick(o, "diplomacies", "Diplomacies");
  const battlefieldsRaw = pick(o, "battlefields", "Battlefields");
  const masterDataRaw = pick(o, "masterData", "MasterData");
  const visibilityRaw = pick(o, "visibility", "Visibility");
  const startOptionsRaw = pick(o, "startOptions", "StartOptions");

  const playerForceId = safeInt(pick(o, "playerForceId", "PlayerForceId"), 1);
  const scenarioId = requiredString(pick(o, "scenarioId", "ScenarioId"), "mini_kanto");
  const difficulty = requiredString(pick(o, "difficulty", "Difficulty"), "Normal");
  const simulationSeed = safeInt(pick(o, "simulationSeed", "SimulationSeed"), 0);

  const width = safeInt(pick(mapRaw, "width", "Width"), 10);
  const height = safeInt(pick(mapRaw, "height", "Height"), 10);

  const map = {
    name: requiredString(pick(mapRaw, "name", "Name"), "策略地图"),
    width,
    height,
  };

  return {
    scenarioId,
    playerForceId,
    difficulty,
    simulationSeed,
    lord,
    map,
    date: {
      year: safeInt(pick(dateRaw, "year", "Year"), 1560),
      month: safeInt(pick(dateRaw, "month", "Month"), 1),
      day: safeInt(pick(dateRaw, "day", "Day"), 1),
    },
    forces: Array.isArray(forcesRaw)
      ? forcesRaw.map((f) => {
          const force = f as Record<string, unknown>;
          const suzerainRaw = pick(force, "suzerainForceId", "SuzerainForceId");
          return {
            id: safeInt(pick(force, "id", "Id")),
            name: requiredString(pick(force, "name", "Name"), "未知势力"),
            food: safeInt(pick(force, "food", "Food")),
            money: safeInt(pick(force, "money", "Money")),
            status: requiredString(pick(force, "status", "Status"), "Independence"),
            suzerainForceId:
              suzerainRaw == null ? null : safeInt(suzerainRaw, 0) || null,
            strongholdCount: safeInt(pick(force, "strongholdCount", "StrongholdCount"), 0),
            characterCount: safeInt(pick(force, "characterCount", "CharacterCount"), 0),
            prestige: safeInt(pick(force, "prestige", "Prestige"), 0),
            orthodoxy: safeInt(pick(force, "orthodoxy", "Orthodoxy"), 0),
            lordResidenceStrongholdId: safeInt(
              pick(force, "lordResidenceStrongholdId", "LordResidenceStrongholdId"),
              0
            ),
            internalArrearsFoodGo: safeInt(
              pick(force, "internalArrearsFoodGo", "InternalArrearsFoodGo"),
              0
            ),
            internalArrearsMoney: safeInt(
              pick(force, "internalArrearsMoney", "InternalArrearsMoney"),
              0
            ),
            successorId: (() => {
              const raw = pick(force, "successorId", "SuccessorId");
              if (raw == null) return null;
              const n = safeInt(raw, 0);
              return n > 0 ? n : null;
            })(),
          } satisfies StrategyForceState;
        })
      : [],
    strongholds: Array.isArray(strongholdsRaw)
      ? strongholdsRaw.map((s) => normalizeStronghold(s, lord, playerForceId))
      : [],
    units: Array.isArray(unitsRaw) ? unitsRaw.map((u) => normalizeUnit(u, lord)) : [],
    ownUnitRoster: Array.isArray(rosterRaw) ? rosterRaw.map(normalizeRosterUnit) : [],
    battlefields: Array.isArray(battlefieldsRaw)
      ? battlefieldsRaw.map(normalizeBattlefield)
      : [],
    supplyConvoys: Array.isArray(convoysRaw) ? convoysRaw.map(normalizeConvoy) : [],
    messengers: Array.isArray(messengersRaw) ? messengersRaw.map(normalizeMessenger) : [],
    characters: Array.isArray(charactersRaw)
      ? charactersRaw.map(normalizeCharacter)
      : [],
    mapCharacters: Array.isArray(mapCharactersRaw)
      ? mapCharactersRaw.map(normalizeMapCharacter)
      : [],
    espionageIntel: Array.isArray(espionageIntelRaw)
      ? espionageIntelRaw.map(normalizeEspionageIntel)
      : [],
    diplomacies: Array.isArray(diplomaciesRaw)
      ? diplomaciesRaw.map((d) => {
          const row = d as Record<string, unknown>;
          return {
            targetForceId: safeInt(pick(row, "targetForceId", "TargetForceId")),
            relation: requiredString(pick(row, "relation", "Relation"), "Neutral"),
            relationship: safeInt(pick(row, "relationship", "Relationship"), 0) || undefined,
            trust: safeInt(pick(row, "trust", "Trust"), 0) || undefined,
            arrearsFoodGo: safeInt(pick(row, "arrearsFoodGo", "ArrearsFoodGo"), 0) || undefined,
            arrearsMoney: safeInt(pick(row, "arrearsMoney", "ArrearsMoney"), 0) || undefined,
          };
        })
      : [],
    masterData: normalizeMasterData(masterDataRaw),
    visibility: normalizeVisibility(visibilityRaw),
    startOptions: normalizeStartOptions(startOptionsRaw),
  };
}
