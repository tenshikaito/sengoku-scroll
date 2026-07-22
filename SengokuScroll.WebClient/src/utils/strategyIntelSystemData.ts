import type {
  StrategyCharacterSummaryState,
  StrategyForceState,
  StrategyStrongholdState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategyTypes";
import { isPlayerRealmForce, resolveRealmRootId } from "@/utils/mapEntityColors";
import {
  formatFoodGo,
  formatMoney,
  formatSoldiers,
} from "@/utils/strategyDisplayUnits";
import {
  countOwnCharacters,
  countOwnStrongholds,
  countRealmCharacters,
  countRealmStrongholds,
} from "@/utils/strategyRealmStats";
import {
  formatStatBand,
  isCharacterFogMode,
  isRestrictedIntelMode,
  resolveStrongholdTypeLabel,
  resolveStrongholdScaleLabel,
  shouldObscureStrongholdPersonnel,
  strongholdHoverFieldValue,
  UNKNOWN_INTEL,
} from "@/utils/strategyIntelDisplay";
import {
  defenseFacilityCategoryLabel,
  strongholdDetailIntelRows,
} from "@/utils/strategyIntelRows";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import { matchesIntelRealmFilter } from "@/utils/intelRealmFilter";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import type { MasterDataListPreset } from "@/utils/strategyIntelSystemColumns";
import {
  hobbyCategoryLabel,
  hobbyCategoryValues,
  personalityFieldLabel,
} from "@/utils/strategyCharacterPersonality";

export interface IntelForceRow {
  id: number;
  name: string;
  lordName: string;
  residenceName: string;
  cultureName: string;
  religionName: string;
  status: string;
  suzerainName: string;
  relation: string;
  strongholdCount: string;
  characterCount: string;
  soldiers: string;
  money: string;
  food: string;
  prestige: string;
  orthodoxy: string;
  successorName: string;
  arrearsMoney: string;
  arrearsFood: string;
  isPlayer: boolean;
  isOwnRealm: boolean;
}

export interface IntelStrongholdRow {
  id: number;
  name: string;
  forceName: string;
  position: string;
  category: string;
  scale: string;
  lordName: string;
  mayorName: string;
  population: string;
  stability: string;
  popularFeelings: string;
  maintenance: string;
  cityGenerals: string;
  wounded: string;
  morale: string;
  training: string;
  cultureName: string;
  religionName: string;
  defense: string;
  garrisonTotal: string;
  money: string;
  food: string;
  pollTaxRate: string;
  agricultureTaxRate: string;
  commerceTaxRate: string;
  tariffTaxRate: string;
  isLordResidence: string;
  isFictional: string;
  isAssault: string;
  isEncircle: string;
  facilities: string;
}

export interface IntelPersonRow {
  id: number;
  name: string;
  forceName: string;
  strongholdName: string;
  isFamily: string;
  role: string;
  superior: string;
  location: string;
  locationType: string;
  leadership: string;
  power: string;
  politics: string;
  strategy: string;
  charm: string;
  loyalty: string;
  status: string;
  healthStatus: string;
  commandTarget: string;
  taskRemainingDays: string;
  yearsInForce: string;
  cultureName: string;
  religionName: string;
  sex: string;
  age: string;
  birthType: string;
  temper: string;
  courage: string;
  principle: string;
  action: string;
  friendship: string;
  ambition: string;
  hobbyWeapon: string;
  hobbyBook: string;
  hobbyArt: string;
  hobbyImport: string;
  hobbyTreasure: string;
  desire: string;
  drinking: string;
  fortune: string;
  skillInfantry: string;
  skillRide: string;
  skillArchery: string;
  skillFirelock: string;
  skillSealing: string;
  skillMilitary: string;
  skillFighting: string;
  skillSpy: string;
  skillAgriculture: string;
  skillCommerce: string;
  skillConstruct: string;
  skillSmelt: string;
  skillEloquence: string;
  skillCourt: string;
  skillSociality: string;
  skillHealing: string;
}

export interface IntelDefenseFacilityRow {
  category: string;
  name: string;
  level: string;
  defense: string;
}

export interface IntelDiplomacyRow {
  forceId: number;
  forceName: string;
  lordName: string;
  /** 关系值 -100~100。 */
  relation: string;
  /** 信赖 -100~100。 */
  trust: string;
  /** 同盟 / 中立 / 战争。 */
  diplomacyStatus: string;
  /** 独立 / 内藩 / 外藩。 */
  politicalStatus: string;
  arrearsMoney: string;
  arrearsFood: string;
  /** 行着色：allied | enemy | neutral */
  diplomacyTone: string;
}

function forceName(worldState: StrategyWorldState, forceId: number): string {
  if (forceId === 0) return "—";
  return worldState.forces.find((f) => f.id === forceId)?.name ?? `势力#${forceId}`;
}

function formatDefense(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n).toLocaleString() : "—";
}

function statPercent(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? String(Math.trunc(n)) : "—";
}

function formatPersonStatValue(value: unknown, worldState: StrategyWorldState): string {
  return isCharacterFogMode(worldState) ? formatStatBand(value) : statPercent(value);
}

function formatForeignForceStatValue(
  value: unknown,
  worldState: StrategyWorldState,
  isOwnRealm: boolean,
): string {
  if (!isOwnRealm && isRestrictedIntelMode(worldState)) {
    return formatStatBand(value);
  }
  return statPercent(value);
}

function safePopulation(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n).toLocaleString() : "—";
}

function oxMark(value: boolean): string {
  return value ? "○" : "×";
}

function sexLabel(value: string | undefined | null): string {
  switch (value) {
    case "Male":
      return "男";
    case "Female":
      return "女";
    default:
      return value?.trim() ? value : "—";
  }
}

function birthTypeLabel(value: string | undefined | null): string {
  switch (value) {
    case "RoyalFamily":
      return "皇族";
    case "Noble":
      return "贵族";
    case "Landlord":
      return "勋贵";
    case "Normal":
      return "平民";
    case "Slave":
      return "奴隶";
    default:
      return value?.trim() ? value : "—";
  }
}

function healthStatusLabel(isSick: boolean | undefined | null): string {
  return isSick ? "生病" : "健康";
}

function formatTaskRemainingDays(
  character: StrategyCharacterSummaryState
): string {
  if (character.forceStatus !== "Task") return "—";
  const days = character.taskRemainingDays;
  if (!Number.isFinite(Number(days))) return "—";
  return `${Math.max(0, Math.trunc(Number(days)))}日`;
}

