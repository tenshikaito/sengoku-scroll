import type { StrategyStrongholdState, StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";

const UNKNOWN_INTEL = "未知";

const STRONGHOLD_TYPE_NAMES: Record<number, string> = {
  1: "平城",
  2: "平山城",
  3: "山城",
};

/** 据点类型显示名（平城/平山城/山城）；忽略后端占位「据点类型#N」。 */
export function resolveStrongholdTypeLabel(
  stronghold: StrategyStrongholdState,
  worldState?: StrategyWorldState | null,
): string {
  const typeName = stronghold.typeName?.trim();
  if (typeName && !/^据点类型#\d+$/.test(typeName)) {
    return typeName;
  }

  const typeId = stronghold.typeId > 0 ? stronghold.typeId : 1;
  const fromMaster = worldState?.masterData?.strongholdTypes?.find((t) => t.id === typeId)?.name;
  if (fromMaster?.trim() && !/^据点类型#\d+$/.test(fromMaster.trim())) {
    return fromMaster.trim();
  }

  return STRONGHOLD_TYPE_NAMES[typeId] ?? "平城";
}

/** 据点规模（与城防默认设施人口门槛一致：大/中/小）。 */
export function resolveStrongholdScaleLabel(population: unknown): string {
  const n = Number(population);
  if (!Number.isFinite(n) || n < 0) return "—";
  if (n >= 50_000) return "大";
  if (n >= 30_000) return "中";
  return "小";
}

const KNOWN_STRONGHOLD_HIDDEN_LABELS = new Set([
  "人口",
  "规模",
  "治安",
  "民心",
  "兵力",
  "士气",
  "训练度",
  "文化",
  "信仰",
  "金钱",
  "粮食",
  "城防",
  "负伤",
  "维护费",
  "城内将",
  "设施",
  "人头税",
  "农业税",
  "商业税",
  "关税",
]);

export function resolveIntelMode(worldState: StrategyWorldState): string {
  return (
    worldState.startOptions?.intelMode ??
    worldState.visibility?.intelMode ??
    "Full"
  );
}

export function isRestrictedIntelMode(worldState: StrategyWorldState): boolean {
  return resolveIntelMode(worldState) !== "Full";
}

/** 困难模式（角色迷雾或 Hard 难度）：人物能力以高/中/低显示。 */
export function isCharacterFogMode(worldState: StrategyWorldState): boolean {
  const fogMode =
    worldState.startOptions?.fogMode ?? worldState.visibility?.fogMode ?? "Force";
  return fogMode === "Character" || worldState.difficulty === "Hard";
}

/** 数值档位：≥70 高，≥40 中，否则低。 */
export function formatStatBand(value: unknown): string {
  const n = Number(value);
  if (!Number.isFinite(n)) return "—";
  if (n >= 70) return "高";
  if (n >= 40) return "中";
  return "低";
}

export type IntelBandTone = "high" | "mid" | "low";

export function resolveIntelBandTone(value: string | undefined | null): IntelBandTone | null {
  switch (value?.trim()) {
    case "高":
      return "high";
    case "中":
      return "mid";
    case "低":
      return "low";
    default:
      return null;
  }
}

/** 非自势力（含内藩）是否须隐藏具体数值（须谍报后才展示）。 */
export function isForeignIntelRestricted(
  worldState: StrategyWorldState,
  forceId: number,
): boolean {
  if (!isRestrictedIntelMode(worldState)) return false;
  return !isPlayerRealmForce(forceId, worldState.playerForceId, worldState.forces);
}

/** 敌方单位是否处于模糊情报（后端写入 soldiersDisplay 等）。 */
export function isMaskedEnemyUnit(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): boolean {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) return false;
  return Boolean(unit.soldiersDisplay?.trim());
}

export function hoverSoldiersLabel(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): string {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) {
    return formatSoldiers(unit.soldiers);
  }

  const display = unit.soldiersDisplay?.trim();
  if (display) return display === "未知" ? UNKNOWN_INTEL : display;
  // 业务：无谍报记录时一律「未知」，不因进入视野显示兵数
  return UNKNOWN_INTEL;
}

export function hoverMoraleLabel(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): string {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) {
    const n = Number(unit.morale);
    return Number.isFinite(n) ? String(Math.trunc(n)) : "—";
  }

  const band = unit.moraleBand?.trim();
  if (band) return band === "未知" ? UNKNOWN_INTEL : band;
  return UNKNOWN_INTEL;
}

export function hoverTrainingLabel(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): string {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) {
    const n = Number(unit.training);
    return Number.isFinite(n) ? String(Math.trunc(n)) : "—";
  }

  const band = unit.trainingBand?.trim();
  if (band) return band === "未知" ? UNKNOWN_INTEL : band;
  return UNKNOWN_INTEL;
}

function resolveStrongholdEspionageBand(
  stronghold: StrategyStrongholdState,
  label: string,
): string | null | undefined {
  switch (label) {
    case "兵力":
      return stronghold.espionageSoldiersBand;
    case "士气":
      return stronghold.espionageMoraleBand;
    case "训练度":
      return stronghold.espionageTrainingBand;
    case "人口":
    case "规模":
      return stronghold.espionagePopulationBand;
    case "粮食":
      return stronghold.espionageFoodBand;
    case "金钱":
      return stronghold.espionageMoneyBand;
    default:
      return undefined;
  }
}

function strongholdEspionageBandOrUnknown(
  stronghold: StrategyStrongholdState,
  label: string,
): string | null {
  const band = resolveStrongholdEspionageBand(stronghold, label);
  if (band === undefined) return null;
  if (band === "未知") return UNKNOWN_INTEL;
  return band;
}

function hasEspionageUnknownMask(stronghold: StrategyStrongholdState): boolean {
  return stronghold.espionageSoldiersBand === "未知";
}

export function strongholdHoverFieldValue(
  stronghold: StrategyStrongholdState,
  label: string,
  value: string,
): string {
  const espionageBand = strongholdEspionageBandOrUnknown(stronghold, label);
  if (espionageBand !== null) return espionageBand;

  if (
    hasEspionageUnknownMask(stronghold) &&
    KNOWN_STRONGHOLD_HIDDEN_LABELS.has(label)
  ) {
    return UNKNOWN_INTEL;
  }

  if (stronghold.visibilityTier === "Known" && KNOWN_STRONGHOLD_HIDDEN_LABELS.has(label)) {
    return UNKNOWN_INTEL;
  }
  return value;
}

/** Known 层级非本家据点：地图可见但数值情报隐藏（含内藩）。 */
export function isKnownStrongholdIntelMasked(
  stronghold: StrategyStrongholdState,
  playerForceId: number,
): boolean {
  return stronghold.visibilityTier === "Known" && stronghold.forceId !== playerForceId;
}

export { UNKNOWN_INTEL };
