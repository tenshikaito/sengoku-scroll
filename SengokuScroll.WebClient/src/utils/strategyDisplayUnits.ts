/** 内部粮单位（合）；1 石 = 1000 合。 */
export const GO_PER_KOKU = 1000;

/** 内部货币最小单位；1 贯 = 1000 最小单位。 */
export const MONEY_PER_KAN = 1000;

function formatScaledAmount(value: unknown, scale: number): string {
  const n = Number(value);
  const safe = Number.isFinite(n) ? n : 0;
  const scaled = safe / scale;
  if (Number.isInteger(scaled)) {
    return scaled.toLocaleString();
  }
  return scaled.toLocaleString(undefined, { maximumFractionDigits: 2 });
}

/** 兵数：面板显示为「人」。 */
export function formatSoldiers(count: unknown): string {
  const n = Number(count);
  const safe = Number.isFinite(n) ? Math.max(0, Math.trunc(n)) : 0;
  return `${safe.toLocaleString()}人`;
}

/** 粮草（合）→ 石。 */
export function formatFoodGo(go: unknown): string {
  return `${formatScaledAmount(go, GO_PER_KOKU)}石`;
}

/** 金钱（最小单位）→ 贯。 */
export function formatMoney(minUnit: unknown): string {
  return `${formatScaledAmount(minUnit, MONEY_PER_KAN)}贯`;
}