function countStrongholdCharacters(
  worldState: StrategyWorldState,
  strongholdId: number
): number {
  const { characters } = worldState;
  if (!characters?.length) return 0;
  return characters.filter(
    (c) => !c.isDead && (c.strongholdId ?? 0) === strongholdId
  ).length;
}

function countStrongholdCharactersInCity(
  worldState: StrategyWorldState,
  strongholdId: number
): number {
  const { characters } = worldState;
  if (!characters?.length) return 0;
  return characters.filter(
    (c) =>
      !c.isDead &&
      c.locationType === "Stronghold" &&
      (c.strongholdId ?? 0) === strongholdId
  ).length;
}

function formatCityGenerals(worldState: StrategyWorldState, strongholdId: number): string {
  const inCity = countStrongholdCharactersInCity(worldState, strongholdId);
  const total = countStrongholdCharacters(worldState, strongholdId);
  return `${inCity}/${total}`;
}

/** 据点城内将领数，格式如 2/5（在城/所属）。 */
export function formatStrongholdCityGenerals(
  worldState: StrategyWorldState,
  strongholdId: number
): string {
  return formatCityGenerals(worldState, strongholdId);
}

function resolveSuccessorName(
  worldState: StrategyWorldState,
  forceId: number
): string {
  const force = worldState.forces.find((f) => f.id === forceId);
  const successorId = force?.successorId;
  if (successorId == null || successorId <= 0) return "—";
  const character = worldState.characters?.find((c) => c.id === successorId);
  return character?.name?.trim() || `人物#${successorId}`;
}

function lookupMasterCulture(
  worldState: StrategyWorldState,
  cultureName: string | undefined | null
) {
  const name = cultureName?.trim();
  if (!name || !worldState.masterData?.cultures?.length) return null;
  return worldState.masterData.cultures.find((c) => c.name?.trim() === name) ?? null;
}

function lookupMasterReligion(
  worldState: StrategyWorldState,
  religionName: string | undefined | null
) {
  const name = religionName?.trim();
  if (!name || !worldState.masterData?.religions?.length) return null;
  return worldState.masterData.religions.find((r) => r.name?.trim() === name) ?? null;
}

/** 占位：未来增减益系统。 */
export function entityEffectsIntelRows(): IntelFieldRow[] {
  return [{ label: "状态", value: "暂无增减益（系统后续实装）" }];
}

function resolveCultureGroupName(
  worldState: StrategyWorldState,
  cultureName: string | undefined | null
): string {
  return lookupMasterCulture(worldState, cultureName)?.group?.trim() || "—";
}

function resolveReligionGroupName(
  worldState: StrategyWorldState,
  religionName: string | undefined | null
): string {
  return lookupMasterReligion(worldState, religionName)?.group?.trim() || "—";
}

function enrichCultureReligionGroupRows(
  worldState: StrategyWorldState,
  rows: IntelFieldRow[]
): IntelFieldRow[] {
  const result: IntelFieldRow[] = [];
  for (const row of rows) {
    result.push(row);
    if (row.label === "文化") {
      result.push({
        label: "文化圈",
        value: resolveCultureGroupName(worldState, row.value),
      });
    } else if (row.label === "信仰") {
      result.push({
        label: "宗教",
        value: resolveReligionGroupName(worldState, row.value),
      });
    }
  }
  return result;
}

export function cultureDetailIntelRows(
  worldState: StrategyWorldState,
  cultureName: string | undefined | null
): IntelFieldRow[] {
  const entry = lookupMasterCulture(worldState, cultureName);
  if (!entry) {
    return [
      { label: "文化", value: cultureName?.trim() || "—" },
      { label: "说明", value: "暂无详细文化属性" },
    ];
  }
  return [
    { label: "文化", value: entry.name },
    { label: "文化圈", value: entry.group?.trim() || "—" },
    { label: "说明", value: entry.description?.trim() || "—" },
  ];
}

export function religionDetailIntelRows(
  worldState: StrategyWorldState,
  religionName: string | undefined | null
): IntelFieldRow[] {
  const entry = lookupMasterReligion(worldState, religionName);
  if (!entry) {
    return [
      { label: "信仰", value: religionName?.trim() || "—" },
      { label: "说明", value: "暂无详细信仰属性" },
    ];
  }
  const rows: IntelFieldRow[] = [
    { label: "信仰", value: entry.name },
    { label: "宗教", value: entry.group?.trim() || "—" },
    { label: "说明", value: entry.description?.trim() || "—" },
  ];
  if (entry.extra?.trim()) {
    rows.push({ label: "属性", value: entry.extra.trim() });
  }
  return rows;
}

export function forceCultureDetailRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelFieldRow[] {
  const row = findForceRow(worldState, forceId);
  return cultureDetailIntelRows(worldState, row?.cultureName);
}

export function forceReligionDetailRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelFieldRow[] {
  const row = findForceRow(worldState, forceId);
  return religionDetailIntelRows(worldState, row?.religionName);
}

export function strongholdCultureDetailRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelFieldRow[] {
  const sh = worldState.strongholds.find((s) => s.id === strongholdId);
  return cultureDetailIntelRows(worldState, sh?.cultureName);
}

export function strongholdReligionDetailRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelFieldRow[] {
  const sh = worldState.strongholds.find((s) => s.id === strongholdId);
  return religionDetailIntelRows(worldState, sh?.religionName);
}

function formatYearsInForce(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? `${Math.max(0, Math.trunc(n))}年` : "—";
}

function diplomacyRelationScore(value: number | undefined | null): string {
  if (!Number.isFinite(Number(value))) return "—";
  return String(Math.trunc(Number(value)));
}

function diplomacyStatusLabel(relation: string | undefined | null): string {
  switch (relation) {
    case "Allied":
      return "同盟";
    case "Enemy":
      return "战争";
    case "Neutral":
      return "中立";
    default:
      return "—";
  }
}

function diplomacyToneFromRelation(relation: string | undefined | null): string {
  switch (relation) {
    case "Allied":
      return "allied";
    case "Enemy":
      return "enemy";
    case "Neutral":
      return "neutral";
    default:
      return "neutral";
  }
}

function strongholdMaintenanceMoney(population: unknown): string {
  const n = Number(population);
  const value = Number.isFinite(n) ? Math.max(800, Math.trunc(n) / 5) : 800;
  return formatMoney(value);
}

