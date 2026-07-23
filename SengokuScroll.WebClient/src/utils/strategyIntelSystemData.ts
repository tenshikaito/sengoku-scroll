import type {
  StrategyCharacterSummaryState,
  StrategyCropCycleState,
  StrategyEntityEffectState,
  StrategyEntityTechnologyState,
  StrategyForceState,
  StrategyStrongholdCityActorState,
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
  birthTypeLabel,
  diplomacyAffinityTierLabel,
  diplomacyStatusLabel,
  diplomacyTrustTierLabel,
  diplomacyToneFromRelation,
  forcePoliticalStatusLabel,
  personLocationTypeLabel,
  personStatusLabel,
  sexLabel,
} from "@/intelDisplay/IntelDisplayBehaviors";
import {
  organizationPrimaryRoleLabel,
  organizationRoleLabelAtIndex,
} from "@/intelDisplay/OrganizationRoleLabelBehavior";
import { taskCategoryLabel } from "@/utils/characterIntelDisplay";
import { findCharacterCityActor, findCharacterCityActorStrongholdName } from "@/utils/personCityActorLookup";
import { personLocationLabel } from "@/intelDisplay/PersonLocationBehavior";
import {
  personCommandTarget,
  personRoleLabel,
} from "@/intelDisplay/PersonForceStatusBehaviors";
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
import { isPersonIntelVisible } from "@/utils/strategyIntelVisibility";

export interface IntelDataOptions {
  realmFilter?: IntelRealmFilterMode;
  intelDebugMode?: boolean;
}
import { matchesIntelRealmFilter } from "@/utils/intelRealmFilter";
import { isIntelDevFieldsVisible } from "@/utils/strategyIntelDev";
import type { MasterDataListPreset } from "@/utils/strategyIntelSystemColumns";
import {
  hobbyCategoryLabel,
  hobbyCategoryValues,
  personalityFieldLabel,
} from "@/utils/strategyCharacterPersonality";

export type IntelForceCategory = "武家" | "商人" | "寺社";
export type IntelForceCategoryFilter = "all" | IntelForceCategory;

export const INTEL_FORCE_CATEGORY_FILTER_OPTIONS: {
  value: IntelForceCategoryFilter;
  label: string;
}[] = [
  { value: "all", label: "显示全部" },
  { value: "武家", label: "武家" },
  { value: "商人", label: "商人" },
  { value: "寺社", label: "寺社" },
];

export interface IntelForceRow {
  id: number;
  name: string;
  lordName: string;
  forceType: IntelForceCategory;
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

function mapForceCategoryToIntelType(category?: string): IntelForceCategory {
  if (category === "Merchant") return "商人";
  if (category === "Religion") return "寺社";
  return "武家";
}

function isOrganizationForceCategory(category?: string): boolean {
  return category === "Merchant" || category === "Religion";
}

function isOrganizationActorKind(kind: string | undefined): boolean {
  return kind === "Merchant" || kind === "Religion";
}

function matchesOrganizationForceIntelFilter(
  worldState: StrategyWorldState,
  organizationForceId: number,
  realmFilter: IntelRealmFilterMode
): boolean {
  const { playerForceId, forces, strongholds } = worldState;
  for (const stronghold of strongholds) {
    if (!matchesIntelRealmFilter(stronghold.forceId, playerForceId, forces, realmFilter)) {
      continue;
    }
    for (const actor of stronghold.cityActors ?? []) {
      if (actor.forceId === organizationForceId && isOrganizationActorKind(actor.kind)) {
        return true;
      }
    }
  }
  return false;
}

function resolveOrganizationHeadquarterStrongholdId(
  worldState: StrategyWorldState,
  force: StrategyForceState
): number | null {
  const configured = force.lordResidenceStrongholdId;
  if (configured != null && configured > 0) return configured;

  let fallbackStrongholdId: number | null = null;
  for (const stronghold of worldState.strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      if (actor.forceId !== force.id || !isOrganizationActorKind(actor.kind)) continue;
      const branch = actor.branchLabel?.trim();
      const isHeadquarter = branch === "本店" || branch === "本院";
      if (isHeadquarter) return stronghold.id;
      if (fallbackStrongholdId == null || stronghold.id < fallbackStrongholdId) {
        fallbackStrongholdId = stronghold.id;
      }
    }
  }
  return fallbackStrongholdId;
}

function resolveOrganizationForceCultureName(
  worldState: StrategyWorldState,
  force: StrategyForceState
): string {
  const residenceId = resolveOrganizationHeadquarterStrongholdId(worldState, force);
  if (!residenceId) return "—";
  const residence = worldState.strongholds.find((s) => s.id === residenceId);
  return residence?.cultureName?.trim() || "—";
}

function resolveOrganizationForceReligionName(
  worldState: StrategyWorldState,
  force: StrategyForceState
): string {
  const residenceId = resolveOrganizationHeadquarterStrongholdId(worldState, force);
  if (!residenceId) return "—";
  const residence = worldState.strongholds.find((s) => s.id === residenceId);
  const actor = (residence?.cityActors ?? []).find(
    (item) => item.forceId === force.id && isOrganizationActorKind(item.kind)
  );
  if (actor) {
    return resolveCityActorReligionName(actor, residence);
  }
  return residence?.religionName?.trim() || "—";
}

export function filterForceRowsByCategory(
  rows: IntelForceRow[],
  filter: IntelForceCategoryFilter
): IntelForceRow[] {
  if (filter === "all") return rows;
  return rows.filter((row) => row.forceType === filter);
}

const ALL_FORCE_CATEGORIES: IntelForceCategory[] = ["武家", "商人", "寺社"];

export function filterForceRowsByCategories(
  rows: IntelForceRow[],
  categories: ReadonlySet<IntelForceCategory>
): IntelForceRow[] {
  if (categories.size === 0) return [];
  if (categories.size >= ALL_FORCE_CATEGORIES.length) return rows;
  return rows.filter((row) => categories.has(row.forceType));
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
  /** 有效作型。 */
  cropPattern: string;
  /** 劳力可用比。 */
  labor: string;
  /** 当季进度摘要。 */
  cropProgress: string;
  /** 农兵池。 */
  militiaPool: string;
  /** 驻城专业队合计。 */
  standingProfessional: string;
  /** 城内伤兵。 */
  standingWounded: string;
}

