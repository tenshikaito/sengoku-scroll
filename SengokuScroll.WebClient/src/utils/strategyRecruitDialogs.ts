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
