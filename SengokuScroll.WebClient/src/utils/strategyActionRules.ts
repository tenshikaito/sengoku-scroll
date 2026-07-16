import type { StrategyUnitState } from "@/api/strategyTypes";

/** 与后端 GameRuleConfig.SiegeOrderAp 默认一致。 */
export const SIEGE_AP_COST = 5;

export function siegeApBlockReason(unit: StrategyUnitState | null | undefined): string | null {
  if (!unit) return "未选择单位";
  if (unit.ap < SIEGE_AP_COST) {
    return `AP 不足，无法下达攻城指令（当前 ${unit.ap}，需要 ${SIEGE_AP_COST}）`;
  }
  return null;
}

/** 与后端 <see cref="GameRuleConfig.AttackAp"/> 默认一致。 */
export const ATTACK_AP_COST = 5;

export function attackApBlockReason(unit: StrategyUnitState | null | undefined): string | null {
  if (!unit) return "未选择单位";
  if (unit.ap < ATTACK_AP_COST) {
    return `AP 不足，无法攻击（当前 ${unit.ap}，需要 ${ATTACK_AP_COST}）`;
  }
  return null;
}

export function parseApiErrorCode(message: string): string | null {
  const match = message.match(/：(\w+)\s*$/);
  return match?.[1] ?? null;
}