export interface IntelPersonRow {
  id: number;
  name: string;
  personType: string;
  personCategory: IntelPersonCategory;
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

export interface IntelStandingGarrisonRow {
  unitName: string;
  typeName: string;
  roleLabel: string;
  isMounted: string;
  soldiers: string;
  morale: string;
  training: string;
  maintenance: string;
}

export interface IntelCropCycleRow {
  name: string;
  period: string;
  progress: string;
  estimatedYield: string;
}

export interface IntelCityActorRow {
  id: number;
  name: string;
  primaryLeaderName: string;
  secondaryLeaderName: string;
  tertiaryLeaderName: string;
  typeLabel: string;
  branchLabel: string;
  characterCount: string;
}


export interface IntelStrongholdFactionProductionRow {
  id: number;
  content: string;
  startTime: string;
  endTime: string;
  efficiency: string;
  status: string;
}

export interface IntelStrongholdTechnologyRow {
  id: number;
  name: string;
  category: string;
  status: string;
  condition: string;
  forceName?: string;
}

export interface IntelDiplomacyRow {
  forceId: number;
  forceName: string;
  lordName: string;
  forceCategory: IntelForceCategory;
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

export interface IntelPersonTaskRow {
  id: number;
  taskType: string;
  name: string;
  target: string;
  status: string;
  remaining: string;
}

export type IntelPersonCategory = "武家" | "商人" | "寺社" | "浪人";
export type IntelPersonCategoryFilter = "all" | IntelPersonCategory;

export const INTEL_PERSON_CATEGORY_FILTER_OPTIONS: {
  value: IntelPersonCategoryFilter;
  label: string;
}[] = [
  { value: "all", label: "显示全部" },
  { value: "武家", label: "武家" },
  { value: "商人", label: "商人" },
  { value: "寺社", label: "寺社" },
  { value: "浪人", label: "浪人" },
];

export type IntelDiplomacyScopeFilter = "all" | "allied" | "enemy" | "neutral" | "innerVassal";

export const INTEL_DIPLOMACY_SCOPE_FILTER_OPTIONS: {
  value: IntelDiplomacyScopeFilter;
  label: string;
}[] = [
  { value: "all", label: "显示全部" },
  { value: "allied", label: "同盟" },
  { value: "enemy", label: "敌对" },
  { value: "neutral", label: "中立" },
  { value: "innerVassal", label: "内藩" },
];

export type ForceDetailScopeOptions = {
  /** 封地根势力详情：是否合并旗下内藩据点/人物。默认 false。 */
  includeInnerVassals?: boolean;
};

const ALL_PERSON_CATEGORIES: IntelPersonCategory[] = ["武家", "商人", "寺社", "浪人"];

export function filterPersonRowsByCategory(
  rows: IntelPersonRow[],
  filter: IntelPersonCategoryFilter
): IntelPersonRow[] {
  if (filter === "all") return rows;
  return rows.filter((row) => row.personCategory === filter);
}

export function filterPersonRowsByCategories(
  rows: IntelPersonRow[],
  categories: ReadonlySet<IntelPersonCategory>
): IntelPersonRow[] {
  if (categories.size === 0) return [];
  if (categories.size >= ALL_PERSON_CATEGORIES.length) return rows;
  return rows.filter((row) => categories.has(row.personCategory));
}

export function forceHasInnerVassals(
  worldState: StrategyWorldState,
  forceId: number
): boolean {
  return worldState.forces.some(
    (force) => force.status === "InnerVassal" && force.suzerainForceId === forceId
  );
}

export function isForceRealmRoot(
  worldState: StrategyWorldState,
  forceId: number
): boolean {
  return resolveRealmRootId(forceId, worldState.forces) === forceId;
}

function collectForceScopeIds(
  worldState: StrategyWorldState,
  forceId: number,
  includeInnerVassals: boolean
): Set<number> {
  const ids = new Set<number>([forceId]);
  if (!includeInnerVassals) return ids;

  for (const force of worldState.forces) {
    if (force.status === "InnerVassal" && force.suzerainForceId === forceId) {
      ids.add(force.id);
    }
  }
  return ids;
}

export function filterDiplomacyRowsByScope(
  rows: IntelDiplomacyRow[],
  filter: IntelDiplomacyScopeFilter
): IntelDiplomacyRow[] {
  if (filter === "all") return rows;
  return rows.filter((row) => {
    if (filter === "innerVassal") return row.politicalStatus === "内藩";
    const tone = row.diplomacyTone ?? "";
    if (filter === "allied") return tone === "allied";
    if (filter === "enemy") return tone === "enemy";
    if (filter === "neutral") return tone === "neutral";
    return true;
  });
}

export function filterDiplomacyRowsByCategory(
  rows: IntelDiplomacyRow[],
  filter: IntelForceCategoryFilter
): IntelDiplomacyRow[] {
  if (filter === "all") return rows;
  return rows.filter((row) => row.forceCategory === filter);
}

function resolveDiplomacyForceCategory(
  worldState: StrategyWorldState,
  forceId: number
): IntelForceCategory {
  const force = worldState.forces.find((item) => item.id === forceId);
  return mapForceCategoryToIntelType(force?.category);
}

function personHasLoyaltyConcept(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): boolean {
  if (character.forceId === 0) return false;
  const role = personRoleLabel(worldState, character);
  if (role.includes("当主") || role.includes("领主")) return false;
  const actor = findCharacterCityActor(worldState, character);
  if (!actor) return true;
  if (actor.kind === "Merchant" || actor.kind === "Religion") {
    const ids = actor.characterIds ?? [];
    const index = ids.indexOf(character.id);
    if (index === 0) return false;
  }
  return true;
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

function formatPersonStatValue(
  value: unknown,
  worldState: StrategyWorldState,
  intelDebugMode = false,
): string {
  if (intelDebugMode) return statPercent(value);
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

/** 占位：灾害/瘟疫/丰收等增减益（据点级）。 */
export function entityEffectsIntelRows(
  effects?: readonly StrategyEntityEffectState[] | null,
): IntelFieldRow[] {
  if (!effects?.length) {
    return [{ label: "状态", value: "暂无增减益（灾害/瘟疫/丰收等后续实装）" }];
  }
  return effects.map((effect) => ({
    label: effect.name,
    value: `${effect.effectTarget} ${effect.magnitude}${
      effect.description?.trim() ? ` · ${effect.description.trim()}` : ""
    }`,
  }));
}

function mapDtoEntityEffects(
  effects?: readonly StrategyEntityEffectState[] | null,
): IntelStanceEffectRow[] {
  if (!effects?.length) return [];
  return effects.map((effect, index) => ({
    id: effect.id > 0 ? effect.id : index + 1,
    name: effect.name,
    effectTarget: effect.effectTarget,
    magnitude: effect.magnitude,
    description: effect.description?.trim() || "—",
  }));
}

/** 角色间看法：影响列仅展示私人关系维度，不含外交关系。 */
function mapCharacterViewEntityEffects(
  effects?: readonly StrategyEntityEffectState[] | null,
): IntelStanceEffectRow[] {
  return mapDtoEntityEffects(effects).map((row) => ({
    ...row,
    effectTarget: normalizeCharacterViewEffectTarget(row.effectTarget),
  }));
}

function normalizeCharacterViewEffectTarget(raw: string): string {
  const trimmed = raw.trim();
  if (trimmed === "外交关系") return "亲疏";
  return trimmed;
}

/** 本家/对方看法 · 影响条目行（势力外交 / 角色私人关系共用列结构）。 */
export interface IntelStanceEffectRow {
  id: number;
  name: string;
  effectTarget: string;
  magnitude: string;
  description: string;
}

const FORCE_OUR_VIEW_EFFECTS: Record<number, IntelStanceEffectRow[]> = {
  2: [
    {
      id: 1,
      name: "桶狭间之战",
      effectTarget: "外交关系",
      magnitude: "长期 = -35",
      description: "今川义元于桶狭间败死，双方怨怼深重。",
    },
    {
      id: 2,
      name: "骏河侵攻",
      effectTarget: "外交关系",
      magnitude: "临时 = -15",
      description: "织田军曾多次威胁骏河边境。",
    },
  ],
  5: [
    {
      id: 1,
      name: "相模对峙",
      effectTarget: "外交关系",
      magnitude: "长期 = -20",
      description: "北条与织田在关东利益上长期摩擦。",
    },
  ],
  6: [
    {
      id: 1,
      name: "三河经略",
      effectTarget: "外交关系",
      magnitude: "长期 = -10",
      description: "德川脱离今川后，酒井家夹在两大势力之间。",
    },
  ],
};

const FORCE_THEIR_VIEW_EFFECTS: Record<number, IntelStanceEffectRow[]> = {
  2: [
    {
      id: 1,
      name: "杀害本家当主",
      effectTarget: "外交关系",
      magnitude: "永久 = -100",
      description: "杀害本家当主是世仇，不共戴天。",
    },
    {
      id: 2,
      name: "尾张扩张",
      effectTarget: "外交关系",
      magnitude: "长期 = -25",
      description: "织田据尾张步步紧逼，威胁今川霸权。",
    },
  ],
  5: [
    {
      id: 1,
      name: "关东介入",
      effectTarget: "外交关系",
      magnitude: "长期 = -18",
      description: "织田势力东进，北条视为心腹大患。",
    },
  ],
};

const PERSON_OUR_VIEW_EFFECTS: Record<number, IntelStanceEffectRow[]> = {
  3: [
    {
      id: 1,
      name: "骏河继承之争",
      effectTarget: "个人观感",
      magnitude: "长期 = -20",
      description: "今川氏真能力不足，难服骏河众臣。",
    },
  ],
  5: [
    {
      id: 1,
      name: "相模强藩",
      effectTarget: "个人观感",
      magnitude: "长期 = -15",
      description: "北条氏康老练多谋，是关东劲敌。",
    },
  ],
  9: [
    {
      id: 1,
      name: "三河自立",
      effectTarget: "个人观感",
      magnitude: "长期 = +10",
      description: "德川家康脱离今川后展现独立手腕，值得留意。",
    },
  ],
};

const PERSON_THEIR_VIEW_EFFECTS: Record<number, IntelStanceEffectRow[]> = {
  3: [
    {
      id: 1,
      name: "杀害本家当主",
      effectTarget: "亲疏",
      magnitude: "永久 = -100",
      description: "杀害本家当主是世仇，今川氏真绝难释怀。",
    },
    {
      id: 2,
      name: "清洲威胁",
      effectTarget: "个人观感",
      magnitude: "长期 = -22",
      description: "织田据清洲，对骏河形成直接压力。",
    },
  ],
  6: [
    {
      id: 1,
      name: "主君效忠",
      effectTarget: "个人观感",
      magnitude: "长期 = +25",
      description: "酒井忠次对德川家康忠心耿耿，视织田为潜在威胁。",
    },
  ],
  9: [
    {
      id: 1,
      name: "尾张霸权",
      effectTarget: "个人观感",
      magnitude: "长期 = -28",
      description: "德川家康视织田扩张为最大隐忧，私下保持距离。",
    },
  ],
};

/** 势力详情 · 本家对该势力的看法（外交 ViewEffects；内藩走 isInnerVassal 行）。 */
export function forceOurViewEffectRows(
  worldState: StrategyWorldState,
  forceId: number,
): IntelStanceEffectRow[] {
  const dip = lookupForceViewDiplomacy(worldState, forceId);
  if (!dip) return [];
  const mapped = mapDtoEntityEffects(dip.ourViewEffects);
  if (mapped.length > 0) return mapped;
  return FORCE_OUR_VIEW_EFFECTS[forceId] ?? [];
}

/** 势力详情 · 该势力对本家的看法（theirViewEffects）。 */
export function forceTheirViewEffectRows(
  worldState: StrategyWorldState,
  forceId: number,
): IntelStanceEffectRow[] {
  const dip = lookupForceViewDiplomacy(worldState, forceId);
  if (!dip) return [];
  const mapped = mapDtoEntityEffects(dip.theirViewEffects);
  if (mapped.length > 0) return mapped;
  return FORCE_THEIR_VIEW_EFFECTS[forceId] ?? [];
}

/** 势力详情是否展示本家/对方看法 Tab（仅本家根势力隐藏）。 */
export function showForceStanceEffectTabsForForce(
  worldState: StrategyWorldState,
  forceId: number | null,
): boolean {
  if (forceId == null) return false;
  return !isPlayerRootForce(worldState, forceId);
}

/** 人物详情 · 当主对该角色的看法（CharacterRelationship.viewEffects；仅亲疏/信赖/个人观感）。 */
export function personViewOfCharacterRows(
  worldState: StrategyWorldState,
  personId: number,
): IntelStanceEffectRow[] {
  if (!worldState.characters?.some((item) => item.id === personId)) return [];
  const lordId = resolvePlayerLordCharacterId(worldState);
  if (lordId != null) {
    const lord = worldState.characters.find((item) => item.id === lordId);
    const rel = lord?.characterRelationships?.find((item) => item.targetCharacterId === personId);
    const mapped = mapCharacterViewEntityEffects(rel?.viewEffects);
    if (mapped.length > 0) return mapped;
  }
  return PERSON_OUR_VIEW_EFFECTS[personId] ?? [];
}

/** 人物详情 · 该角色对当主的看法（反向 viewEffects）。 */
export function personCharacterViewOfLordRows(
  worldState: StrategyWorldState,
  personId: number,
): IntelStanceEffectRow[] {
  if (!worldState.characters?.some((item) => item.id === personId)) return [];
  const lordId = resolvePlayerLordCharacterId(worldState);
  if (lordId != null) {
    const person = worldState.characters.find((item) => item.id === personId);
    const rel = person?.characterRelationships?.find((item) => item.targetCharacterId === lordId);
    const mapped = mapCharacterViewEntityEffects(rel?.viewEffects);
    if (mapped.length > 0) return mapped;
  }
  return PERSON_THEIR_VIEW_EFFECTS[personId] ?? [];
}

/** @deprecated 使用 personViewOfCharacterRows */
export const personOurViewEffectRows = personViewOfCharacterRows;

/** @deprecated 使用 personCharacterViewOfLordRows */
export const personTheirViewEffectRows = personCharacterViewOfLordRows;

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

function strongholdMaintenanceMoney(
  stronghold: Pick<StrategyStrongholdState, "maintenance" | "population">,
): string {
  if (stronghold.maintenance != null && stronghold.maintenance > 0) {
    return formatMoney(stronghold.maintenance);
  }
  const n = Number(stronghold.population);
  const value = Number.isFinite(n) ? Math.max(800, Math.trunc(n) / 5) : 800;
  return formatMoney(value);
}

function cropPatternLabel(pattern: string | undefined): string {
  switch (pattern) {
    case "Double":
      return "二季作";
    case "Triple":
      return "三季作";
    default:
      return "单季作";
  }
}

function formatLaborThousands(value: number): string {
  const n = Math.max(0, Math.trunc(value));
  if (n === 0) return "0";
  const thousands = n / 1000;
  return Number.isInteger(thousands) ? `${thousands}千` : `${thousands.toFixed(1)}千`;
}

function strongholdLaborLabel(sh: StrategyStrongholdState): string {
  if (sh.laborCapacity == null || sh.laborCapacity <= 0) return "—";
  const available = sh.laborAvailable ?? sh.laborCapacity;
  const ratio = sh.laborRatioPercent ?? 100;
  return `${formatLaborThousands(available)}(${ratio}%)`;
}

function strongholdCropProgressLabel(sh: StrategyStrongholdState): string {
  const early = sh.earlyCropProgressPercent ?? 0;
  if (sh.effectiveCropPattern === "Triple") {
    return `早${early}%·晚${sh.lateCropProgressPercent ?? 0}%·三${sh.thirdCropProgressPercent ?? 0}%`;
  }
  if (sh.effectiveCropPattern === "Double") {
    return `早${early}%·晚${sh.lateCropProgressPercent ?? 0}%`;
  }
  return `${early}%`;
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

function resolveForceHomeStrongholdName(
  worldState: StrategyWorldState,
  forceId: number
): string | null {
  if (forceId <= 0) return null;

  const force = worldState.forces.find((item) => item.id === forceId);
  const residenceId = force?.lordResidenceStrongholdId;
  if (residenceId != null && residenceId > 0) {
    const residence = worldState.strongholds.find((item) => item.id === residenceId);
    if (residence?.name?.trim()) return residence.name.trim();
  }

  const lordResidence = worldState.strongholds.find(
    (item) => item.forceId === forceId && item.isLordResidence
  );
  if (lordResidence?.name?.trim()) return lordResidence.name.trim();

  const firstOwned = worldState.strongholds.find((item) => item.forceId === forceId);
  return firstOwned?.name?.trim() || null;
}

function personStrongholdName(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  const resolvedName = character.strongholdName?.trim();
  if (resolvedName) return resolvedName;

  const fromActor = findCharacterCityActorStrongholdName(worldState, character);
  if (fromActor !== "—") return fromActor;

  const strongholdId = character.strongholdId ?? 0;
  if (strongholdId > 0) {
    const sh = worldState.strongholds.find((s) => s.id === strongholdId);
    if (sh?.name?.trim()) return sh.name.trim();
  }

  const forceHome = resolveForceHomeStrongholdName(worldState, character.forceId);
  if (forceHome) return forceHome;

  return "—";
}

function lookupPlayerDiplomacy(
  worldState: StrategyWorldState,
  targetRootId: number
) {
  return worldState.diplomacies.find((d) => d.targetForceId === targetRootId);
}

function resolvePlayerRootForceId(worldState: StrategyWorldState): number {
  return resolveRealmRootId(worldState.playerForceId, worldState.forces);
}

function isPlayerRootForce(worldState: StrategyWorldState, forceId: number): boolean {
  return forceId === resolvePlayerRootForceId(worldState);
}

/** 势力详情 · 本家/内藩看法所依据的外交条目。 */
function lookupForceViewDiplomacy(
  worldState: StrategyWorldState,
  forceId: number,
) {
  if (isPlayerRootForce(worldState, forceId)) return undefined;

  const playerRoot = resolvePlayerRootForceId(worldState);
  const force = worldState.forces.find((item) => item.id === forceId);
  if (
    force?.status === "InnerVassal" &&
    force.suzerainForceId === playerRoot
  ) {
    return worldState.diplomacies.find(
      (item) => item.targetForceId === forceId && item.isInnerVassal,
    );
  }

  const targetRoot = resolveRealmRootId(forceId, worldState.forces);
  return lookupPlayerDiplomacy(worldState, targetRoot);
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

  const force = worldState.forces.find((f) => f.id === forceId);
  if (isOrganizationForceCategory(force?.category)) {
    const orgCharacters =
      worldState.characters?.filter((c) => !c.isDead && c.forceId === forceId) ?? [];
    if (force?.lordResidenceStrongholdId) {
      const leaderAtResidence = orgCharacters.find(
        (c) => c.strongholdId === force.lordResidenceStrongholdId
      );
      if (leaderAtResidence?.name?.trim()) return leaderAtResidence.name.trim();
    }
    const namedLeader = orgCharacters.find((c) => c.name?.trim());
    if (namedLeader?.name?.trim()) return namedLeader.name.trim();

    for (const stronghold of worldState.strongholds) {
      for (const actor of stronghold.cityActors ?? []) {
        if (actor.forceId !== forceId || !isOrganizationActorKind(actor.kind)) continue;
        const leader = resolveCityActorLeaderName(worldState, actor, stronghold);
        if (leader !== "—") return leader;
      }
    }
    return "—";
  }

  const residence = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isLordResidence
  );
  if (residence?.lordName?.trim()) return residence.lordName.trim();

  if (force?.lordResidenceStrongholdId) {
    const sh = worldState.strongholds.find((s) => s.id === force.lordResidenceStrongholdId);
    if (sh?.lordName?.trim()) return sh.lordName.trim();
  }

  const directRule = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isDirectRule
  );
  return directRule?.lordName?.trim() || "—";
}

function resolveOrganizationHostForceName(
  worldState: StrategyWorldState,
  force: StrategyForceState
): string {
  const residenceId = resolveOrganizationHeadquarterStrongholdId(worldState, force);
  if (!residenceId) return "—";
  const residence = worldState.strongholds.find((item) => item.id === residenceId);
  if (!residence) return "—";
  return forceName(worldState, residence.forceId);
}

function resolveForceResidenceName(
  worldState: StrategyWorldState,
  force: StrategyForceState
): string {
  if (isOrganizationForceCategory(force.category)) {
    const residenceId = resolveOrganizationHeadquarterStrongholdId(worldState, force);
    if (!residenceId) return "—";
    const sh = worldState.strongholds.find((s) => s.id === residenceId);
    return sh?.name?.trim() || "—";
  }

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
  const force = worldState.forces.find((item) => item.id === forceId);
  if (force?.totalSoldiers && force.totalSoldiers > 0) return force.totalSoldiers;
  let total = 0;
  for (const sh of worldState.strongholds) {
    if (sh.forceId !== forceId) continue;
    total += strongholdTotalSoldiers(sh);
  }
  for (const unit of worldState.units) {
    if (unit.forceId !== forceId) continue;
    total += Number.isFinite(Number(unit.soldiers))
      ? Math.max(0, Math.trunc(Number(unit.soldiers)))
      : 0;
  }
  return total;
}

function resolveForceGarrisonSoldiers(worldState: StrategyWorldState, forceId: number): number {
  const force = worldState.forces.find((item) => item.id === forceId);
  if (force?.garrisonSoldiers != null && force.garrisonSoldiers >= 0) return force.garrisonSoldiers;
  return worldState.strongholds
    .filter((sh) => sh.forceId === forceId)
    .reduce((sum, sh) => sum + Math.max(0, sh.garrisonSoldiers ?? 0), 0);
}

function resolveForceMilitiaSoldiers(worldState: StrategyWorldState, forceId: number): number {
  const force = worldState.forces.find((item) => item.id === forceId);
  if (force?.militiaSoldiers != null && force.militiaSoldiers >= 0) return force.militiaSoldiers;
  return worldState.strongholds
    .filter((sh) => sh.forceId === forceId)
    .reduce((sum, sh) => sum + Math.max(0, sh.militiaSoldiers ?? 0), 0);
}

function strongholdTotalSoldiers(stronghold: StrategyStrongholdState): number {
  if (stronghold.totalSoldiers != null && stronghold.totalSoldiers > 0) {
    return Math.max(0, stronghold.totalSoldiers);
  }
  return Math.max(0, stronghold.garrisonSoldiers ?? 0) + Math.max(0, stronghold.militiaSoldiers ?? 0);
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
  const force = forces.find((item) => item.id === forceId);
  if (isOrganizationForceCategory(force?.category)) {
    if (kind === "stronghold") {
      return String(force?.strongholdCount ?? 0);
    }
    return String(force?.characterCount ?? countForceOwnCharacters(worldState, forceId));
  }

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
    forceCategory: resolveDiplomacyForceCategory(worldState, force.id),
    relation: diplomacyAffinityTierLabel(dip?.relationship, relation),
    trust: diplomacyTrustTierLabel(dip?.trust, relation),
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
  const dip = worldState.diplomacies.find(
    (item) => item.targetForceId === force.id && item.isInnerVassal,
  );
  if (dip) {
    return {
      forceId: force.id,
      forceName: forceName(worldState, force.id),
      lordName: force.lordName?.trim() || resolveForceLordName(worldState, force.id),
      forceCategory: "武家",
      relation: diplomacyAffinityTierLabel(dip.relationship, dip.relation),
      trust: diplomacyTrustTierLabel(dip.trust, dip.relation),
      diplomacyStatus: diplomacyStatusLabel(dip.relation),
      politicalStatus: "内藩",
      arrearsMoney: formatMoney(dip.arrearsMoney ?? force.internalArrearsMoney ?? 0),
      arrearsFood: formatFoodGo(dip.arrearsFoodGo ?? force.internalArrearsFoodGo ?? 0),
      diplomacyTone: diplomacyToneFromRelation(dip.relation),
    };
  }

  return {
    forceId: force.id,
    forceName: forceName(worldState, force.id),
    lordName: resolveForceLordName(worldState, force.id),
    forceCategory: "武家",
    relation: "友好",
    trust: "信任",
    diplomacyStatus: "同盟",
    politicalStatus: "内藩",
    arrearsMoney: formatMoney(force.internalArrearsMoney ?? 0),
    arrearsFood: formatFoodGo(force.internalArrearsFoodGo ?? 0),
    diplomacyTone: "allied",
  };
}

function strongholdMilitiaCount(stronghold: StrategyStrongholdState): number {
  const fromUnits = (stronghold.standingGarrisonUnits ?? [])
    .filter((unit) => unit.role === "Militia")
    .reduce((sum, unit) => sum + Math.max(0, unit.soldiers), 0);
  const pool = Math.max(0, stronghold.militiaSoldiers ?? 0);
  if (fromUnits > 0 || pool > 0) return fromUnits + pool;
  return Math.max(0, stronghold.garrisonSoldiers ?? 0);
}

function strongholdStandingSoldiers(stronghold: StrategyStrongholdState): number {
  return (stronghold.standingGarrisonUnits ?? [])
    .filter((unit) => unit.role === "Samurai")
    .reduce((sum, unit) => sum + Math.max(0, unit.soldiers), 0);
}

function strongholdTotalGarrisonSoldiers(stronghold: StrategyStrongholdState): number {
  const fromUnits = (stronghold.standingGarrisonUnits ?? []).reduce(
    (sum, unit) => sum + Math.max(0, unit.soldiers),
    0,
  );
  if (fromUnits > 0) return fromUnits;
  return strongholdMilitiaCount(stronghold) + strongholdStandingSoldiers(stronghold);
}

function strongholdCityGarrison(stronghold: StrategyStrongholdState): number {
  return strongholdTotalSoldiers(stronghold);
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


/** 势力 Tab · 势力列表（武家 + 商家/寺社组织势力）。 */
export function forceIntelListRows(
  worldState: StrategyWorldState,
  options?: { realmFilter?: IntelRealmFilterMode }
): IntelForceRow[] {
  const { playerForceId, forces } = worldState;
  const realmFilter = options?.realmFilter ?? "all";
  const sorted = sortForcesPlayerFirst(worldState, [...forces]);

  return sorted
    .filter((force) => {
      if (isOrganizationForceCategory(force.category)) {
        return matchesOrganizationForceIntelFilter(worldState, force.id, realmFilter);
      }
      return matchesIntelRealmFilter(force.id, playerForceId, forces, realmFilter);
    })
    .map((force) => {
      const isOrg = isOrganizationForceCategory(force.category);
      const forceType = mapForceCategoryToIntelType(force.category);
      const suzerainName = isOrg
        ? resolveOrganizationHostForceName(worldState, force)
        : !isOrg && force.suzerainForceId != null && force.suzerainForceId > 0
          ? forceName(worldState, force.suzerainForceId)
          : "—";
      const isOwnRealm =
        isPlayerRealmForce(force.id, playerForceId, forces) ||
        (isOrg && matchesOrganizationForceIntelFilter(worldState, force.id, "realm"));

      return {
        id: force.id,
        name: force.name,
        lordName: force.lordName?.trim() || resolveForceLordName(worldState, force.id),
        forceType,
        residenceName: resolveForceResidenceName(worldState, force),
        cultureName: isOrg
          ? resolveOrganizationForceCultureName(worldState, force)
          : force.cultureName?.trim() || resolveForceCultureName(worldState, force.id),
        religionName: isOrg
          ? resolveOrganizationForceReligionName(worldState, force)
          : force.religionName?.trim() || resolveForceReligionName(worldState, force.id),
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
        soldiers: isOrg
          ? "—"
          : formatSoldiers(
              force.totalSoldiers && force.totalSoldiers > 0
                ? force.totalSoldiers
                : countForceOwnSoldiers(worldState, force.id),
            ),
        money: formatMoney(force.money),
        food: formatFoodGo(force.food),
        prestige: isOrg
          ? "—"
          : formatForeignForceStatValue(force.prestige, worldState, isOwnRealm),
        orthodoxy: isOrg
          ? "—"
          : formatForeignForceStatValue(force.orthodoxy, worldState, isOwnRealm),
        successorName: isOrg ? "—" : resolveSuccessorName(worldState, force.id),
        arrearsMoney: isOrg ? "—" : formatMoney(force.internalArrearsMoney ?? 0),
        arrearsFood: isOrg ? "—" : formatFoodGo(force.internalArrearsFoodGo ?? 0),
        isPlayer: force.id === playerForceId,
        isOwnRealm,
      };
    });
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
        scale: strongholdHoverFieldValue(
          worldState,
          sh,
          "规模",
          resolveStrongholdScaleLabel(sh.scale, sh.population)
        ),
        lordName: obscurePersonnel ? UNKNOWN_INTEL : (sh.lordName?.trim() || "—"),
        mayorName: obscurePersonnel ? UNKNOWN_INTEL : (sh.mayorName?.trim() || "—"),
        population: strongholdHoverFieldValue(worldState, sh, "人口", safePopulation(sh.population)),
        stability: strongholdHoverFieldValue(worldState, sh, "治安", statPercent(sh.stability)),
        popularFeelings: strongholdHoverFieldValue(worldState, sh, "民心", statPercent(sh.popularFeelings)),
        maintenance: strongholdHoverFieldValue(
          worldState,
          sh,
          "维护费",
          strongholdMaintenanceMoney(sh)
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
        cropPattern: strongholdHoverFieldValue(
          worldState,
          sh,
          "作型",
          cropPatternLabel(sh.effectiveCropPattern)
        ),
        labor: strongholdHoverFieldValue(worldState, sh, "劳力", strongholdLaborLabel(sh)),
        cropProgress: strongholdHoverFieldValue(
          worldState,
          sh,
          "农事进度",
          strongholdCropProgressLabel(sh)
        ),
        militiaPool: strongholdHoverFieldValue(
          worldState,
          sh,
          "农兵",
          formatSoldiers(strongholdMilitiaCount(sh))
        ),
        standingProfessional: strongholdHoverFieldValue(
          worldState,
          sh,
          "武士",
          formatSoldiers(
            (sh.standingGarrisonUnits ?? [])
              .filter((u) => u.role === "Samurai")
              .reduce((sum, u) => sum + u.soldiers, 0)
          )
        ),
        standingWounded: strongholdHoverFieldValue(
          worldState,
          sh,
          "伤兵",
          formatSoldiers(sh.garrisonWounded ?? 0)
        ),
      };
    });

  return sortIntelRowsPlayerEntityFirst(rows, resolvePlayerLordStrongholdId(worldState));
}

function resolveCityActorBranchLabel(
  worldState: StrategyWorldState,
  actor: StrategyStrongholdCityActorState,
  strongholdId: number
): string {
  if (actor.branchLabel && actor.branchLabel !== "—") return actor.branchLabel;
  if (!actor.forceId || actor.forceId <= 0) return "—";

  const force = worldState.forces.find((item) => item.id === actor.forceId);
  const residenceId = force?.lordResidenceStrongholdId;
  if (!residenceId) return "—";

  if (actor.kind === "Religion") {
    return strongholdId === residenceId ? "本院" : "分院";
  }
  if (actor.kind === "Merchant") {
    return strongholdId === residenceId ? "本店" : "分店";
  }
  return "—";
}

function resolveCityActorRoleHolders(
  worldState: StrategyWorldState,
  actor: StrategyStrongholdCityActorState,
  stronghold?: StrategyStrongholdState
): { primary: string; secondary: string; tertiary: string } {
  if (actor.kind === "Government") {
    return {
      primary: resolveForceLordName(worldState, stronghold?.forceId ?? 0),
      secondary: stronghold?.lordName?.trim() || "—",
      tertiary: stronghold?.mayorName?.trim() || "—",
    };
  }

  const ids = actor.characterIds ?? [];
  const names = ids
    .map((id) => worldState.characters?.find((item) => item.id === id)?.name?.trim())
    .filter((name): name is string => Boolean(name));

  if (names.length === 0) {
    const leader = resolveCityActorLeaderName(worldState, actor, stronghold);
    return {
      primary: leader,
      secondary: "—",
      tertiary: "—",
    };
  }

  return {
    primary: names[0] ?? "—",
    secondary: names[1] ?? "—",
    tertiary: names[2] ?? "—",
  };
}

function resolveCityActorLeaderName(
  worldState: StrategyWorldState,
  actor: StrategyStrongholdCityActorState,
  stronghold?: StrategyStrongholdState
): string {
  if (actor.leaderName && actor.leaderName !== "—") return actor.leaderName;
  if (actor.kind === "Government") {
    return stronghold?.lordName?.trim() || "—";
  }
  const ids = actor.characterIds ?? [];
  for (const id of ids) {
    const character = worldState.characters?.find((item) => item.id === id);
    if (character?.name?.trim()) return character.name.trim();
  }
  return "—";
}

function resolveTempleReligionName(templeName: string): string {
  if (
    templeName.includes("神宫") ||
    templeName.includes("神社") ||
    templeName.includes("八幡")
  ) {
    return "神道教";
  }
  if (templeName.includes("寺")) return "佛教";
  return "神道教";
}

function resolveCityActorReligionName(
  actor: StrategyStrongholdCityActorState,
  stronghold?: StrategyStrongholdState
): string {
  if (actor.kind === "Religion") {
    return resolveTempleReligionName(actor.name);
  }
  if (actor.kind === "Merchant" && actor.name.includes("南蛮")) {
    return "基督教";
  }
  return stronghold?.religionName?.trim() || "—";
}

function resolvePersonCategory(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): IntelPersonCategory {
  const actor = findCharacterCityActor(worldState, character);
  if (actor?.kind === "Merchant") return "商人";
  if (actor?.kind === "Religion") return "寺社";
  if (character.forceId === 0) return "浪人";
  return "武家";
}

function resolvePersonForceName(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  const force = worldState.forces.find((item) => item.id === character.forceId);
  if (force?.category === "Merchant" || force?.category === "Religion") {
    const actor = findCharacterCityActor(worldState, character);
    if (actor?.name?.trim()) return actor.name.trim();
    return force.name?.trim() || "—";
  }

  const actor = findCharacterCityActor(worldState, character);
  if (actor && (actor.kind === "Merchant" || actor.kind === "Religion")) {
    return actor.name?.trim() || "—";
  }
  if (character.forceId === 0) return "—";
  return forceName(worldState, character.forceId);
}

function resolveOrganizationCharacterIds(
  worldState: StrategyWorldState,
  organizationForceId: number
): Set<number> {
  const ids = new Set<number>();
  for (const stronghold of worldState.strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      if (actor.forceId !== organizationForceId || !isOrganizationActorKind(actor.kind)) continue;
      for (const id of actor.characterIds ?? []) {
        if (id > 0) ids.add(id);
      }
    }
  }
  return ids;
}

function isPersonIntelListedCharacter(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): boolean {
  const force = worldState.forces.find((item) => item.id === character.forceId);
  if (!isOrganizationForceCategory(force?.category)) return true;

  const rosterIds = resolveOrganizationCharacterIds(worldState, character.forceId);
  if (rosterIds.size === 0) return true;
  return rosterIds.has(character.id);
}

function resolveOrganizationPersonType(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
  category: "Merchant" | "Religion"
): string {
  const actor = findCharacterCityActor(worldState, character);
  if (actor && (actor.kind === "Merchant" || actor.kind === "Religion")) {
    const ids = actor.characterIds ?? [];
    const index = ids.indexOf(character.id);
    if (index >= 0) {
      if (actor.kind === "Merchant" && actor.branchLabel === "分店" && ids.length === 1) {
        return "掌柜";
      }
      return organizationRoleLabelAtIndex(actor.kind, index);
    }
  }

  if (category === "Merchant") return "掌柜";
  return "执事";
}

function resolvePersonType(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState
): string {
  if (character.forceId === 0) return "在野";

  const force = worldState.forces.find((item) => item.id === character.forceId);
  if (force?.category === "Merchant") {
    return resolveOrganizationPersonType(worldState, character, "Merchant");
  }
  if (force?.category === "Religion") {
    return resolveOrganizationPersonType(worldState, character, "Religion");
  }

  const actor = findCharacterCityActor(worldState, character);
  if (actor) {
    const ids = actor.characterIds ?? [];
    const index = ids.indexOf(character.id);
    if (index >= 0 && (actor.kind === "Merchant" || actor.kind === "Religion")) {
      return organizationRoleLabelAtIndex(actor.kind, index);
    }
    return cityActorKindLabel(actor.kind);
  }

  const role = personRoleLabel(worldState, character);
  if (role.includes("当主") || role.includes("领主")) return "领主";
  if (role.includes("代官")) return "代官";
  return "家臣";
}

function mapPersonIntelRow(
  worldState: StrategyWorldState,
  character: StrategyCharacterSummaryState,
  intelDebugMode = false,
): IntelPersonRow {
  const p = character.personality;
  const s = character.proficiency;
  const hobbies = hobbyCategoryValues(p?.hobby, character.id);
  return {
    id: character.id,
    name: character.name?.trim() || `人物#${character.id}`,
    personType: resolvePersonType(worldState, character),
    personCategory: resolvePersonCategory(worldState, character),
    forceName: resolvePersonForceName(worldState, character),
    strongholdName: personStrongholdName(worldState, character),
    isFamily: oxMark(personIsFamilyMember(worldState, character)),
    role: personRoleLabel(worldState, character),
    superior: personSuperiorLabel(worldState, character),
    location: personLocationLabel(worldState, character),
    locationType: personLocationTypeLabel(character.locationType),
    leadership: formatPersonStatValue(character.leadership, worldState, intelDebugMode),
    power: formatPersonStatValue(character.power, worldState, intelDebugMode),
    politics: formatPersonStatValue(character.politics, worldState, intelDebugMode),
    strategy: formatPersonStatValue(character.strategy, worldState, intelDebugMode),
    charm: formatPersonStatValue(character.charm, worldState, intelDebugMode),
    loyalty: personHasLoyaltyConcept(worldState, character)
      ? statPercent(character.loyalty ?? p?.friendship)
      : "—",
    status: personStatusLabel(character.forceStatus),
    healthStatus: healthStatusLabel(character.isSick),
    commandTarget: personCommandTarget(worldState, character),
    taskRemainingDays: formatTaskRemainingDays(character),
    yearsInForce: formatYearsInForce(character.yearsInForce),
    cultureName: character.cultureName?.trim() || "—",
    religionName: character.religionName?.trim() || "—",
    sex: sexLabel(character.sex),
    age: statPercent(character.age),
    birthType: birthTypeLabel(character.birthType),
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
    skillInfantry: formatPersonStatValue(s?.infantry, worldState, intelDebugMode),
    skillRide: formatPersonStatValue(s?.ride, worldState, intelDebugMode),
    skillArchery: formatPersonStatValue(s?.archery, worldState, intelDebugMode),
    skillFirelock: formatPersonStatValue(s?.firelock, worldState, intelDebugMode),
    skillSealing: formatPersonStatValue(s?.sealing, worldState, intelDebugMode),
    skillMilitary: formatPersonStatValue(s?.military, worldState, intelDebugMode),
    skillFighting: formatPersonStatValue(s?.fighting, worldState, intelDebugMode),
    skillSpy: formatPersonStatValue(s?.spy, worldState, intelDebugMode),
    skillAgriculture: formatPersonStatValue(s?.agriculture, worldState, intelDebugMode),
    skillCommerce: formatPersonStatValue(s?.commerce, worldState, intelDebugMode),
    skillConstruct: formatPersonStatValue(s?.construct, worldState, intelDebugMode),
    skillSmelt: formatPersonStatValue(s?.smelt, worldState, intelDebugMode),
    skillEloquence: formatPersonStatValue(s?.eloquence, worldState, intelDebugMode),
    skillCourt: formatPersonStatValue(s?.court, worldState, intelDebugMode),
    skillSociality: formatPersonStatValue(s?.sociality, worldState, intelDebugMode),
    skillHealing: formatPersonStatValue(s?.healing, worldState, intelDebugMode),
  };
}

/** 人物 Tab：人物列表（全字段）。 */
export function personIntelRows(
  worldState: StrategyWorldState,
  options?: IntelDataOptions
): IntelPersonRow[] {
  const { playerForceId, forces, characters } = worldState;
  const realmFilter = options?.realmFilter ?? "all";
  const intelDebugMode = options?.intelDebugMode ?? false;
  if (!characters?.length) return [];

  const rows = characters
    .filter(
      (c) =>
        !c.isDead &&
        isPersonIntelListedCharacter(worldState, c) &&
        isPersonIntelVisible(worldState, c, intelDebugMode) &&
        matchesIntelRealmFilter(c.forceId, playerForceId, forces, realmFilter)
    )
    .map((c) => mapPersonIntelRow(worldState, c, intelDebugMode));

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
  personId: number | null,
  options?: Pick<IntelDataOptions, "intelDebugMode">,
): IntelPersonRow | null {
  if (personId == null) return null;
  return personIntelRows(worldState, options).find((row) => row.id === personId) ?? null;
}

/** 势力详情 · 基本（完整字段）。 */
export function forceDetailIntelRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelFieldRow[] {
  const row = findForceRow(worldState, forceId);
  if (!row) return [];

  if (row.forceType !== "武家") {
    const orgKind = row.forceType === "商人" ? "Merchant" : "Religion";
    const primaryLabel = organizationPrimaryRoleLabel(orgKind);
    return [
      { label: "名称", value: row.name },
      { label: "类型", value: row.forceType },
      { label: primaryLabel, value: row.lordName },
      { label: "居城", value: row.residenceName },
      { label: "文化", value: row.cultureName },
      { label: "信仰", value: row.religionName },
      { label: "状态", value: row.status },
      { label: "领内", value: row.suzerainName },
      { label: "店数", value: row.strongholdCount },
      { label: "人数", value: row.characterCount },
      { label: "金钱", value: row.money },
      { label: "粮食", value: row.food },
    ];
  }

  return [
    { label: "名称", value: row.name },
    { label: "类型", value: row.forceType },
    { label: "当主", value: row.lordName },
    { label: "居城", value: row.residenceName },
    { label: "文化", value: row.cultureName },
    { label: "信仰", value: row.religionName },
    { label: "状态", value: row.status },
    { label: "宗主", value: row.suzerainName },
    { label: "继承人", value: row.successorName },
    { label: "总兵力", value: row.soldiers },
    { label: "驻军", value: formatSoldiers(resolveForceGarrisonSoldiers(worldState, forceId)) },
    { label: "农兵", value: formatSoldiers(resolveForceMilitiaSoldiers(worldState, forceId)) },
    { label: "据点数", value: row.strongholdCount },
    { label: "现任", value: row.characterCount },
    { label: "金钱", value: row.money },
    { label: "粮食", value: row.food },
    { label: "威望", value: row.prestige },
    { label: "正统", value: row.orthodoxy },
  ];
}

function resolveForceLordResidenceStrongholdId(
  worldState: StrategyWorldState,
  forceId: number
): number | null {
  const force = worldState.forces.find((item) => item.id === forceId);
  if (force?.lordResidenceStrongholdId) {
    return force.lordResidenceStrongholdId;
  }

  const residence = worldState.strongholds.find(
    (s) => s.forceId === forceId && s.isLordResidence
  );
  if (residence) return residence.id;

  return worldState.strongholds.find((s) => s.forceId === forceId)?.id ?? null;
}

/** 势力详情 · 据点表。 */
export function forceStrongholdTableRows(
  worldState: StrategyWorldState,
  forceId: number,
  options?: ForceDetailScopeOptions
): IntelStrongholdRow[] {
  const row = findForceRow(worldState, forceId);
  if (!row) return [];

  const force = worldState.forces.find((item) => item.id === forceId);
  if (isOrganizationForceCategory(force?.category)) {
    return strongholdIntelRows(worldState).filter((sh) => {
      const stronghold = worldState.strongholds.find((item) => item.id === sh.id);
      if (!stronghold) return false;
      return (stronghold.cityActors ?? []).some(
        (actor) => actor.forceId === forceId && isOrganizationActorKind(actor.kind)
      );
    });
  }

  const scopeIds = collectForceScopeIds(
    worldState,
    forceId,
    options?.includeInnerVassals === true
  );
  return strongholdIntelRows(worldState).filter((sh) => {
    const stronghold = worldState.strongholds.find((item) => item.id === sh.id);
    if (!stronghold) return false;
    return scopeIds.has(stronghold.forceId);
  });
}

/** 势力详情 · 现任人物表。 */
export function forcePersonTableRows(
  worldState: StrategyWorldState,
  forceId: number,
  options?: ForceDetailScopeOptions & Pick<IntelDataOptions, "intelDebugMode">,
): IntelPersonRow[] {
  const row = findForceRow(worldState, forceId);
  if (!row) return [];

  const intelOptions: IntelDataOptions = {
    intelDebugMode: options?.intelDebugMode,
  };

  const force = worldState.forces.find((item) => item.id === forceId);
  if (isOrganizationForceCategory(force?.category)) {
    return personIntelRows(worldState, intelOptions).filter((personRow) => {
      const character = worldState.characters?.find((item) => item.id === personRow.id);
      return character?.forceId === forceId;
    });
  }

  const scopeIds = collectForceScopeIds(
    worldState,
    forceId,
    options?.includeInnerVassals === true
  );
  return personIntelRows(worldState, intelOptions).filter((personRow) => {
    if (personRow.personCategory !== "武家") return false;
    const character = worldState.characters?.find((item) => item.id === personRow.id);
    if (!character) return false;
    return scopeIds.has(character.forceId);
  });
}

/** 据点详情 · 技术表（实体技术 + Master Data 合并）。 */
export function strongholdTechnologyTableRows(
  worldState: StrategyWorldState,
  strongholdId: number,
  options?: { forceName?: string }
): IntelStrongholdTechnologyRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  const forceNameLabel = options?.forceName ?? forceName(worldState, stronghold.forceId);
  const entityRows = mapEntityTechnologyRows(stronghold.technologies, forceNameLabel);
  if (entityRows.length > 0) return entityRows;

