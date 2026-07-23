import type { StrategyGarrisonTroopPoolState, StrategyCropCycleState, StrategyGarrisonStandingUnitState, StrategyStrongholdCityActorState } from "@/api/strategyTypes";

const POPULATION_PER_FARMER = 2;

export function deriveStrongholdLaborFields(population: number, militiaAway = 0) {
  const laborCapacity = Math.max(1, Math.floor(Math.max(0, population) / POPULATION_PER_FARMER));
  const away = Math.min(laborCapacity, Math.max(0, militiaAway));
  const laborAvailable = Math.max(0, laborCapacity - away);
  const laborRatioPercent = Math.floor((laborAvailable * 100) / laborCapacity);
  return { laborCapacity, laborAvailable, militiaAway: away, laborRatioPercent };
}

/** 与后端 StrongholdMilitaryBootstrapHelper 拆分逻辑对齐（Mock/缺字段回退）。 */
export function deriveGarrisonTroopPools(
  garrisonSoldiers: number,
  population: number,
): StrategyGarrisonTroopPoolState[] {
  const total = Math.max(0, garrisonSoldiers);
  if (total <= 0) return [];

  const horseCap = Math.max(20, Math.floor(population / 80));
  const matchlockCap = Math.max(10, Math.floor(population / 120));
  const cavalry = Math.min(Math.floor(total / 8), horseCap);
  const matchlock = Math.min(Math.floor(total / 12), matchlockCap);
  const archers = Math.min(
    Math.floor(total / 10),
    Math.max(0, Math.floor((total - cavalry - matchlock) / 2)),
  );
  const professional = cavalry + matchlock + archers;
  if (professional <= 0) {
    return [{ typeId: 1, typeName: "足轻", soldiers: total }];
  }

  const militia = total - professional;
  const pools: StrategyGarrisonTroopPoolState[] = [];
  if (militia > 0) pools.push({ typeId: 1, typeName: "足轻", soldiers: militia });
  if (cavalry > 0) pools.push({ typeId: 3, typeName: "骑兵", soldiers: cavalry });
  if (matchlock > 0) pools.push({ typeId: 4, typeName: "铁炮", soldiers: matchlock });
  if (archers > 0) pools.push({ typeId: 2, typeName: "弓兵", soldiers: archers });
  return pools;
}

function estimateSubUnitMaintenance(typeId: number, soldiers: number, isMounted: boolean): number {
  if (typeId === 1 || soldiers <= 0) return 0;

  const baseCost = Math.max(50, soldiers);
  const basisPoints = 10_000;
  const mountedMultiplier = isMounted && typeId !== 3 ? 15_000 : 10_000;

  switch (typeId) {
    case 3:
      return baseCost * 2;
    case 4:
      return Math.floor((baseCost * 2 * (isMounted ? mountedMultiplier : 10_000)) / basisPoints);
    case 2:
      return Math.floor((baseCost * 15_000 * (isMounted ? mountedMultiplier : 10_000)) / basisPoints);
    default:
      return baseCost;
  }
}