/** 从人物姓名提取氏前缀（如「北条氏康」→「北条」）。 */
function extractClanPrefix(name: string | undefined | null): string {
  const trimmed = name?.trim() ?? "";
  if (!trimmed) return "";
  const shiIndex = trimmed.indexOf("氏");
  if (shiIndex > 0) return trimmed.slice(0, shiIndex);
  return trimmed.length >= 2 ? trimmed.slice(0, 2) : trimmed;
}

function personIsFamilyMember(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): boolean {
  const lordName = resolveForceLordName(worldState, character.forceId);
  const lordClan = extractClanPrefix(lordName);
  const charClan = extractClanPrefix(character.name);
  if (!lordClan || !charClan) return false;
  return lordClan === charClan;
}

function resolveForceCultureName(worldState: StrategyWorldState, forceId: number): string {
  const residence = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isLordResidence
  );
  if (residence?.cultureName?.trim()) return residence.cultureName.trim();
  const first = worldState.strongholds.find((s) => s.forceId === forceId);
  return first?.cultureName?.trim() || "—";
}

function resolveForceReligionName(worldState: StrategyWorldState, forceId: number): string {
  const residence = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isLordResidence
  );
  if (residence?.religionName?.trim()) return residence.religionName.trim();
  const first = worldState.strongholds.find((s) => s.forceId === forceId);
  return first?.religionName?.trim() || "—";
}

function personStrongholdName(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  const strongholdId = character.strongholdId ?? 0;
  if (strongholdId <= 0) return "—";
  const sh = worldState.strongholds.find((s) => s.id === strongholdId);
  return sh?.name?.trim() || "—";
}

function lookupPlayerDiplomacy(
  worldState: StrategyWorldState,
  targetRootId: number
) {
  return worldState.diplomacies.find((d) => d.targetForceId === targetRootId);
}

function forcePoliticalStatusLabel(status: string | undefined | null): string {
  switch (status) {
    case "Independence":
      return "独立";
    case "InnerVassal":
      return "内藩";
    case "OuterVassal":
      return "外藩";
    default:
      return status?.trim() ? status : "—";
  }
}

function personStatusLabel(status: string | undefined | null): string {
  switch (status) {
    case "Idle":
      return "空闲";
    case "UnitAction":
      return "出阵";
    case "Task":
      return "任务中";
    case "Prisoner":
      return "俘虏";
    default:
      return status?.trim() ? status : "—";
  }
}

function personLocationTypeLabel(value: string | undefined | null): string {
  switch (value) {
    case "Stronghold":
      return "据点";
    case "Unit":
      return "部队";
    case "Map":
      return "地图";
    default:
      return value?.trim() ? value : "—";
  }
}

function playerRelationLabel(
  worldState: StrategyWorldState,
  targetRootId: number
): string {
  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  if (targetRootId === playerRoot) return "自势力";
  const diplomacy = lookupPlayerDiplomacy(worldState, targetRootId);
  return diplomacyStatusLabel(diplomacy?.relation ?? null);
}

function resolveForceLordName(worldState: StrategyWorldState, forceId: number): string {
  if (forceId === worldState.playerForceId) {
    return worldState.lord.name?.trim() || "—";
  }

  const residence = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isLordResidence
  );
  if (residence?.lordName?.trim()) return residence.lordName.trim();

  const force = worldState.forces.find((f) => f.id === forceId);
  if (force?.lordResidenceStrongholdId) {
    const sh = worldState.strongholds.find((s) => s.id === force.lordResidenceStrongholdId);
    if (sh?.lordName?.trim()) return sh.lordName.trim();
  }

  const directRule = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isDirectRule
  );
  return directRule?.lordName?.trim() || "—";
}

function resolveForceResidenceName(
  worldState: StrategyWorldState,
  force: StrategyForceState
): string {
  if (force.lordResidenceStrongholdId) {
    const sh = worldState.strongholds.find((s) => s.id === force.lordResidenceStrongholdId);
    if (sh?.name) return sh.name;
  }

  const residence = worldState.strongholds.find(
    (s) => s.forceId === force.id && s.isLordResidence
  );
  if (residence?.name) return residence.name;

  if (force.id === worldState.playerForceId) {
    return worldState.lord.residenceStrongholdName?.trim() || "—";
  }

  return "—";
}

function countForceOwnSoldiers(worldState: StrategyWorldState, forceId: number): number {
  let total = 0;
  for (const sh of worldState.strongholds) {
    if (sh.forceId !== forceId) continue;
    total += Number.isFinite(Number(sh.garrisonSoldiers))
      ? Math.max(0, Math.trunc(Number(sh.garrisonSoldiers)))
      : 0;
  }
  for (const unit of worldState.units) {
    if (unit.forceId !== forceId) continue;
    total += Number.isFinite(Number(unit.soldiers))
      ? Math.max(0, Math.trunc(Number(unit.soldiers)))
      : 0;
  }
  return total;
}

function countForceOwnCharacters(worldState: StrategyWorldState, forceId: number): number {
  const { characters, strongholds, units, lord } = worldState;
  if (characters?.length) {
    return characters.filter((c) => !c.isDead && c.forceId === forceId).length;
  }
  return countOwnCharacters(forceId, strongholds, units, {
    characters,
    lordName: forceId === worldState.playerForceId ? lord.name : resolveForceLordName(worldState, forceId),
  });
}

/** 封地合计(本势力)，与画面顶部势力状态栏一致。 */
function formatForceRealmStatCount(
  worldState: StrategyWorldState,
  forceId: number,
  kind: "stronghold" | "character",
): string {
  const { forces, strongholds, units, characters, lord } = worldState;
  const realmRootId = resolveRealmRootId(forceId, forces);
  const lordName =
    realmRootId === worldState.playerForceId
      ? lord.name
      : resolveForceLordName(worldState, realmRootId);

  if (kind === "stronghold") {
    const realm = countRealmStrongholds(realmRootId, forces, strongholds);
    const own = countOwnStrongholds(forceId, strongholds);
    return `${realm}(${own})`;
  }

  const realm = countRealmCharacters(realmRootId, forces, strongholds, units, {
    characters,
    forceCharacterCount: forces.find((f) => f.id === realmRootId)?.characterCount,
    lordName,
  });
  const own = countForceOwnCharacters(worldState, forceId);
  return `${realm}(${own})`;
}