  const localDouble = stronghold.knowsDoubleCrop === true;
  const localTriple = stronghold.knowsTripleCrop === true;
  const forceTech = resolveForceTechnologyFlags(worldState, stronghold.forceId);
  const effectiveDouble = localDouble && forceTech.knowsDouble;
  const effectiveTriple = localTriple && forceTech.knowsTriple;
  const effectivePattern = effectiveTriple
    ? "Triple"
    : effectiveDouble
      ? "Double"
      : "Single";

  return [
    {
      id: 1,
      name: "二季作",
      category: "农业",
      status: localDouble ? "已掌握" : "未掌握",
      forceName: forceNameLabel,
      condition: effectiveDouble
        ? "可用"
        : !localDouble && !forceTech.knowsDouble
          ? "势力与据点均未掌握"
          : !forceTech.knowsDouble
            ? "势力未掌握"
            : "据点未掌握",
    },
    {
      id: 2,
      name: "三季作",
      category: "农业",
      status: localTriple ? "已掌握" : "未掌握",
      forceName: forceNameLabel,
      condition: effectiveTriple
        ? "可用"
        : !localTriple && !forceTech.knowsTriple
          ? "势力与据点均未掌握"
          : !forceTech.knowsTriple
            ? "势力未掌握"
            : "据点未掌握",
    },
    {
      id: 3,
      name: "有效作型",
      category: "农业",
      status: cropPatternLabel(effectivePattern),
      forceName: forceNameLabel,
      condition: "势力与据点技术交集",
    },
  ];
}