/** Mock/缺字段：按 SubUnit 模型推导常备军表（清洲 id=1 为双足轻队）。 */
export function deriveStandingGarrisonUnits(
  strongholdId: number,
  garrisonSoldiers: number,
  militiaSoldiers: number | undefined,
  morale: number,
  training: number,
  pools: StrategyGarrisonTroopPoolState[],
): StrategyGarrisonStandingUnitState[] {
  const rows: StrategyGarrisonStandingUnitState[] = [];
  let militiaRemaining = militiaSoldiers ?? pools.find((p) => p.typeId === 1)?.soldiers ?? garrisonSoldiers;

  if (strongholdId === 1 && militiaRemaining > 0) {
    const first = Math.floor(militiaRemaining / 2);
    const second = militiaRemaining - first;
    if (first > 0) {
      rows.push({
        subUnitId: strongholdId * 100 + 1,
        unitName: "足轻一",
        typeId: 1,
        typeName: "足轻",
        isMounted: false,
        soldiers: first,
        role: "Militia",
        morale,
        training,
        maintenanceMoney: 0,
      });
    }
    if (second > 0) {
      rows.push({
        subUnitId: strongholdId * 100 + 2,
        unitName: "足轻二",
        typeId: 1,
        typeName: "足轻",
        isMounted: false,
        soldiers: second,
        role: "Militia",
        morale,
        training,
        maintenanceMoney: 0,
      });
    }
    militiaRemaining = 0;
  } else if (militiaRemaining > 0) {
    rows.push({
      subUnitId: 0,
      unitName: "农兵备",
      typeId: 1,
      typeName: "足轻",
      isMounted: false,
      soldiers: militiaRemaining,
      role: "Militia",
      morale,
      training,
      maintenanceMoney: 0,
    });
    militiaRemaining = 0;
  }

  let subId = strongholdId * 100 + 10;
  for (const pool of pools) {
    if (pool.typeId === 1 || pool.soldiers <= 0) continue;
    rows.push({
      subUnitId: subId++,
      unitName: pool.typeName,
      typeId: pool.typeId,
      typeName: pool.typeName,
      isMounted: pool.typeId === 3,
      soldiers: pool.soldiers,
      role: "Samurai",
      morale,
      training,
      maintenanceMoney: estimateSubUnitMaintenance(pool.typeId, pool.soldiers, pool.typeId === 3),
    });
  }

  if (rows.length === 0 && garrisonSoldiers > 0) {
    rows.push({
      subUnitId: 0,
      unitName: "足轻",
      typeId: 1,
      typeName: "足轻",
      isMounted: false,
      soldiers: garrisonSoldiers,
      role: "Militia",
      morale,
      training,
      maintenanceMoney: 0,
    });
  }

  return rows;
}

const SINGLE_CYCLE = { startMonth: 4, startDay: 1, endMonth: 10, endDay: 15, name: "单季作" };
const DOUBLE_CYCLES = [
  { startMonth: 3, startDay: 1, endMonth: 5, endDay: 31, name: "早稻" },
  { startMonth: 6, startDay: 2, endMonth: 8, endDay: 31, name: "晚稻" },
];
const TRIPLE_CYCLES = [
  ...DOUBLE_CYCLES,
  { startMonth: 9, startDay: 2, endMonth: 10, endDay: 15, name: "第三季" },
];

function resolveCycleTemplates(pattern: string | undefined) {
  switch (pattern) {
    case "Triple":
      return TRIPLE_CYCLES;
    case "Double":
      return DOUBLE_CYCLES;
    default:
      return [SINGLE_CYCLE];
  }
}

function resolveProgressPercent(
  cycleIndex: number,
  early: number,
  late: number,
  third: number,
): number {
  if (cycleIndex === 0) return early;
  if (cycleIndex === 1) return late;
  if (cycleIndex === 2) return third;
  return 0;
}

export function deriveCropCycles(
  stronghold: {
    effectiveCropPattern?: string;
    earlyCropProgressPercent?: number;
    lateCropProgressPercent?: number;
    thirdCropProgressPercent?: number;
    agricultureProductionPotential?: number;
  },
): StrategyCropCycleState[] {
  const pattern = stronghold.effectiveCropPattern ?? "Single";
  const templates = resolveCycleTemplates(pattern);
  const potentialTotal = Math.max(0, stronghold.agricultureProductionPotential ?? 0);
  const share = templates.length > 0 ? potentialTotal / templates.length : 0;

  return templates.map((tpl, cycleIndex) => {
    const progressPercent = resolveProgressPercent(
      cycleIndex,
      stronghold.earlyCropProgressPercent ?? 0,
      stronghold.lateCropProgressPercent ?? 0,
      stronghold.thirdCropProgressPercent ?? 0,
    );
    return {
      cycleIndex,
      name: tpl.name,
      startMonth: tpl.startMonth,
      startDay: tpl.startDay,
      endMonth: tpl.endMonth,
      endDay: tpl.endDay,
      progressPercent,
      progressCapPercent: 100,
      potentialYieldGo: Math.trunc(share),
      estimatedYieldGo: Math.trunc((share * progressPercent) / 100),
    };
  });
}

