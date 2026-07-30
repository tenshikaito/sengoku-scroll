import type { StrategyWorldState } from "@/api/strategyTypes";
import { personIntelRows } from "@/utils/strategyIntelSystemData";

export const MONEY_PER_KAN = 1000;
export const MONEY_PER_SOLDIER = 100;

/** 与后端 RecruitConstants.ConscriptCharmDivisor 一致。 */
export const CONSCRIPT_CHARM_DIVISOR = 5;

export function recruitAssignablePersonRows(
  worldState: StrategyWorldState,
  strongholdId: number,
): ReturnType<typeof personIntelRows> {
  const lordName = worldState.lord.name?.trim();
  const playerForceId = worldState.playerForceId;

  return personIntelRows(worldState, { realmFilter: "homeOnly" }).filter((row) => {
    const character = worldState.characters?.find((c) => c.id === row.id);
    if (!character) return false;
    if (character.forceId !== playerForceId || character.isDead) return false;
    if (character.forceStatus !== "Idle") return false;
    if (character.locationType !== "Stronghold") return false;
    if ((character.strongholdId ?? 0) !== strongholdId) return false;
    if (lordName && character.name === lordName) return false;
    return true;
  });
}

/** 据点内驻留的本家将领（情报人物表行）。 */
export function strongholdOfficerRows(
  worldState: StrategyWorldState,
  strongholdId: number | null | undefined,
): ReturnType<typeof personIntelRows> {
  const resolvedStrongholdId = strongholdId ?? 0;
  if (resolvedStrongholdId <= 0) return [];

  const playerForceId = worldState.playerForceId;
  return personIntelRows(worldState, { realmFilter: "homeOnly" }).filter((row) => {
    const character = worldState.characters?.find((c) => c.id === row.id);
    if (!character || character.isDead) return false;
    if (character.forceId !== playerForceId) return false;
    if (character.locationType !== "Stronghold") return false;
    if ((character.strongholdId ?? 0) !== resolvedStrongholdId) return false;
    return true;
  });
}

/** @deprecated 使用 {@link strongholdOfficerRows} */
export function lordResidenceOfficerRows(
  worldState: StrategyWorldState,
  residenceStrongholdId: number | null | undefined,
): ReturnType<typeof personIntelRows> {
  return strongholdOfficerRows(worldState, residenceStrongholdId);
}

export function conscriptDailyRate(charm: number | undefined): number {
  const value = Math.max(0, Math.trunc(charm ?? 0));
  return Math.max(1, Math.floor(value / CONSCRIPT_CHARM_DIVISOR));
}

/** 将后端资金（文）换算为对话框使用的贯。 */
export function maxMercenaryBudgetKan(moneyInMon: number): number {
  return Math.max(0, Math.floor(moneyInMon / MONEY_PER_KAN));
}

/**
 * 募兵预算滑块关键点（单位：贯）。
 * 输入 maxKan 必须是贯，不是文。
 * 例：府库 18_000_000 文 → maxKan = 18000 → 关键点 1、3600、7200、10800、14400、18000。
 */
export function buildMercenaryBudgetKanStops(maxKan: number): number[] {
  if (maxKan < 1) return [];
  if (maxKan <= 8) {
    return Array.from({ length: maxKan }, (_, index) => index + 1);
  }

  const maxMarks = 6;
  const raw: number[] = [1];
  for (let index = 1; index < maxMarks - 1; index++) {
    const ratio = index / (maxMarks - 1);
    raw.push(Math.max(1, Math.round(1 + ratio * (maxKan - 1))));
  }
  raw.push(maxKan);

  const stops: number[] = [];
  for (const value of raw) {
    if (stops.length === 0 || value > stops[stops.length - 1]) {
      stops.push(value);
    }
  }
  return stops;
}

/** el-slider 刻度（键与标签均为贯）。 */
export function buildMercenaryBudgetKanMarks(maxKan: number): Record<number, string> {
  const stops = buildMercenaryBudgetKanStops(maxKan);
  return Object.fromEntries(stops.map((kan) => [kan, `${kan}`]));
}

export function kanToMoney(kan: number): number {
  return Math.max(0, Math.trunc(kan)) * MONEY_PER_KAN;
}

export function mercenarySoldiersFromKan(kan: number): number {
  return Math.floor((kan * MONEY_PER_KAN) / MONEY_PER_SOLDIER);
}

/** 刻度标签取整步长（仅用于 5 档刻度，不影响滑块步进）。 */
function troopAllocationMarkRoundStep(max: number): number | null {
  if (max >= 5000) return 1000;
  if (max >= 500) return 100;
  return null;
}

/** 刻度标签取整：仅大池用整千/整百；小池用整数避免刻度塌缩。 */
function roundTroopMarkValue(value: number, max: number): number {
  const step = troopAllocationMarkRoundStep(max);
  if (step == null) {
    return Math.min(max, Math.max(0, Math.round(value)));
  }
  const rounded = Math.round(value / step) * step;
  return Math.min(max, Math.max(0, rounded));
}

/**
 * 兵种分配滑块 5 档刻度位置（含 0 与池上限）；中间档为整百/整千标签，末档为实际上限。
 */
export function buildTroopAllocationStops(max: number): number[] {
  if (max <= 0) return [0];

  const maxMarks = 5;
  const raw: number[] = [];
  for (let index = 0; index < maxMarks; index++) {
    if (index === maxMarks - 1) {
      raw.push(max);
    } else {
      const ratio = index / (maxMarks - 1);
      raw.push(roundTroopMarkValue(ratio * max, max));
    }
  }

  const stops: number[] = [];
  for (const value of raw) {
    if (stops.length === 0 || value > stops[stops.length - 1]) {
      stops.push(value);
    }
  }

  if (stops[stops.length - 1] !== max) {
    stops.push(max);
  }
  return stops;
}

/** el-slider 刻度：键为实际兵力，标签为格式化兵力。 */
export function buildTroopAllocationMarks(max: number): Record<number, string> {
  const stops = buildTroopAllocationStops(max);
  return Object.fromEntries(stops.map((value) => [value, value.toLocaleString()]));
}

/** 据点内可担任组建总将的将领（Idle / Task）。 */
export function expeditionCommanderRows(
  worldState: StrategyWorldState,
  strongholdId: number,
): ReturnType<typeof personIntelRows> {
  const playerForceId = worldState.playerForceId;
  return personIntelRows(worldState, { realmFilter: "homeOnly" }).filter((row) => {
    const character = worldState.characters?.find((c) => c.id === row.id);
    if (!character || character.isDead) return false;
    if (character.forceId !== playerForceId) return false;
    if (character.locationType !== "Stronghold") return false;
    if ((character.strongholdId ?? 0) !== strongholdId) return false;
    return character.forceStatus === "Idle" || character.forceStatus === "Task";
  });
}