function resolveForceTechnologyFlags(
  worldState: StrategyWorldState,
  forceId: number
): { knowsDouble: boolean; knowsTriple: boolean } {
  const force = worldState.forces.find((item) => item.id === forceId);
  const fromTechnologies = (force?.technologies ?? []).reduce(
    (acc, tech) => {
      if (tech.status !== 1) return acc;
      if (tech.id === 1) acc.knowsDouble = true;
      if (tech.id === 2) acc.knowsTriple = true;
      return acc;
    },
    { knowsDouble: false, knowsTriple: false },
  );
  if (fromTechnologies.knowsDouble || fromTechnologies.knowsTriple) return fromTechnologies;

  const residenceId = resolveForceLordResidenceStrongholdId(worldState, forceId);
  const residence = worldState.strongholds.find((s) => s.id === residenceId);
  return {
    knowsDouble: residence?.knowsDoubleCrop === true,
    knowsTriple: residence?.knowsTripleCrop === true,
  };
}

function mapEntityTechnologyRows(
  technologies: StrategyEntityTechnologyState[] | undefined,
  forceNameLabel: string,
): IntelStrongholdTechnologyRow[] {
  if (!technologies?.length) return [];
  return technologies.map((tech) => ({
    id: tech.id,
    name: tech.name,
    category: tech.category,
    status: tech.status === 1 ? "已完成" : "研究中",
    forceName: forceNameLabel,
    condition:
      tech.target && tech.effectivity != null
        ? `${tech.target} ${tech.effectivity >= 0 ? "+" : ""}${tech.effectivity}`
        : tech.target ?? "—",
  }));
}