export function shouldDeriveStrongholdLaborFields(
  population: number,
  laborCapacity: number | undefined,
): boolean {
  return population > 0 && (laborCapacity == null || laborCapacity <= 0);
}

const MERCHANT_HOUSE_NAMES = ["三井屋", "今井屋", "津田屋", "住友屋", "鸿池屋"];

function resolveMerchantHouseName(strongholdId: number): string {
  return MERCHANT_HOUSE_NAMES[Math.max(0, strongholdId - 1) % MERCHANT_HOUSE_NAMES.length];
}

function isNanbanMerchantName(name: string): boolean {
  return name.includes("南蛮");
}

function resolveTempleName(strongholdName: string): string {
  switch (strongholdName) {
    case "清洲":
      return "热田神宫";
    case "小田原":
      return "早云寺";
    case "冈崎":
      return "八幡宫";
    case "骏府":
      return "久能山浅间神社";
    default:
      return `${strongholdName}寺`;
  }
}

/** API 缺字段或旧存档无 CityActors 时，补全官府/民间等核心势力行。 */
export function deriveCityActors(stronghold: {
  id: number;
  name: string;
  population: number;
  lordId?: number;
  lordName?: string | null;
  isDirectRule?: boolean;
  money?: number;
  food?: number;
  cityActors?: StrategyStrongholdCityActorState[];
}): StrategyStrongholdCityActorState[] {
  const existing = (stronghold.cityActors ?? []).map((actor) =>
    actor.kind === "Nanban" ? { ...actor, kind: "Merchant" } : actor,
  );
  const hasKind = (kind: string) => existing.some((actor) => actor.kind === kind);
  const core: StrategyStrongholdCityActorState[] = [];

  if (!hasKind("Government")) {
    core.push({
      id: stronghold.id * 1000 + 1,
      name: `${stronghold.name}官府`,
      kind: "Government",
      money: stronghold.money ?? 0,
      food: stronghold.food ?? 0,
      luxuryGoods: 0,
      commerceProduction: 0,
      agricultureProduction: 0,
      characterCount: 0,
    });
  }

  if (!hasKind("Civilian")) {
    core.push({
      id: stronghold.id * 1000 + 2,
      name: "民间",
      kind: "Civilian",
      money: 0,
      food: 0,
      luxuryGoods: 0,
      commerceProduction: 0,
      agricultureProduction: 0,
      characterCount: 0,
    });
  }

  const lordId = stronghold.lordId ?? 0;
  const isDirectRule = stronghold.isDirectRule ?? lordId === 0;
  if (lordId > 0 && !isDirectRule && !hasKind("Kokujin")) {
    core.push({
      id: lordId,
      name: stronghold.lordName?.trim() || `国人 #${lordId}`,
      kind: "Kokujin",
      money: 0,
      food: 0,
      luxuryGoods: 0,
      commerceProduction: 0,
      agricultureProduction: 0,
      characterCount: 1,
    });
  }

  const supplemental: StrategyStrongholdCityActorState[] = [];
  const commerceValue = Math.max(1000, stronghold.population * 2);

  if (existing.length === 0 && commerceValue >= 20 && !hasKind("Merchant")) {
    supplemental.push({
      id: stronghold.id * 1000 + 7,
      name: resolveMerchantHouseName(stronghold.id),
      kind: "Merchant",
      money: 18_000 + Math.max(0, stronghold.id) * 2_500,
      food: 2_400_000,
      luxuryGoods: 100,
      commerceProduction: Math.max(500, Math.floor(commerceValue / 40)),
      agricultureProduction: 0,
      characterCount: 1,
      characterIds: [90_000 + stronghold.id * 100 + 1],
      leaderName: `${resolveMerchantHouseName(stronghold.id)}当主`,
      branchLabel: "本店",
    });
  }

  if (existing.length === 0 && stronghold.population >= 40_000 && !hasKind("Religion")) {
    supplemental.push({
      id: stronghold.id * 1000 + 8,
      name: resolveTempleName(stronghold.name),
      kind: "Religion",
      money: 12_000,
      food: 1_200_000,
      luxuryGoods: 20,
      commerceProduction: 0,
      agricultureProduction: Math.max(120_000, stronghold.population * 3),
      characterCount: 1,
      characterIds: [90_000 + stronghold.id * 100 + 8],
      leaderName: "住持",
      branchLabel: "本院",
    });
  }

  if (existing.length === 0 && commerceValue >= 80_000) {
    supplemental.push({
      id: stronghold.id * 1000 + 9,
      name: "南蛮商会",
      kind: "Merchant",
      money: 35_000,
      food: 1_200_000,
      luxuryGoods: 300,
      commerceProduction: Math.max(800, Math.floor(commerceValue / 40)),
      agricultureProduction: 0,
      characterCount: 1,
      characterIds: stronghold.id === 1 ? [90_030] : [90_000 + stronghold.id * 100 + 9],
      leaderName: stronghold.id === 1 ? "柏来图" : "南蛮商人",
      branchLabel: "分店",
    });
  }

  if (stronghold.id === 1 && !existing.some((actor) => actor.id === stronghold.id * 1000 + 71)) {
    supplemental.push({
      id: stronghold.id * 1000 + 71,
      name: "今井屋",
      kind: "Merchant",
      money: 28_000,
      food: 1_200_000,
      luxuryGoods: 180,
      commerceProduction: 640,
      agricultureProduction: 0,
      characterCount: 2,
      characterIds: [90_012, 90_013],
    });
  }

  if (stronghold.id === 1 && !existing.some((actor) => actor.id === stronghold.id * 1000 + 88)) {
    supplemental.push({
      id: stronghold.id * 1000 + 88,
      name: "证愿寺",
      kind: "Religion",
      money: 5_000,
      food: 600_000,
      luxuryGoods: 12,
      commerceProduction: 0,
      agricultureProduction: 120_000,
      characterCount: 1,
      characterIds: [90_022],
    });
  }

  const merged = [...core, ...existing, ...supplemental];
  if (stronghold.id === 1) {
    return merged.map((actor) => {
      if (actor.kind === "Civilian" && !actor.characterIds?.length) {
        return {
          ...actor,
          characterIds: [90_001, 90_002, 90_003],
          characterCount: 3,
        };
      }

      if (actor.id === stronghold.id * 1000 + 7 && actor.kind === "Merchant") {
        return {
          ...actor,
          name: "三井屋",
          characterIds: actor.characterIds?.length ? actor.characterIds : [90_011, 90_014],
          characterCount: Math.max(actor.characterCount ?? 0, actor.characterIds?.length ?? 2),
          leaderName: actor.leaderName ?? "三井高利",
          branchLabel: actor.branchLabel ?? "本店",
        };
      }

      if (actor.id === stronghold.id * 1000 + 9 && actor.kind === "Merchant") {
        const characterIds = actor.characterIds?.length ? actor.characterIds : [90_030];
        return {
          ...actor,
          name: isNanbanMerchantName(actor.name) ? "南蛮商会" : actor.name,
          characterIds,
          characterCount: Math.max(actor.characterCount ?? 0, characterIds.length),
          leaderName: actor.leaderName ?? "柏来图",
          branchLabel: actor.branchLabel ?? "分店",
        };
      }

      if (actor.id === stronghold.id * 1000 + 71 && actor.kind === "Merchant") {
        return {
          ...actor,
          name: "今井屋",
          characterIds: actor.characterIds?.length ? actor.characterIds : [90_012, 90_013],
          characterCount: Math.max(actor.characterCount ?? 0, actor.characterIds?.length ?? 2),
          leaderName: actor.leaderName ?? "今井宗久",
          branchLabel: actor.branchLabel ?? "分店",
        };
      }

      return actor;
    });
  }

  return merged;
}

