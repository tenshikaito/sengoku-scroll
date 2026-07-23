import type { StrategyForceState, StrategyStrongholdState, StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { resolveIntelBandTone as resolveIntelBandToneFromBehaviors } from "@/intelDisplay/IntelDisplayBehaviors";
import { resolveStrongholdEspionageBand } from "@/intelDisplay/StrongholdEspionageBandBehavior";
import { GameStartOptionsProfile } from "@/gameStartOptions/GameStartOptionsProfile";
import { isAllyIntelVisible as isAllyIntelVisibleFromProfile } from "@/gameStartOptions/IntelModeBehavior";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";
import { formatFoodGo, formatMoney, formatSoldiers } from "@/utils/strategyDisplayUnits";

function startProfile(worldState: StrategyWorldState): GameStartOptionsProfile {
  return GameStartOptionsProfile.fromWorldState(worldState);
}

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

/** 据点规模：优先读 1–30 数值；无则按人口回退大/中/小。 */
export function resolveStrongholdScaleLabel(
  scale: unknown,
  population?: unknown,
): string {
  const scaleValue = Number(scale);
  if (Number.isFinite(scaleValue) && scaleValue >= 1 && scaleValue <= 30) {
    return String(Math.trunc(scaleValue));
  }
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
  "农兵",
  "武士",
  "伤兵",
  "劳力",
  "农事进度",
  "生产效率",
  "人头税",
  "农业税",
  "商业税",
  "关税",
]);

export function resolveIntelMode(worldState: StrategyWorldState): string {
  return startProfile(worldState).intel.mode;
}

export function isRestrictedIntelMode(worldState: StrategyWorldState): boolean {
  return startProfile(worldState).isRestrictedIntelMode();
}

/** 开启显示同盟情报时：与同盟 Realm 共享具体数值；外藩除外。 */
export const isAllyIntelVisible = isAllyIntelVisibleFromProfile;

/** 困难模式（角色迷雾或 Hard 难度）：人物能力以高/中/低显示。 */
export function isCharacterFogMode(worldState: StrategyWorldState): boolean {
  return startProfile(worldState).isCharacterFogMode();
}

/** 数值档位：≥70 高，≥40 中，否则低。 */
export function formatStatBand(value: unknown): string {
  const n = Number(value);
  if (!Number.isFinite(n)) return "—";
  if (n >= 70) return "高";
  if (n >= 40) return "中";
  return "低";
}

export const resolveIntelBandTone = resolveIntelBandToneFromBehaviors;

/** 非自势力是否须隐藏具体数值（须谍报后才展示；同盟/内藩在开启选项时可见）。 */
export function isForeignIntelRestricted(
  worldState: StrategyWorldState,
  forceId: number,
): boolean {
  return startProfile(worldState).isForeignIntelRestricted(worldState, forceId);
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

function hasUnitDomesticEspionage(worldState: StrategyWorldState, unitId: number): boolean {
  return (
    worldState.espionageIntel?.some(
      (entry) =>
        entry.targetKind === "Unit" &&
        entry.targetId === unitId &&
        (entry.scope === "Domestic" || entry.scope === "Both"),
    ) ?? false
  );
}

export function hoverMoneyLabel(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): string {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) {
    return formatMoney(unit.money);
  }
  if (hasUnitDomesticEspionage(worldState, unit.id)) {
    return formatMoney(unit.money);
  }
  return UNKNOWN_INTEL;
}

export function hoverFoodLabel(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): string {
  if (!isForeignIntelRestricted(worldState, unit.forceId)) {
    return formatFoodGo(unit.food);
  }
  if (hasUnitDomesticEspionage(worldState, unit.id)) {
    return formatFoodGo(unit.food);
  }
  return UNKNOWN_INTEL;
}

function resolveStrongholdEspionageBandLocal(
  stronghold: StrategyStrongholdState,
  label: string,
): string | null | undefined {
  return resolveStrongholdEspionageBand(stronghold, label);
}