/** 势力详情 · 技术表（势力居城掌握的技术）。 */
export function forceTechnologyTableRows(
  worldState: StrategyWorldState,
  forceId: number,
  options?: ForceDetailScopeOptions & { showForceColumn?: boolean }
): IntelStrongholdTechnologyRow[] {
  const row = findForceRow(worldState, forceId);
  if (!row) return [];

  const force = worldState.forces.find((item) => item.id === forceId);
  const directTechRows = mapEntityTechnologyRows(force?.technologies, force?.name ?? forceName(worldState, forceId));
  if (directTechRows.length > 0) return directTechRows;

  if (isOrganizationForceCategory(force?.category)) {
    const residenceId = force?.lordResidenceStrongholdId;
    if (!residenceId) return [];
    return strongholdTechnologyTableRows(worldState, residenceId, {
      forceName: force?.name,
    });
  }

  const scopeIds = collectForceScopeIds(
    worldState,
    forceId,
    options?.includeInnerVassals === true
  );
  const strongholdIds = worldState.strongholds
    .filter((stronghold) => scopeIds.has(stronghold.forceId))
    .map((stronghold) => stronghold.id);

  if (strongholdIds.length === 0) {
    const residenceId = resolveForceLordResidenceStrongholdId(worldState, forceId);
    if (residenceId == null) return [];
    return strongholdTechnologyTableRows(worldState, residenceId, {
      forceName: options?.showForceColumn ? forceName(worldState, forceId) : undefined,
    });
  }

  const rows: IntelStrongholdTechnologyRow[] = [];
  for (const strongholdId of strongholdIds) {
    const stronghold = worldState.strongholds.find((item) => item.id === strongholdId);
    if (!stronghold) continue;
    const baseId = strongholdId * 10;
    rows.push(
      ...strongholdTechnologyTableRows(worldState, strongholdId, {
        forceName: options?.showForceColumn
          ? forceName(worldState, stronghold.forceId)
          : undefined,
      }).map((item, index) => ({ ...item, id: baseId + index }))
    );
  }
  return rows;
}