function buildDiplomacyRow(
  worldState: StrategyWorldState,
  force: StrategyForceState
): IntelDiplomacyRow {
  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  let dipTargetRoot = resolveRealmRootId(force.id, worldState.forces);

  if (
    force.status === "InnerVassal" &&
    force.suzerainForceId != null &&
    force.suzerainForceId !== playerRoot
  ) {
    dipTargetRoot = resolveRealmRootId(force.suzerainForceId, worldState.forces);
  }

  const dip = lookupPlayerDiplomacy(worldState, dipTargetRoot);

  const relation = dip?.relation ?? null;

  return {
    forceId: force.id,
    forceName: forceName(worldState, force.id),
    lordName: resolveForceLordName(worldState, force.id),
    relation: diplomacyRelationScore(dip?.relationship),
    trust: diplomacyRelationScore(dip?.trust),
    diplomacyStatus: diplomacyStatusLabel(relation),
    politicalStatus: forcePoliticalStatusLabel(force.status),
    arrearsMoney: formatMoney(dip?.arrearsMoney ?? force.internalArrearsMoney ?? 0),
    arrearsFood: formatFoodGo(dip?.arrearsFoodGo ?? force.internalArrearsFoodGo ?? 0),
    diplomacyTone: diplomacyToneFromRelation(relation),
  };
}

function buildInnerVassalDiplomacyRow(
  worldState: StrategyWorldState,
  force: StrategyForceState
): IntelDiplomacyRow {
  return {
    forceId: force.id,
    forceName: forceName(worldState, force.id),
    lordName: resolveForceLordName(worldState, force.id),
    relation: "—",
    trust: "—",
    diplomacyStatus: "同盟",
    politicalStatus: "内藩",
    arrearsMoney: formatMoney(force.internalArrearsMoney ?? 0),
    arrearsFood: formatFoodGo(force.internalArrearsFoodGo ?? 0),
    diplomacyTone: "allied",
  };
}

function strongholdCityGarrison(stronghold: StrategyStrongholdState): number {
  return Number.isFinite(Number(stronghold.garrisonSoldiers))
    ? Math.max(0, Math.trunc(Number(stronghold.garrisonSoldiers)))
    : 0;
}

function strongholdTypeLabel(
  stronghold: StrategyStrongholdState,
  worldState: StrategyWorldState,
): string {
  return resolveStrongholdTypeLabel(stronghold, worldState);
}

function strongholdFacilitiesSummary(stronghold: StrategyStrongholdState): string {
  if (!stronghold.defenseFacilities.length) return "无";
  return stronghold.defenseFacilities
    .map(
      (f) =>
        `${defenseFacilityCategoryLabel(f.category)}·${f.name} Lv.${f.level}(+${f.defense})`
    )
    .join("；");
}

function personLocationLabel(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  switch (character.locationType) {
    case "Stronghold": {
      const strongholdId = character.strongholdId ?? 0;
      if (strongholdId <= 0) return "—";
      const sh = worldState.strongholds.find((s) => s.id === strongholdId);
      return sh?.name?.trim() || "—";
    }
    case "Unit": {
      const unit = worldState.units.find(
        (u) =>
          u.commanderId === character.id ||
          u.composition?.some((sub) => sub.commanderId === character.id)
      );
      return unit?.name?.trim() || "—";
    }
    case "Map":
      return "地图";
    default:
      return character.locationType?.trim() ? character.locationType : "—";
  }
}

function personCommandTarget(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  if (character.forceStatus === "Task") return "任务中";
  if (character.forceStatus === "UnitAction") {
    return personLocationLabel(worldState, character);
  }
  if (character.forceStatus === "Prisoner") return "—";
  return "—";
}

function personRoleLabel(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  const name = character.name?.trim();
  if (!name) return "—";

  if (
    character.forceId === worldState.playerForceId &&
    name === worldState.lord.name?.trim()
  ) {
    return "当主";
  }

  const strongholdId = character.strongholdId ?? 0;
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (stronghold) {
    if (stronghold.lordId === character.id || stronghold.lordName?.trim() === name) {
      return stronghold.isLordResidence ? "当主" : "领主";
    }
    if (stronghold.mayorName?.trim() === name) return "代官";
  }

  if (character.locationType === "Unit") return "将";
  if (character.forceStatus === "Prisoner") return "俘虏";
  if (character.forceStatus === "Task") return "奉行";
  return "—";
}

function resolveForceLordDisplayName(worldState: StrategyWorldState, forceId: number): string {
  const force = worldState.forces.find((f) => f.id === forceId);
  if (!force) return "—";
  const lordName = worldState.lord.name?.trim();
  if (forceId === worldState.playerForceId && lordName) return lordName;
  return `${force.name}当主`;
}

function findUnitLedByCharacter(
  worldState: StrategyWorldState,
  characterId: number
): StrategyUnitState | null {
  for (const unit of worldState.units) {
    if (unit.commanderId === characterId) return unit;
    if (unit.composition?.some((entry) => entry.commanderId === characterId)) return unit;
  }
  return null;
}

function personSuperiorLabel(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  const role = personRoleLabel(worldState, character);
  if (role === "当主" || role === "俘虏") return "—";

  if (character.leaderId && character.leaderId > 0) {
    const direct = worldState.characters?.find((c) => c.id === character.leaderId);
    if (direct && !direct.isDead) return direct.name?.trim() || "—";
  }

  if (role === "领主") return resolveForceLordDisplayName(worldState, character.forceId);

  if (role === "代官") {
    const strongholdId = character.strongholdId ?? 0;
    const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
    if (stronghold?.lordName?.trim()) return stronghold.lordName.trim();
    return resolveForceLordDisplayName(worldState, character.forceId);
  }

  if (role === "将") {
    const unit = findUnitLedByCharacter(worldState, character.id);
    if (!unit) return resolveForceLordDisplayName(worldState, character.forceId);
    if (unit.commanderId === character.id) return resolveForceLordDisplayName(worldState, character.forceId);
    if (unit.commanderName?.trim()) return unit.commanderName.trim();
    return resolveForceLordDisplayName(worldState, character.forceId);
  }

  return resolveForceLordDisplayName(worldState, character.forceId);
}

function resolvePlayerLordCharacterId(worldState: StrategyWorldState): number | null {
  const lordName = worldState.lord.name?.trim();
  if (!lordName) return null;

  const match = worldState.characters?.find(
    (c) =>
      !c.isDead &&
      c.forceId === worldState.playerForceId &&
      c.name?.trim() === lordName
  );
  return match?.id ?? null;
}

