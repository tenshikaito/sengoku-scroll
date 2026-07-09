import type { StrategyForceState } from "@/api/strategyTypes";
import { resolveRealmRootId } from "@/utils/mapEntityColors";

/** 势力色板（含内藩等扩展势力；M2-b 色块地图）。 */
const FORCE_PALETTE: readonly number[] = [
  0x2563eb,
  0xdc2626,
  0x16a34a,
  0xca8a04,
  0x9333ea,
  0x0891b2,
  0xdb2777,
  0x65a30d,
  0x7c3aed,
  0xea580c,
];

function paletteColor(forceId: number): number {
  if (forceId <= 0) return 0x64748b;
  return FORCE_PALETTE[(forceId - 1) % FORCE_PALETTE.length]!;
}

/** 势力视图：每个势力（含内藩）独立配色。 */
export function getForceColor(forceId: number): number {
  return paletteColor(forceId);
}

/** 封地视图：内藩与宗主同色。 */
export function getRealmColor(
  realmRootId: number,
  forces?: readonly StrategyForceState[]
): number {
  const rootId =
    forces && forces.length > 0 ? resolveRealmRootId(realmRootId, forces) : realmRootId;
  return paletteColor(rootId);
}

export function getForceColorCss(forceId: number): string {
  const color = getForceColor(forceId);
  return `#${color.toString(16).padStart(6, "0")}`;
}