/** 某势力视角的外交列表（封地根势力展示全表，他势力仅展示与己关系）。 */
export function diplomacyForForceRows(
  worldState: StrategyWorldState,
  forceId: number
): IntelDiplomacyRow[] {
  const force = worldState.forces.find((item) => item.id === forceId);
  if (isOrganizationForceCategory(force?.category)) {
    return [];
  }

  const playerRoot = resolveRealmRootId(worldState.playerForceId, worldState.forces);
  const selectedRoot = resolveRealmRootId(forceId, worldState.forces);

  if (
    forceId === selectedRoot &&
    isPlayerRealmForce(forceId, worldState.playerForceId, worldState.forces)
  ) {
    return diplomacyIntelRows(worldState);
  }

  if (force?.status === "InnerVassal") {
    return [];
  }

  if (forceId === playerRoot) {
    return diplomacyIntelRows(worldState);
  }

  const diplomacy = lookupPlayerDiplomacy(worldState, selectedRoot);
  const playerForce = worldState.forces.find((f) => f.id === playerRoot);
  const relation = diplomacy?.relation ?? null;

  return [
    {
      forceId: playerRoot,
      forceName: forceName(worldState, playerRoot),
      lordName: resolveForceLordName(worldState, playerRoot),
      forceCategory: resolveDiplomacyForceCategory(worldState, playerRoot),
      relation: diplomacyAffinityTierLabel(diplomacy?.relationship, relation),
      trust: diplomacyTrustTierLabel(diplomacy?.trust, relation),
      diplomacyStatus: diplomacyStatusLabel(relation),
      politicalStatus: forcePoliticalStatusLabel(playerForce?.status),
      arrearsMoney: formatMoney(diplomacy?.arrearsMoney ?? playerForce?.internalArrearsMoney ?? 0),
      arrearsFood: formatFoodGo(diplomacy?.arrearsFoodGo ?? playerForce?.internalArrearsFoodGo ?? 0),
      diplomacyTone: diplomacyToneFromRelation(relation),
    },
  ];
}