function resolvePlayerLordStrongholdId(worldState: StrategyWorldState): number | null {
  const { playerForceId, strongholds, lord } = worldState;

  const residence = strongholds.find(
    (s) => s.forceId === playerForceId && s.isLordResidence
  );
  if (residence) return residence.id;

  const residenceName = lord.residenceStrongholdName?.trim();
  if (residenceName) {
    const byName = strongholds.find(
      (s) => s.forceId === playerForceId && s.name?.trim() === residenceName
    );
    if (byName) return byName.id;
  }

  const playerCharacterId = resolvePlayerLordCharacterId(worldState);
  if (playerCharacterId != null) {
    const character = worldState.characters?.find((c) => c.id === playerCharacterId);
    const strongholdId = character?.strongholdId ?? 0;
    if (strongholdId > 0) {
      const affiliated = strongholds.find((s) => s.id === strongholdId);
      if (affiliated) return affiliated.id;
    }
  }

  const atLordPosition = strongholds.find((s) => s.x === lord.x && s.y === lord.y);
  return atLordPosition?.id ?? null;
}

/** 将玩家操控实体（当主 / 居城 / 本家势力）固定排在列表首位，其余按名称排序。 */
function sortIntelRowsPlayerEntityFirst<T extends { id: number; name: string }>(
  rows: T[],
  primaryId: number | null
): T[] {
  return [...rows].sort((a, b) => {
    if (primaryId != null) {
      if (a.id === primaryId && b.id !== primaryId) return -1;
      if (b.id === primaryId && a.id !== primaryId) return 1;
    }
    return a.name.localeCompare(b.name, "zh-Hans");
  });
}

function sortForcesPlayerFirst(
  worldState: StrategyWorldState,
  forces: StrategyForceState[]
): StrategyForceState[] {
  return sortIntelRowsPlayerEntityFirst(forces, worldState.playerForceId);
}

function forceRelationToPlayer(worldState: StrategyWorldState, forceId: number): string {
  const { playerForceId, forces } = worldState;
  const playerRoot = resolveRealmRootId(playerForceId, forces);
  if (forceId === playerForceId) return "自势力";

  const force = worldState.forces.find((f) => f.id === forceId);
  if (
    force?.status === "InnerVassal" &&
    force.suzerainForceId != null &&
    resolveRealmRootId(force.suzerainForceId, forces) === playerRoot
  ) {
    return "内藩";
  }

  const root = resolveRealmRootId(forceId, forces);
  if (root === playerRoot) return "封地";
  return playerRelationLabel(worldState, root);
}

/** 势力 Tab · 势力列表（含全部独立势力，不去重宗主）。 */
export function forceIntelListRows(
  worldState: StrategyWorldState,
  options?: { realmFilter?: IntelRealmFilterMode }
): IntelForceRow[] {
  const { playerForceId, forces } = worldState;
  const realmFilter = options?.realmFilter ?? "all";
  const sorted = sortForcesPlayerFirst(worldState, [...forces]);

  const rows = sorted
    .filter((force) => matchesIntelRealmFilter(force.id, playerForceId, forces, realmFilter))
    .map((force) => {
    const suzerainName =
      force.suzerainForceId != null && force.suzerainForceId > 0
        ? forceName(worldState, force.suzerainForceId)
        : "—";
    const isOwnRealm = isPlayerRealmForce(force.id, playerForceId, forces);

    return {
      id: force.id,
      name: force.name,
      lordName: resolveForceLordName(worldState, force.id),
      residenceName: resolveForceResidenceName(worldState, force),
      cultureName: resolveForceCultureName(worldState, force.id),
      religionName: resolveForceReligionName(worldState, force.id),
      status: forcePoliticalStatusLabel(force.status),
      suzerainName,
      relation: forceRelationToPlayer(worldState, force.id),
      strongholdCount:
        !isOwnRealm && isRestrictedIntelMode(worldState)
          ? "—"
          : formatForceRealmStatCount(worldState, force.id, "stronghold"),
      characterCount:
        !isOwnRealm && isRestrictedIntelMode(worldState)
          ? "—"
          : formatForceRealmStatCount(worldState, force.id, "character"),
      soldiers: formatSoldiers(countForceOwnSoldiers(worldState, force.id)),
      money: formatMoney(force.money),
      food: formatFoodGo(force.food),
      prestige: formatForeignForceStatValue(force.prestige, worldState, isOwnRealm),
      orthodoxy: formatForeignForceStatValue(force.orthodoxy, worldState, isOwnRealm),
      successorName: resolveSuccessorName(worldState, force.id),
      arrearsMoney: formatMoney(force.internalArrearsMoney ?? 0),
      arrearsFood: formatFoodGo(force.internalArrearsFoodGo ?? 0),
      isPlayer: force.id === playerForceId,
      isOwnRealm,
    };
  });

  return rows;
}