function strongholdEspionageBandOrUnknown(
  stronghold: StrategyStrongholdState,
  label: string,
): string | null {
  const band = resolveStrongholdEspionageBandLocal(stronghold, label);
  if (band === undefined) return null;
  if (band === "未知") return UNKNOWN_INTEL;
  return band;
}

function hasEspionageUnknownMask(stronghold: StrategyStrongholdState): boolean {
  return stronghold.espionageSoldiersBand === "未知";
}

function hasStrongholdMilitaryEspionageIntel(stronghold: StrategyStrongholdState): boolean {
  const band = stronghold.espionageSoldiersBand?.trim();
  return Boolean(band && band !== "未知");
}

function hasStrongholdDomesticEspionageIntel(stronghold: StrategyStrongholdState): boolean {
  const bands = [
    stronghold.espionagePopulationBand,
    stronghold.espionageFoodBand,
    stronghold.espionageMoneyBand,
  ];
  return bands.some((band) => {
    const trimmed = band?.trim();
    return Boolean(trimmed && trimmed !== "未知");
  });
}

export function shouldObscureStrongholdPersonnel(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (isPlayerRealmForce(stronghold.forceId, worldState.playerForceId, worldState.forces)) {
    return false;
  }
  if (isKnownStrongholdIntelMasked(stronghold, worldState.playerForceId, worldState.forces)) {
    return true;
  }
  return isForeignIntelRestricted(worldState, stronghold.forceId);
}

export function strongholdHoverFieldValue(
  worldState: StrategyWorldState | null | undefined,
  stronghold: StrategyStrongholdState,
  label: string,
  value: string,
): string {
  const espionageBand = strongholdEspionageBandOrUnknown(stronghold, label);
  if (espionageBand !== null) return espionageBand;

  if (
    worldState &&
    isForeignIntelRestricted(worldState, stronghold.forceId) &&
    KNOWN_STRONGHOLD_HIDDEN_LABELS.has(label)
  ) {
    return UNKNOWN_INTEL;
  }

  if (
    hasEspionageUnknownMask(stronghold) &&
    KNOWN_STRONGHOLD_HIDDEN_LABELS.has(label)
  ) {
    return UNKNOWN_INTEL;
  }

  if (
    stronghold.visibilityTier === "Known" &&
    KNOWN_STRONGHOLD_HIDDEN_LABELS.has(label) &&
    worldState &&
    !isPlayerRealmForce(stronghold.forceId, worldState.playerForceId, worldState.forces)
  ) {
    return UNKNOWN_INTEL;
  }
  return value;
}

/** Known 层级非自势力圈据点：地图可见但数值情报隐藏。 */
export function isKnownStrongholdIntelMasked(
  stronghold: StrategyStrongholdState,
  playerForceId: number,
  forces: readonly StrategyForceState[],
): boolean {
  if (stronghold.visibilityTier !== "Known") return false;
  if (isPlayerRealmForce(stronghold.forceId, playerForceId, forces)) return false;
  return true;
}

export function battlefieldParticipantMoraleLabel(
  worldState: StrategyWorldState,
  forceId: number,
  morale: number,
): string {
  if (isForeignIntelRestricted(worldState, forceId)) return UNKNOWN_INTEL;
  return `${Math.max(0, Math.min(100, morale))}%`;
}

export function battlefieldParticipantMoneyLabel(
  worldState: StrategyWorldState,
  forceId: number,
  money: number,
): string {
  if (isForeignIntelRestricted(worldState, forceId)) return UNKNOWN_INTEL;
  return formatMoney(money);
}

export function battlefieldParticipantFoodLabel(
  worldState: StrategyWorldState,
  forceId: number,
  food: number,
): string {
  if (isForeignIntelRestricted(worldState, forceId)) return UNKNOWN_INTEL;
  return formatFoodGo(food);
}

export { hasStrongholdMilitaryEspionageIntel, hasStrongholdDomesticEspionageIntel, UNKNOWN_INTEL };