export function forceIntroText(worldState: StrategyWorldState, forceId: number): string {
  const force = worldState.forces.find((item) => item.id === forceId);
  if (force?.introduction?.trim()) return force.introduction.trim();

  const row = findForceRow(worldState, forceId);
  if (!row) return "暂无该势力介绍。";

  if (row.forceType !== "武家") {
    return [
      `${row.name}为${row.residenceName}内的${row.forceType}势力，由${row.lordName}主持。`,
      `隶属${row.suzerainName}领内，共${row.characterCount}人。`,
      `金钱${row.money}、粮食${row.food}。`,
      "（完整势力传记与情报迷雾系统后续实装。）",
    ].join("");
  }

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

export function strongholdEffectsDetailRows(
  worldState: StrategyWorldState,
  strongholdId: number,
): IntelFieldRow[] {
  const stronghold = worldState.strongholds.find((item) => item.id === strongholdId);
  return entityEffectsIntelRows(stronghold?.activeEffects);
}

export function forceEffectsIntelRows(
  worldState: StrategyWorldState,
  forceId: number,
): IntelFieldRow[] {
  const force = worldState.forces.find((item) => item.id === forceId);
  return entityEffectsIntelRows(force?.activeEffects);
}

export function personEffectsIntelRows(
  worldState: StrategyWorldState,
  personId: number,
): IntelFieldRow[] {
  const character = worldState.characters?.find((item) => item.id === personId);
  return entityEffectsIntelRows(character?.activeEffects);
}

function findStrongholdCityActor(
  stronghold: StrategyStrongholdState,
  actorId: number | null | undefined
): StrategyStrongholdCityActorState | null {
  if (actorId == null) return null;
  return (stronghold.cityActors ?? []).find((actor) => actor.id === actorId) ?? null;
}

function cityActorKindLabel(kind: string | undefined): string {
  switch (kind) {
    case "Government":
      return "官府";
    case "Civilian":
      return "民间";
    case "Kokujin":
      return "国人";
    case "Religion":
      return "寺社";
    case "Merchant":
    default:
      return "商家";
  }
}

function cropCycleProductionStatus(cycle: StrategyCropCycleState): string {
  if (cycle.progressPercent >= cycle.progressCapPercent) return "待收";
  if (cycle.progressPercent > 0) return "进行中";
  return "未开始";
}

function cropCycleEfficiency(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
  cycle: StrategyCropCycleState
): string {
  const laborRatio = stronghold.laborRatioPercent;
  const progressRatio =
    cycle.progressCapPercent > 0
      ? Math.round((cycle.progressPercent / cycle.progressCapPercent) * 100)
      : 0;
  const value =
    laborRatio != null
      ? `${statPercent(laborRatio)}%（进度 ${statPercent(progressRatio)}%）`
      : `${statPercent(progressRatio)}%`;
  return strongholdHoverFieldValue(worldState, stronghold, "生产效率", value);
}

function characterAtStronghold(
  character: StrategyCharacterSummaryState,
  strongholdId: number
): boolean {
  return (character.strongholdId ?? 0) === strongholdId;
}

function isWildCharacter(character: StrategyCharacterSummaryState): boolean {
  return !character.isDead && character.forceId === 0;
}

function resolveCityActorCharacterIds(actor: StrategyStrongholdCityActorState): number[] {
  if (actor.characterIds && actor.characterIds.length > 0) {
    return actor.characterIds;
  }
  return actor.characterCount > 0 ? Array.from({ length: actor.characterCount }, (_, index) => -(actor.id + index + 1)) : [];
}

function charactersForCityActor(
  worldState: StrategyWorldState,
  strongholdId: number,
  actor: StrategyStrongholdCityActorState
): StrategyCharacterSummaryState[] {
  const characters = worldState.characters ?? [];

  if (actor.kind === "Civilian") {
    return characters.filter(
      (character) => isWildCharacter(character) && characterAtStronghold(character, strongholdId)
    );
  }

  const ids = new Set(resolveCityActorCharacterIds(actor).filter((id) => id > 0));
  if (ids.size === 0) return [];

  return characters.filter((character) => !character.isDead && ids.has(character.id));
}

/** 城中势力 Tab · 人物（与人物 Tab 状态表一致）。 */
export function strongholdFactionPersonRows(
  worldState: StrategyWorldState,
  strongholdId: number,
  actorId: number | null
): IntelPersonRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  const actor = findStrongholdCityActor(stronghold, actorId);
  if (!actor) return [];

  if (actor.kind === "Government") {
    const characters = (worldState.characters ?? []).filter(
      (c) => !c.isDead && characterAtStronghold(c, strongholdId) && c.forceId !== 0
    );
    return characters
      .filter((c) => {
        const role = personRoleLabel(worldState, c);
        return role.includes("领主") || role.includes("代官") || role.includes("当主");
      })
      .map((c) => mapPersonIntelRow(worldState, c));
  }

  return charactersForCityActor(worldState, strongholdId, actor).map((c) =>
    mapPersonIntelRow(worldState, c)
  );
}

function cropPatternProductionStatus(stronghold: StrategyStrongholdState): string {
  const pattern = stronghold.effectiveCropPattern ?? "Single";
  if (pattern === "Triple") return "受三季作技术支撑";
  if (pattern === "Double") return "受二季作技术支撑";
  return "受单季作型限制";
}

function actorAgricultureProductionRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
  actor: StrategyStrongholdCityActorState,
  label: string,
  rowIdBase: number
): IntelStrongholdFactionProductionRow[] {
  if (actor.agricultureProduction <= 0) return [];

  const cycles = stronghold.cropCycles ?? [];
  if (cycles.length === 0) {
    return [
      {
        id: rowIdBase,
        content: label,
        startTime: "—",
        endTime: "—",
        efficiency: formatFoodGo(actor.agricultureProduction),
        status: strongholdHoverFieldValue(
          worldState,
          stronghold,
          "农事进度",
          cropPatternProductionStatus(stronghold)
        ),
      },
    ];
  }

  const share = actor.agricultureProduction / cycles.length;
  return cycles.map((cycle, index) => ({
    id: rowIdBase + index,
    content: `${label}·${cycle.name}`,
    startTime: `${cycle.startMonth}/${cycle.startDay}`,
    endTime: `${cycle.endMonth}/${cycle.endDay}`,
    efficiency: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "农业产出",
      formatFoodGo(Math.trunc((share * cycle.progressPercent) / 100))
    ),
    status: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "农事进度",
      cropCycleProductionStatus(cycle)
    ),
  }));
}