/** 据点 Tab：据点列表（全字段）。 */
export function strongholdIntelRows(
  worldState: StrategyWorldState,
  options?: { realmFilter?: IntelRealmFilterMode }
): IntelStrongholdRow[] {
  const { playerForceId, forces, strongholds } = worldState;
  const realmFilter = options?.realmFilter ?? "all";
  const rows = strongholds
    .filter((sh) =>
      matchesIntelRealmFilter(sh.forceId, playerForceId, forces, realmFilter)
    )
    .map((sh) => {
      const garrison = strongholdCityGarrison(sh);
      const siegeThreat = sh.siegeThreat ?? null;
      const masked = shouldObscureStrongholdPersonnel(worldState, sh);
      const obscurePersonnel = masked;
      return {
        id: sh.id,
        name: sh.name,
        forceName: forceName(worldState, sh.forceId),
        position: `(${sh.x}, ${sh.y})`,
        category: strongholdTypeLabel(sh, worldState),
        scale: strongholdHoverFieldValue(worldState, sh, "规模", resolveStrongholdScaleLabel(sh.population)),
        lordName: obscurePersonnel ? UNKNOWN_INTEL : (sh.lordName?.trim() || "—"),
        mayorName: obscurePersonnel ? UNKNOWN_INTEL : (sh.mayorName?.trim() || "—"),
        population: strongholdHoverFieldValue(worldState, sh, "人口", safePopulation(sh.population)),
        stability: strongholdHoverFieldValue(worldState, sh, "治安", statPercent(sh.stability)),
        popularFeelings: strongholdHoverFieldValue(worldState, sh, "民心", statPercent(sh.popularFeelings)),
        maintenance: strongholdHoverFieldValue(
          worldState,
          sh,
          "维护费",
          strongholdMaintenanceMoney(sh.population)
        ),
        cityGenerals: strongholdHoverFieldValue(
          worldState,
          sh,
          "城内将",
          formatCityGenerals(worldState, sh.id)
        ),
        wounded: strongholdHoverFieldValue(worldState, sh, "负伤", formatSoldiers(sh.garrisonWounded ?? 0)),
        morale: strongholdHoverFieldValue(worldState, sh, "士气", statPercent(sh.morale)),
        training: strongholdHoverFieldValue(worldState, sh, "训练度", statPercent(sh.training)),
        cultureName: strongholdHoverFieldValue(worldState, sh, "文化", sh.cultureName?.trim() || "—"),
        religionName: strongholdHoverFieldValue(worldState, sh, "信仰", sh.religionName?.trim() || "—"),
        defense: strongholdHoverFieldValue(worldState, sh, "城防", formatDefense(sh.defense)),
        garrisonTotal: strongholdHoverFieldValue(worldState, sh, "兵力", formatSoldiers(garrison)),
        money: strongholdHoverFieldValue(worldState, sh, "金钱", formatMoney(sh.money)),
        food: strongholdHoverFieldValue(worldState, sh, "粮食", formatFoodGo(sh.food)),
        pollTaxRate: strongholdHoverFieldValue(worldState, sh, "人头税", `${statPercent(sh.pollTaxRate)}%`),
        agricultureTaxRate: strongholdHoverFieldValue(
          worldState,
          sh,
          "农业税",
          `${statPercent(sh.agricultureTaxRate)}%`
        ),
        commerceTaxRate: strongholdHoverFieldValue(
          worldState,
          sh,
          "商业税",
          `${statPercent(sh.commerceTaxRate)}%`
        ),
        tariffTaxRate: strongholdHoverFieldValue(worldState, sh, "关税", `${statPercent(sh.tariffTaxRate)}%`),
        isLordResidence: oxMark(sh.isLordResidence),
        isFictional: oxMark(!sh.isHistorical),
        isAssault: oxMark(siegeThreat === "Assault"),
        isEncircle: oxMark(siegeThreat === "Encircle"),
        facilities: strongholdHoverFieldValue(
          worldState,
          sh,
          "设施",
          strongholdFacilitiesSummary(sh)
        ),
      };
    });

  return sortIntelRowsPlayerEntityFirst(rows, resolvePlayerLordStrongholdId(worldState));
}

/** 人物 Tab：人物列表（全字段）。 */
export function personIntelRows(
  worldState: StrategyWorldState,
  options?: { realmFilter?: IntelRealmFilterMode }
): IntelPersonRow[] {
  const { playerForceId, forces, characters } = worldState;
  const realmFilter = options?.realmFilter ?? "all";
  if (!characters?.length) return [];

  const rows = characters
    .filter(
      (c) =>
        !c.isDead &&
        matchesIntelRealmFilter(c.forceId, playerForceId, forces, realmFilter)
    )
    .map((c) => {
      const p = c.personality;
      const s = c.proficiency;
      const hobbies = hobbyCategoryValues(p?.hobby, c.id);
      return {
        id: c.id,
        name: c.name?.trim() || `人物#${c.id}`,
        forceName: forceName(worldState, c.forceId),
        strongholdName: personStrongholdName(worldState, c),
        isFamily: oxMark(personIsFamilyMember(worldState, c)),
        role: personRoleLabel(worldState, c),
        superior: personSuperiorLabel(worldState, c),
        location: personLocationLabel(worldState, c),
        locationType: personLocationTypeLabel(c.locationType),
        leadership: formatPersonStatValue(c.leadership, worldState),
        power: formatPersonStatValue(c.power, worldState),
        politics: formatPersonStatValue(c.politics, worldState),
        strategy: formatPersonStatValue(c.strategy, worldState),
        charm: formatPersonStatValue(c.charm, worldState),
        loyalty: statPercent(c.loyalty ?? p?.friendship),
        status: personStatusLabel(c.forceStatus),
        healthStatus: healthStatusLabel(c.isSick),
        commandTarget: personCommandTarget(worldState, c),
        taskRemainingDays: formatTaskRemainingDays(c),
        yearsInForce: formatYearsInForce(c.yearsInForce),
        cultureName: c.cultureName?.trim() || "—",
        religionName: c.religionName?.trim() || "—",
        sex: sexLabel(c.sex),
        age: statPercent(c.age),
        birthType: birthTypeLabel(c.birthType),
        temper: statPercent(p?.temper),
        courage: statPercent(p?.courage),
        principle: statPercent(p?.principle),
        action: statPercent(p?.action),
        friendship: statPercent(p?.friendship),
        ambition: statPercent(p?.ambition),
        hobbyWeapon: statPercent(hobbies.hobbyWeapon),
        hobbyBook: statPercent(hobbies.hobbyBook),
        hobbyArt: statPercent(hobbies.hobbyArt),
        hobbyImport: statPercent(hobbies.hobbyImport),
        hobbyTreasure: statPercent(hobbies.hobbyTreasure),
        desire: statPercent(p?.desire),
        drinking: statPercent(p?.drinking),
        fortune: statPercent(p?.fortune),
        skillInfantry: formatPersonStatValue(s?.infantry, worldState),
        skillRide: formatPersonStatValue(s?.ride, worldState),
        skillArchery: formatPersonStatValue(s?.archery, worldState),
        skillFirelock: formatPersonStatValue(s?.firelock, worldState),
        skillSealing: formatPersonStatValue(s?.sealing, worldState),
        skillMilitary: formatPersonStatValue(s?.military, worldState),
        skillFighting: formatPersonStatValue(s?.fighting, worldState),
        skillSpy: formatPersonStatValue(s?.spy, worldState),
        skillAgriculture: formatPersonStatValue(s?.agriculture, worldState),
        skillCommerce: formatPersonStatValue(s?.commerce, worldState),
        skillConstruct: formatPersonStatValue(s?.construct, worldState),
        skillSmelt: formatPersonStatValue(s?.smelt, worldState),
        skillEloquence: formatPersonStatValue(s?.eloquence, worldState),
        skillCourt: formatPersonStatValue(s?.court, worldState),
        skillSociality: formatPersonStatValue(s?.sociality, worldState),
        skillHealing: formatPersonStatValue(s?.healing, worldState),
      };
    });

  return sortIntelRowsPlayerEntityFirst(rows, resolvePlayerLordCharacterId(worldState));
}

/** @deprecated 使用 strongholdIntelRows */
export const playerStrongholdRows = strongholdIntelRows;

