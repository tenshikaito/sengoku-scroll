/** 内部粮单位（合）；1 石 = 1000 合。 */
export const GO_PER_KOKU = 1000;

/** 内部货币最小单位；1 贯 = 1000 最小单位。 */
export const MONEY_PER_KAN = 1000;

function formatScaledAmount(value: unknown, scale: number): string {
  const n = Number(value);
  const safe = Number.isFinite(n) ? n : 0;
  const scaled = safe / scale;
  return Math.trunc(scaled).toLocaleString();
}

/** 兵数：面板显示为「人」。 */
export function formatSoldiers(count: unknown): string {
  const n = Number(count);
  const safe = Number.isFinite(n) ? Math.max(0, Math.trunc(n)) : 0;
  return `${safe.toLocaleString()}人`;
}

/** 围城等场景下遮蔽敌方兵力：仅保留首位数字，其余为 *（如 3***人）。 */
export function maskSoldiersFirstDigit(count: unknown): string {
  const n = Number(count);
  const safe = Number.isFinite(n) ? Math.max(0, Math.trunc(n)) : 0;
  if (safe <= 0) return "0人";
  const text = String(safe);
  if (text.length <= 1) return `${text}人`;
  return `${text[0]}${"*".repeat(text.length - 1)}人`;
}

export function formatSiegeSoldiers(
  count: unknown,
  forceId: number,
  playerForceId: number,
): string {
  if (forceId === playerForceId) return formatSoldiers(count);
  return maskSoldiersFirstDigit(count);
}

/** 粮草（合）→ 石（仅数值）。 */
export function formatFoodKoku(go: unknown): string {
  return formatScaledAmount(go, GO_PER_KOKU);
}

/** 粮草（合）→ 石。 */
export function formatFoodGo(go: unknown): string {
  return `${formatFoodKoku(go)}石`;
}

/** 金钱（最小单位）→ 贯（仅数值）。 */
export function formatMoneyKan(minUnit: unknown): string {
  return formatScaledAmount(minUnit, MONEY_PER_KAN);
}

/** 金钱（最小单位）→ 贯。 */
export function formatMoney(minUnit: unknown): string {
  return `${formatMoneyKan(minUnit)}贯`;
}