export function enrichStrongholdDerivedFields<
  T extends {
    id: number;
    name: string;
    population: number;
    lordId?: number;
    lordName?: string | null;
    isDirectRule?: boolean;
    money?: number;
    food?: number;
    garrisonSoldiers: number;
    militiaSoldiers?: number;
    militiaAway?: number;
    laborCapacity?: number;
    laborAvailable?: number;
    laborRatioPercent?: number;
    garrisonTroopPools?: StrategyGarrisonTroopPoolState[];
    standingGarrisonUnits?: StrategyGarrisonStandingUnitState[];
    cropCycles?: StrategyCropCycleState[];
    cityActors?: StrategyStrongholdCityActorState[];
    agricultureProductionPotential?: number;
    knowsDoubleCrop?: boolean;
    knowsTripleCrop?: boolean;
    effectiveCropPattern?: string;
    earlyCropProgressPercent?: number;
    lateCropProgressPercent?: number;
    thirdCropProgressPercent?: number;
    morale?: number;
    training?: number;
  },
>(stronghold: T): T {
  const militiaAway = stronghold.militiaAway ?? 0;
  const labor = deriveStrongholdLaborFields(stronghold.population, militiaAway);
  const pools =
    stronghold.garrisonTroopPools && stronghold.garrisonTroopPools.length > 0
      ? stronghold.garrisonTroopPools
      : deriveGarrisonTroopPools(stronghold.garrisonSoldiers, stronghold.population);
  const militiaFromUnits = (stronghold.standingGarrisonUnits ?? [])
    .filter((unit) => unit.role === "Militia")
    .reduce((sum, unit) => sum + unit.soldiers, 0);
  const militiaSoldiers =
    stronghold.militiaSoldiers ??
    (militiaFromUnits > 0
      ? militiaFromUnits
      : (pools.find((p) => p.typeId === 1)?.soldiers ?? 0));
  const pattern = stronghold.effectiveCropPattern ?? "Single";
  const morale = stronghold.morale ?? 80;
  const training = stronghold.training ?? 65;

  return {
    ...stronghold,
    militiaSoldiers,
    laborCapacity: shouldDeriveStrongholdLaborFields(stronghold.population, stronghold.laborCapacity)
      ? labor.laborCapacity
      : stronghold.laborCapacity,
    laborAvailable: shouldDeriveStrongholdLaborFields(stronghold.population, stronghold.laborCapacity)
      ? labor.laborAvailable
      : (stronghold.laborAvailable ?? labor.laborAvailable),
    laborRatioPercent:
      stronghold.laborRatioPercent ??
      (shouldDeriveStrongholdLaborFields(stronghold.population, stronghold.laborCapacity)
        ? labor.laborRatioPercent
        : 100),
    militiaAway,
    garrisonTroopPools: pools,
    effectiveCropPattern: pattern,
    earlyCropProgressPercent: stronghold.earlyCropProgressPercent ?? 0,
    lateCropProgressPercent: stronghold.lateCropProgressPercent ?? 0,
    thirdCropProgressPercent: stronghold.thirdCropProgressPercent ?? 0,
    agricultureProductionPotential:
      stronghold.agricultureProductionPotential ??
      Math.max(0, Math.floor(stronghold.population * 0.8)),
    knowsDoubleCrop:
      stronghold.knowsDoubleCrop ??
      (pattern === "Double" || pattern === "Triple"),
    knowsTripleCrop: stronghold.knowsTripleCrop ?? pattern === "Triple",
    standingGarrisonUnits:
      stronghold.standingGarrisonUnits && stronghold.standingGarrisonUnits.length > 0
        ? stronghold.standingGarrisonUnits
        : deriveStandingGarrisonUnits(
            stronghold.id,
            stronghold.garrisonSoldiers,
            militiaSoldiers,
            morale,
            training,
            pools,
          ),
    cropCycles:
      stronghold.cropCycles && stronghold.cropCycles.length > 0
        ? stronghold.cropCycles
        : deriveCropCycles({
            effectiveCropPattern: pattern,
            earlyCropProgressPercent: stronghold.earlyCropProgressPercent ?? 0,
            lateCropProgressPercent: stronghold.lateCropProgressPercent ?? 0,
            thirdCropProgressPercent: stronghold.thirdCropProgressPercent ?? 0,
            agricultureProductionPotential:
              stronghold.agricultureProductionPotential ??
              Math.max(0, Math.floor(stronghold.population * 0.8)),
          }),
    cityActors: deriveCityActors(stronghold),
  };
}