/** 据点详情 · 城防设施表行。 */
export function strongholdDefenseFacilityTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelDefenseFacilityRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold?.defenseFacilities.length) return [];

  return stronghold.defenseFacilities.map((facility) => ({
    category: defenseFacilityCategoryLabel(facility.category),
    name: facility.name?.trim() || "—",
    level: statPercent(facility.level),
    defense: formatDefense(facility.defense),
  }));
}
/** @deprecated 使用 personIntelRows */
export const playerPersonRows = personIntelRows;

/** 势力 Tab · 外交列表（自势力视角）。 */
export function diplomacyIntelRows(worldState: StrategyWorldState): IntelDiplomacyRow[] {
  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  const rows: IntelDiplomacyRow[] = [];

  for (const force of worldState.forces) {
    if (isPlayerRealmForce(force.id, playerRoot, worldState.forces)) {
      if (
        force.id !== playerRoot &&
        force.status === "InnerVassal" &&
        force.suzerainForceId === playerRoot
      ) {
        rows.push(buildInnerVassalDiplomacyRow(worldState, force));
      }
      continue;
    }

    rows.push(buildDiplomacyRow(worldState, force));
  }

  return rows.sort((a, b) => a.forceName.localeCompare(b.forceName, "zh-Hans"));
}

export function findForceRow(
  worldState: StrategyWorldState,
  forceId: number | null
): IntelForceRow | null {
  if (forceId == null) return null;
  return forceIntelListRows(worldState).find((row) => row.id === forceId) ?? null;
}

export function findStrongholdRow(
  worldState: StrategyWorldState,
  strongholdId: number | null
): IntelStrongholdRow | null {
  if (strongholdId == null) return null;
  return strongholdIntelRows(worldState).find((row) => row.id === strongholdId) ?? null;
}

export function findPersonRow(
  worldState: StrategyWorldState,
  personId: number | null
): IntelPersonRow | null {
  if (personId == null) return null;
  return personIntelRows(worldState).find((row) => row.id === personId) ?? null;
}

/** 势力详情 · 基本（完整字段）。 */
export function forceDetailIntelRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelFieldRow[] {
  const row = findForceRow(worldState, forceId);
  if (!row) return [];

  return [
    { label: "名称", value: row.name },
    { label: "当主", value: row.lordName },
    { label: "居城", value: row.residenceName },
    { label: "文化", value: row.cultureName },
    { label: "信仰", value: row.religionName },
    { label: "状态", value: row.status },
    { label: "宗主", value: row.suzerainName },
    { label: "继承人", value: row.successorName },
    { label: "兵力", value: row.soldiers },
    { label: "据点数", value: row.strongholdCount },
    { label: "现任", value: row.characterCount },
    { label: "金钱", value: row.money },
    { label: "粮食", value: row.food },
    { label: "威望", value: row.prestige },
    { label: "正统", value: row.orthodoxy },
  ];
}

/** 某势力视角的外交列表（封地势力展示全表，他势力仅展示与己关系）。 */
export function diplomacyForForceRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelDiplomacyRow[] {
  if (isPlayerRealmForce(forceId, worldState.playerForceId, worldState.forces)) {
    return diplomacyIntelRows(worldState);
  }

  const selectedRoot = resolveRealmRootId(forceId, worldState.forces);
  const diplomacy = lookupPlayerDiplomacy(worldState, selectedRoot);
  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  const playerForce = worldState.forces.find((f) => f.id === playerRoot);

  const relation = diplomacy?.relation ?? null;

  return [
    {
      forceId: playerRoot,
      forceName: forceName(worldState, playerRoot),
      lordName: resolveForceLordName(worldState, playerRoot),
      relation: diplomacyRelationScore(diplomacy?.relationship),
      trust: diplomacyRelationScore(diplomacy?.trust),
      diplomacyStatus: diplomacyStatusLabel(relation),
      politicalStatus: forcePoliticalStatusLabel(playerForce?.status),
      arrearsMoney: formatMoney(diplomacy?.arrearsMoney ?? playerForce?.internalArrearsMoney ?? 0),
      arrearsFood: formatFoodGo(diplomacy?.arrearsFoodGo ?? playerForce?.internalArrearsFoodGo ?? 0),
      diplomacyTone: diplomacyToneFromRelation(relation),
    },
  ];
}

export function forceIntroText(worldState: StrategyWorldState, forceId: number): string {
  const row = findForceRow(worldState, forceId);
  if (!row) return "暂无该势力介绍。";

  const lines = [
    `${row.name}由${row.lordName}统领，当主驻于${row.residenceName}。`,
    `封地据${row.strongholdCount}座，现任${row.characterCount}名，兵力${row.soldiers}。`,
    `金钱${row.money}、粮食${row.food}；威望${row.prestige}、正统${row.orthodoxy}。`,
    `与自势力关系：${row.relation}；政治地位：${row.status}。`,
  ];
  if (row.suzerainName !== "—") {
    lines.push(`宗主为${row.suzerainName}。`);
  }
  lines.push("（完整势力传记与情报迷雾系统后续实装。）");
  return lines.join("");
}

export function strongholdDetailFieldRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelFieldRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];
  return strongholdDetailIntelRows(worldState, stronghold, { includeBattleIntel: false });
}

export function strongholdIntroText(
  worldState: StrategyWorldState,
  strongholdId: number
): string {
  const row = findStrongholdRow(worldState, strongholdId);
  if (!row) return "暂无该据点介绍。";

  return [
    `${row.name}隶属${row.forceName}，${row.category}。`,
    `领主${row.lordName}${row.mayorName !== "—" ? `，代官${row.mayorName}` : ""}。`,
    `人口${row.population}，城防${row.defense}，兵力${row.garrisonTotal}。`,
    `粮食${row.food}、金钱${row.money}；文化${row.cultureName}、信仰${row.religionName}。`,
    row.isAssault === "○" || row.isEncircle === "○"
      ? `当前${row.isAssault === "○" ? "正遭战斗" : ""}${row.isAssault === "○" && row.isEncircle === "○" ? "且" : ""}${row.isEncircle === "○" ? "正遭包围" : ""}。`
      : "当前未遭进攻。",
    "（据点传记与情报迷雾系统后续实装。）",
  ].join("");
}