/** 城中势力 Tab · 生产（按所选势力行；收税与商业贸易不计入生产）。 */
export function strongholdFactionProductionRows(
  worldState: StrategyWorldState,
  strongholdId: number,
  actorId: number | null
): IntelStrongholdFactionProductionRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  const actor = findStrongholdCityActor(stronghold, actorId);
  if (!actor) return [];

  if (actor.kind === "Civilian") {
    const cycles = stronghold.cropCycles ?? [];
    if (cycles.length === 0) {
      return [
        {
          id: 1,
          content: "民间大田",
          startTime: "—",
          endTime: "—",
          efficiency: strongholdHoverFieldValue(
            worldState,
            stronghold,
            "农业潜力",
            formatFoodGo(stronghold.agricultureProductionPotential ?? 0)
          ),
          status: strongholdHoverFieldValue(
            worldState,
            stronghold,
            "农事进度",
            cropPatternProductionStatus(stronghold)
          ),
        },
      ];
    }

    return cycles.map((cycle) => ({
      id: cycle.cycleIndex,
      content: cycle.name,
      startTime: `${cycle.startMonth}/${cycle.startDay}`,
      endTime: `${cycle.endMonth}/${cycle.endDay}`,
      efficiency: cropCycleEfficiency(worldState, stronghold, cycle),
      status: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "农事进度",
        cropCycleProductionStatus(cycle)
      ),
    }));
  }

  if (actor.kind === "Government") {
    return [];
  }

  if (actor.kind === "Merchant") {
    return actorAgricultureProductionRows(worldState, stronghold, actor, "商田", 1);
  }

  if (actor.kind === "Religion") {
    return actorAgricultureProductionRows(worldState, stronghold, actor, "寺社田", 1);
  }

  if (actor.kind === "Kokujin") {
    return actorAgricultureProductionRows(worldState, stronghold, actor, "领内田", 1);
  }

  return [];
}

function formatCropPeriod(startMonth: number, startDay: number, endMonth: number, endDay: number): string {
  return `${startMonth}/${startDay}–${endMonth}/${endDay}`;
}

function standingGarrisonRoleLabel(role: string): string {
  switch (role) {
    case "Samurai":
      return "精锐";
    case "Militia":
    default:
      return "农兵";
  }
}

function formatStandingUnitName(unitName: string | undefined, typeName: string): string {
  const base = unitName?.trim() || typeName.trim() || "队";
  return base.endsWith("队") ? base : `${base}队`;
}

/** 据点详情 · 常备军 SubUnit 表。 */
export function strongholdStandingGarrisonTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelStandingGarrisonRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  const units = stronghold.standingGarrisonUnits ?? [];
  if (units.length === 0) {
    return [];
  }

  return units.map((unit) => ({
    unitName: formatStandingUnitName(unit.unitName, unit.typeName),
    typeName: unit.typeName,
    roleLabel: standingGarrisonRoleLabel(unit.role),
    isMounted: unit.isMounted ? "○" : "×",
    soldiers: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "常备兵",
      formatSoldiers(unit.soldiers)
    ),
    morale: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "士气",
      statPercent(unit.morale)
    ),
    training: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "训练度",
      statPercent(unit.training)
    ),
    maintenance: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "维持费",
      formatMoney(unit.maintenanceMoney)
    ),
  }));
}

/** 据点详情 · 农业季作表。 */
export function strongholdCropCycleTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelCropCycleRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  const cycles = stronghold.cropCycles ?? [];
  return cycles.map((cycle) => ({
    name: cycle.name,
    period: formatCropPeriod(cycle.startMonth, cycle.startDay, cycle.endMonth, cycle.endDay),
    progress: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "农事进度",
      `${statPercent(cycle.progressPercent)} / ${statPercent(cycle.progressCapPercent)}`
    ),
    estimatedYield: strongholdHoverFieldValue(
      worldState,
      stronghold,
      "预估产量",
      formatFoodGo(cycle.estimatedYieldGo)
    ),
  }));
}

function mapCityActorIntelRow(
  worldState: StrategyWorldState,
  actor: StrategyStrongholdCityActorState,
  stronghold: StrategyStrongholdState
): IntelCityActorRow {
  const roles = resolveCityActorRoleHolders(worldState, actor, stronghold);
  return {
    id: actor.id,
    name: actor.name,
    primaryLeaderName: roles.primary,
    secondaryLeaderName: roles.secondary,
    tertiaryLeaderName: roles.tertiary,
    typeLabel: cityActorKindLabel(actor.kind),
    branchLabel: resolveCityActorBranchLabel(worldState, actor, stronghold.id),
    characterCount: String(Math.max(0, actor.characterCount)),
  };
}

function strongholdCityActorRowsByKind(
  worldState: StrategyWorldState,
  strongholdId: number,
  kind: "Merchant" | "Religion"
): IntelCityActorRow[] {
  const stronghold = worldState.strongholds.find((s) => s.id === strongholdId);
  if (!stronghold) return [];

  return (stronghold.cityActors ?? [])
    .filter((actor) => actor.kind === kind)
    .map((actor) => mapCityActorIntelRow(worldState, actor, stronghold));
}

/** 据点详情 · 商家表。 */
export function strongholdMerchantTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelCityActorRow[] {
  return strongholdCityActorRowsByKind(worldState, strongholdId, "Merchant");
}

/** 据点详情 · 寺社表。 */
export function strongholdTempleTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelCityActorRow[] {
  return strongholdCityActorRowsByKind(worldState, strongholdId, "Religion");
}

/** @deprecated 使用 strongholdMerchantTableRows / strongholdTempleTableRows */
export function strongholdCityActorTableRows(
  worldState: StrategyWorldState,
  strongholdId: number
): IntelCityActorRow[] {
  return [
    ...strongholdMerchantTableRows(worldState, strongholdId),
    ...strongholdTempleTableRows(worldState, strongholdId),
  ];
}

export function strongholdIntroText(
  worldState: StrategyWorldState,
  strongholdId: number
): string {
  const stronghold = worldState.strongholds.find((item) => item.id === strongholdId);
  if (stronghold?.introduction?.trim()) return stronghold.introduction.trim();

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
  personId: number,
  intelDebugMode = false,
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId, { intelDebugMode });
  if (!row) return [];

  const rows: IntelFieldRow[] = [
    { label: "统率", value: row.leadership },
    { label: "武勇", value: row.power },
    { label: "政治", value: row.politics },
    { label: "智谋", value: row.strategy },
    { label: "魅力", value: row.charm },
  ];

  if (!isIntelDevFieldsVisible(intelDebugMode)) return rows;

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
  personId: number,
  intelDebugMode = false,
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId, { intelDebugMode });
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
  personId: number,
  intelDebugMode = false,
): IntelFieldRow[] {
  const row = findPersonRow(worldState, personId, { intelDebugMode });
  if (!row) return [];

  const character = worldState.characters?.find((item) => item.id === personId);
  const rows: IntelFieldRow[] = [
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
  ];

  if (character && personHasLoyaltyConcept(worldState, character)) {
    rows.push({ label: "忠诚", value: row.loyalty });
  }

  rows.push(
    { label: "命令对象", value: row.commandTarget },
    { label: "出身", value: row.birthType },
    { label: "文化", value: row.cultureName },
    { label: "信仰", value: row.religionName },
    { label: "性别", value: row.sex },
    { label: "年龄", value: row.age }
  );

  return rows;
}

/** 人物详情 · 任务表。 */
export function personTaskTableRows(
  worldState: StrategyWorldState,
  personId: number
): IntelPersonTaskRow[] {
  const character = worldState.characters?.find((item) => item.id === personId);
  if (!character) return [];

  if (character.activeTasks?.length) {
    return character.activeTasks.map((task, index) => ({
      id: index + 1,
      taskType: taskCategoryLabel(task.taskCategory),
      name: task.name,
      target: task.target,
      status: task.status,
      remaining: task.remaining,
    }));
  }

  const rows: IntelPersonTaskRow[] = [];
  let nextId = 1;

  if (
    character.forceStatus === "Task"
    || (character.taskRemainingDays != null && character.taskRemainingDays >= 0)
  ) {
    rows.push({
      id: nextId++,
      taskType: "势力",
      name: "任务",
      target: personStrongholdName(worldState, character),
      status: personStatusLabel(character.forceStatus),
      remaining: formatTaskRemainingDays(character),
    });
  }

  if (character.forceStatus === "UnitAction") {
    rows.push({
      id: nextId++,
      taskType: "势力",
      name: "出阵",
      target: personLocationLabel(worldState, character),
      status: "进行中",
      remaining: "—",
    });
  }

  return rows;
}

export function personIntroText(
  worldState: StrategyWorldState,
  personId: number,
  intelDebugMode = false,
): string {
  const character = worldState.characters?.find((item) => item.id === personId);
  if (character?.introduction?.trim()) return character.introduction.trim();

  const row = findPersonRow(worldState, personId, { intelDebugMode });
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
  relationTone: string;
  characterName: string;
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
    relationTone: rel.relationTone?.trim() || "—",
    characterName: rel.characterName,
  }));
}

/** @deprecated 使用 playerPersonRows */
export const playerCharacterRows = playerPersonRows;