/** 人物详情 · 属性（五维；性格类仅开发阶段）。 */
export function personStatDetailRows(
  worldState: StrategyWorldState,
  personId: number
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId);
  if (!row) return [];

  const rows: IntelFieldRow[] = [
    { label: "统率", value: row.leadership },
    { label: "武勇", value: row.power },
    { label: "政治", value: row.politics },
    { label: "智谋", value: row.strategy },
    { label: "魅力", value: row.charm },
  ];

  if (!isIntelDevFieldsVisible()) return rows;

  rows.push(
    { label: personalityFieldLabel("temper"), value: row.temper, dev: true },
    { label: personalityFieldLabel("courage"), value: row.courage, dev: true },
    { label: personalityFieldLabel("principle"), value: row.principle, dev: true },
    { label: personalityFieldLabel("action"), value: row.action, dev: true },
    { label: personalityFieldLabel("friendship"), value: row.friendship, dev: true },
    { label: personalityFieldLabel("ambition"), value: row.ambition, dev: true },
    { label: hobbyCategoryLabel("hobbyWeapon"), value: row.hobbyWeapon, dev: true },
    { label: hobbyCategoryLabel("hobbyBook"), value: row.hobbyBook, dev: true },
    { label: hobbyCategoryLabel("hobbyArt"), value: row.hobbyArt, dev: true },
    { label: hobbyCategoryLabel("hobbyImport"), value: row.hobbyImport, dev: true },
    { label: hobbyCategoryLabel("hobbyTreasure"), value: row.hobbyTreasure, dev: true },
    { label: personalityFieldLabel("desire"), value: row.desire, dev: true },
    { label: personalityFieldLabel("drinking"), value: row.drinking, dev: true },
    { label: personalityFieldLabel("fortune"), value: row.fortune, dev: true }
  );
  return rows;
}

/** 人物详情 · 能力（16 项技能）。 */
export function personSkillDetailRows(
  worldState: StrategyWorldState,
  personId: number
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId);
  if (!row) return [];

  return [
    { label: "步兵", value: row.skillInfantry },
    { label: "骑马", value: row.skillRide },
    { label: "弓术", value: row.skillArchery },
    { label: "火枪", value: row.skillFirelock },
    { label: "航海", value: row.skillSealing },
    { label: "军略", value: row.skillMilitary },
    { label: "战斗", value: row.skillFighting },
    { label: "谍报", value: row.skillSpy },
    { label: "农业", value: row.skillAgriculture },
    { label: "商业", value: row.skillCommerce },
    { label: "建筑", value: row.skillConstruct },
    { label: "冶炼", value: row.skillSmelt },
    { label: "辩才", value: row.skillEloquence },
    { label: "宫廷", value: row.skillCourt },
    { label: "交际", value: row.skillSociality },
    { label: "医术", value: row.skillHealing },
  ];
}

export function personDetailIntelRows(
  worldState: StrategyWorldState,
  personId: number
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId);
  if (!row) return [];

  return enrichCultureReligionGroupRows(worldState, [
    { label: "姓名", value: row.name },
    { label: "势力", value: row.forceName },
    { label: "据点", value: row.strongholdName },
    { label: "职位", value: row.role },
    { label: "一门", value: row.isFamily },
    { label: "仕官", value: row.yearsInForce },
    { label: "所在", value: row.location },
    { label: "位置类型", value: row.locationType },
    { label: "状态", value: row.status },
    { label: "健康", value: row.healthStatus },
    { label: "忠诚", value: row.loyalty },
    { label: "命令对象", value: row.commandTarget },
    { label: "出身", value: row.birthType },
    { label: "文化", value: row.cultureName },
    { label: "信仰", value: row.religionName },
    { label: "性别", value: row.sex },
    { label: "年龄", value: row.age },
  ]);
}

export function personIntroText(worldState: StrategyWorldState, personId: number): string {
  const row = findPersonRow(worldState, personId);
  if (!row) return "暂无该人物介绍。";

  return [
    `${row.name}仕于${row.forceName}，${row.role}。`,
    `仕官${row.yearsInForce}；统率${row.leadership}、武勇${row.power}，当前${row.status}。`,
    `驻在${row.location}（${row.locationType}）；文化${row.cultureName}、信仰${row.religionName}。`,
    "（人物传记与情报迷雾系统后续实装。）",
  ].join("");
}

export interface IntelMasterDataRow {
  id: number;
  name: string;
  [key: string]: string | number;
}

function mapMasterEntries(
  entries:
    | Array<{
        id?: number;
        name?: string | null;
        group?: string | null;
        description?: string | null;
        extra?: string | null;
        fields?: Record<string, string | null | undefined> | null;
      }>
    | undefined
): IntelMasterDataRow[] {
  if (!entries?.length) return [];
  return entries.map((entry) => {
    const row: IntelMasterDataRow = {
      id: Number(entry.id ?? 0),
      name: entry.name?.trim() || "—",
    };

    const fields = entry.fields ?? {};
    for (const [key, raw] of Object.entries(fields)) {
      row[key] = raw == null || String(raw).trim() === "" ? "—" : String(raw).trim();
    }

    if (Object.keys(fields).length === 0) {
      if (entry.description?.trim()) row.description = entry.description.trim();
      if (entry.group?.trim()) {
        const group = entry.group.trim();
        row.cultureGroup = group;
        row.religionGroup = group;
        row.category = group;
      }
      if (entry.extra?.trim()) {
        const extra = entry.extra.trim();
        row.terrainType = extra;
        row.extra = extra;
      }
    } else {
      if (entry.group?.trim()) row.group = entry.group.trim();
      if (entry.description?.trim() && row.description == null) row.description = entry.description.trim();
      if (entry.extra?.trim()) row.extra = entry.extra.trim();
    }

    return row;
  });
}

/** Master Data Tab · 指定分类列表行。 */
export function masterDataIntelRows(
  worldState: StrategyWorldState,
  preset: MasterDataListPreset
): IntelMasterDataRow[] {
  const master = worldState.masterData;
  if (!master) return [];
  if (preset === "enums") return mapMasterEntries(master.enums);
  return mapMasterEntries(master[preset]);
}

export interface IntelPersonRelationRow {
  id: number;
  relationType: string;
  characterName: string;
  characterId: number;
}

/** 人物详情 · 人际关系表格行。 */
export function personRelationshipTableRows(
  worldState: StrategyWorldState,
  characterId: number,
): IntelPersonRelationRow[] {
  const character = worldState.characters?.find((c) => c.id === characterId);
  if (!character?.relations?.length) return [];

  return character.relations.map((rel, index) => ({
    id: index + 1,
    relationType: rel.relationType,
    characterName: rel.characterName,
    characterId: rel.characterId,
  }));
}

/** @deprecated 使用 playerPersonRows */
export const playerCharacterRows = playerPersonRows;
