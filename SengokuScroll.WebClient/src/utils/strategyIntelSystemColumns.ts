/** 情报系统列表列定义（8 列为原则，非硬性上限）。 */
import type { IntelColumnLink } from "@/utils/strategyIntelNavigation";
import { MASTER_DATA_COLUMN_PRESETS } from "@/utils/strategyMasterDataColumns";
import {
  HOBBY_CATEGORY_KEYS,
  PERSONALITY_FIELD_KEYS,
} from "@/utils/strategyCharacterPersonality";
import { resolveIntelTabLabel } from "@/i18n/intelColumns";
import { listPresetTabsForMainTab as listPresetTabsFromBehaviors } from "@/intelDisplay/IntelDisplayBehaviors";
import { organizationRoleLabels } from "@/intelDisplay/OrganizationRoleLabelBehavior";

export interface IntelTableColumnDef {
  prop: string;
  /** @deprecated 优先使用 labelKey 或 ui.intel.column.{prop} 资源键 */
  label?: string;
  labelKey?: string;
  width?: number | string;
  minWidth?: number | string;
  align?: "left" | "center" | "right";
  /** 仅开发阶段显示的列（表头样式区分）。 */
  devOnly?: boolean;
  /** 高/中/低档位着色（情报遮蔽）。 */
  band?: boolean;
  /** 数值偏低（&lt;40 或「低」）时标红，偏高保持默认字色。 */
  lowRedStat?: boolean;
  /** 从行数据读取 tone 字段，用于单元格着色（如外交状态列）。 */
  toneField?: string;
  /** 单元格跳转目标（缺省时按 prop 名匹配通用规则）。 */
  link?: IntelColumnLink;
}

export type ForceListPreset = "status" | "military";
export type StrongholdListPreset = "status" | "supplies" | "military";
export type PersonListPreset =
  | "status"
  | "office"
  | "order"
  | "personal"
  | "ability1"
  | "ability2";

export type MasterDataListPreset =
  | "cultureGroups"
  | "cultures"
  | "religionGroups"
  | "religions"
  | "weathers"
  | "strongholdTypes"
  | "defenseFacilityTypes"
  | "unitTypes"
  | "terrains"
  | "climates"
  | "regions"
  | "roads"
  | "landmarks"
  | "terrainVegetationFeatures"
  | "terrainSurfaceFeatures"
  | "enums";

function pickColumns(
  columns: IntelTableColumnDef[],
  keys: string[]
): IntelTableColumnDef[] {
  return keys
    .map((key) => columns.find((c) => c.prop === key))
    .filter((c): c is IntelTableColumnDef => c != null);
}

export const MASTER_DATA_LIST_COLUMN_PRESETS: Record<
  MasterDataListPreset,
  IntelTableColumnDef[]
> = MASTER_DATA_COLUMN_PRESETS as Record<MasterDataListPreset, IntelTableColumnDef[]>;

const PERSONALITY_DEV_COLUMNS: IntelTableColumnDef[] = [
  ...PERSONALITY_FIELD_KEYS.map((key) => ({
    prop: key,
    labelKey: `enum.personality.${key}`,
    width: 56,
    align: "center" as const,
    devOnly: true,
  })),
  ...HOBBY_CATEGORY_KEYS.map((key) => ({
    prop: key,
    labelKey: `enum.hobby.${key}`,
    width: 56,
    align: "center" as const,
    devOnly: true,
  })),
];

const FORCE_ALL: IntelTableColumnDef[] = [
  { prop: "name", label: "名称", minWidth: 88 },
  { prop: "lordName", label: "当主", minWidth: 80 },
  { prop: "forceType", label: "类型", width: 56, align: "center" },
  { prop: "residenceName", label: "居城", minWidth: 88 },
  { prop: "cultureName", label: "文化", width: 72 },
  { prop: "religionName", label: "信仰", width: 72 },
  { prop: "status", label: "状态", width: 72 },
  { prop: "suzerainName", label: "宗主", minWidth: 80 },
  { prop: "successorName", label: "继承人", minWidth: 80 },
  { prop: "strongholdCount", label: "据点", width: 56, align: "center" },
  { prop: "characterCount", label: "现任", width: 56, align: "center" },
  { prop: "soldiers", label: "兵力", width: 96, align: "right" },
  { prop: "money", label: "金钱", width: 96, align: "right" },
  { prop: "food", label: "粮食", width: 96, align: "right" },
  { prop: "prestige", label: "威望", width: 56, align: "center", lowRedStat: true },
  { prop: "orthodoxy", label: "正统", width: 56, align: "center", lowRedStat: true },
];

export const FORCE_LIST_COLUMN_PRESETS: Record<ForceListPreset, IntelTableColumnDef[]> = {
  status: pickColumns(FORCE_ALL, [
    "name",
    "lordName",
    "forceType",
    "cultureName",
    "religionName",
    "residenceName",
    "status",
    "suzerainName",
    "successorName",
    "strongholdCount",
    "characterCount",
    "prestige",
    "orthodoxy",
  ]),
  military: pickColumns(FORCE_ALL, [
    "name",
    "soldiers",
    "strongholdCount",
    "characterCount",
    "money",
    "food",
  ]),
};

const STRONGHOLD_ALL: IntelTableColumnDef[] = [
  { prop: "name", label: "名称", minWidth: 96 },
  { prop: "category", label: "类型", width: 72 },
  { prop: "scale", label: "规模", width: 56, align: "center" },
  { prop: "forceName", label: "势力", minWidth: 80 },
  { prop: "lordName", label: "领主", minWidth: 80 },
  { prop: "mayorName", label: "代官", minWidth: 80 },
  { prop: "isLordResidence", label: "居城", width: 52, align: "center" },
  { prop: "cityGenerals", label: "现任", width: 80, align: "center" },
  { prop: "population", label: "人口", width: 80, align: "right" },
  { prop: "stability", label: "治安", width: 56, align: "center" },
  { prop: "popularFeelings", label: "民心", width: 56, align: "center" },
  { prop: "cultureName", label: "文化", width: 72 },
  { prop: "religionName", label: "信仰", width: 72 },
  { prop: "maintenance", label: "维持费", width: 88, align: "right" },
  { prop: "isEncircle", label: "包围", width: 52, align: "center" },
  { prop: "isFictional", label: "虚构", width: 52, align: "center" },
  { prop: "money", label: "金钱", width: 96, align: "right" },
  { prop: "food", label: "粮食", width: 96, align: "right" },
  { prop: "pollTaxRate", label: "人头税", width: 72, align: "center" },
  { prop: "agricultureTaxRate", label: "农业税", width: 72, align: "center" },
  { prop: "commerceTaxRate", label: "商业税", width: 72, align: "center" },
  { prop: "tariffTaxRate", label: "关税", width: 72, align: "center" },
  { prop: "defense", label: "城防", width: 72, align: "right" },
  { prop: "garrisonTotal", label: "兵力", width: 88, align: "right" },
  { prop: "morale", label: "士气", width: 56, align: "center" },
  { prop: "training", label: "训练", width: 56, align: "center" },
  { prop: "wounded", label: "伤兵", width: 72, align: "right" },
  { prop: "cropPattern", label: "作型", width: 72, align: "center" },
  { prop: "labor", label: "劳力", width: 88, align: "right" },
  { prop: "cropProgress", label: "农事", width: 96, align: "center" },
  { prop: "militiaPool", label: "农兵", width: 88, align: "right" },
  { prop: "standingProfessional", label: "专业队", width: 88, align: "right" },
  { prop: "standingWounded", label: "伤兵", width: 72, align: "right" },
];

export const STRONGHOLD_LIST_COLUMN_PRESETS: Record<
  StrongholdListPreset,
  IntelTableColumnDef[]
> = {
  status: pickColumns(STRONGHOLD_ALL, [
    "name",
    "forceName",
    "lordName",
    "mayorName",
    "isLordResidence",
    "category",
    "scale",
    "cityGenerals",
    "population",
    "cultureName",
    "religionName",
    "isEncircle",
    "isFictional",
  ]),
  supplies: pickColumns(STRONGHOLD_ALL, [
    "name",
    "money",
    "food",
    "stability",
    "popularFeelings",
    "labor",
    "cropProgress",
    "pollTaxRate",
    "agricultureTaxRate",
    "commerceTaxRate",
    "tariffTaxRate",
    "maintenance",
  ]),
  military: pickColumns(STRONGHOLD_ALL, [
    "name",
    "garrisonTotal",
    "militiaPool",
    "morale",
    "training",
    "wounded",
    "defense",
  ]),
};

/** 任命对话框：据点列表（窄列，minWidth 以便铺满容器）。 */
export const APPOINT_STRONGHOLD_COLUMNS: IntelTableColumnDef[] = [
  { prop: "name", label: "名称", minWidth: 80 },
  { prop: "appointedLord", label: "领主", minWidth: 72 },
  { prop: "mayorName", label: "代官", minWidth: 72 },
];

export const STRONGHOLD_DEFENSE_COLUMNS: IntelTableColumnDef[] = [
  { prop: "category", label: "类别", width: 72 },
  { prop: "name", labelKey: "ui.intel.column.facilityName", label: "设施", minWidth: 96 },
  { prop: "level", label: "等级", width: 56, align: "center" },
  { prop: "defense", label: "城防", width: 72, align: "right" },
];


export const STRONGHOLD_STANDING_GARRISON_COLUMNS: IntelTableColumnDef[] = [
  { prop: "unitName", label: "名称", minWidth: 88 },
  { prop: "typeName", label: "兵种", width: 72 },
  { prop: "roleLabel", label: "类别", width: 72, align: "center" },
  { prop: "isMounted", label: "骑马", width: 56, align: "center" },
  { prop: "soldiers", label: "兵力", width: 80, align: "right" },
  { prop: "morale", label: "士气", width: 56, align: "center" },
  { prop: "training", label: "训练", width: 56, align: "center" },
  { prop: "maintenance", label: "维持费", width: 88, align: "right" },
];

export const STRONGHOLD_CROP_CYCLE_COLUMNS: IntelTableColumnDef[] = [
  { prop: "name", label: "季作", width: 72 },
  { prop: "period", label: "期间", minWidth: 120 },
  { prop: "progress", label: "进度", width: 88, align: "center" },
  { prop: "estimatedYield", label: "预估产量", width: 96, align: "right" },
];

export function cityActorTableColumns(
  kind: "Merchant" | "Religion",
): IntelTableColumnDef[] {
  const labels = organizationRoleLabels(kind);
  return [
    { prop: "name", label: "名称", minWidth: 120 },
    { prop: "primaryLeaderName", label: labels.primary, minWidth: 88 },
    { prop: "secondaryLeaderName", label: labels.secondary, minWidth: 88 },
    { prop: "tertiaryLeaderName", label: labels.tertiary, minWidth: 88 },
    { prop: "branchLabel", label: "分支", width: 56, align: "center" },
    { prop: "characterCount", label: "人数", width: 56, align: "center" },
  ];
}

/** 势力据点 Tab · 组织 Actor 汇总表（非官府；名称后接据点）。 */
export function forceOrgActorTableColumns(
  kind: "Merchant" | "Religion",
): IntelTableColumnDef[] {
  const labels = organizationRoleLabels(kind);
  return [
    { prop: "nameWithStronghold", label: "名称", minWidth: 140 },
    { prop: "primaryLeaderName", label: labels.primary, minWidth: 80 },
    { prop: "secondaryLeaderName", label: labels.secondary, minWidth: 80 },
    { prop: "branchLabel", label: "分支", width: 52, align: "center" },
    { prop: "hostForceName", label: "势力", minWidth: 80 },
    { prop: "hostLordName", label: "领主", minWidth: 80 },
    { prop: "money", label: "金钱", width: 88, align: "right" },
    { prop: "food", label: "粮食", width: 88, align: "right" },
    { prop: "horse", label: "马匹", width: 72, align: "right" },
    { prop: "characterCount", label: "人数", width: 52, align: "center" },
  ];
}

export const STRONGHOLD_FACTION_PRODUCTION_COLUMNS: IntelTableColumnDef[] = [
  { prop: "content", label: "生产内容", minWidth: 96 },
  { prop: "startTime", label: "开始", width: 72, align: "center" },
  { prop: "endTime", label: "结束", width: 72, align: "center" },
  { prop: "efficiency", label: "效率", width: 72, align: "center" },
  { prop: "status", label: "状态", width: 72, align: "center" },
];

export const STRONGHOLD_TECHNOLOGY_COLUMNS: IntelTableColumnDef[] = [
  { prop: "name", label: "技术", minWidth: 88 },
  { prop: "category", label: "类别", width: 72, align: "center" },
  { prop: "status", label: "掌握", width: 72, align: "center" },
  { prop: "condition", label: "有效", minWidth: 120 },
];

export const FORCE_TECHNOLOGY_COLUMNS: IntelTableColumnDef[] = [
  { prop: "name", label: "技术", minWidth: 88 },
  { prop: "category", label: "类别", width: 72, align: "center" },
  { prop: "status", label: "掌握", width: 72, align: "center" },
  { prop: "forceName", label: "势力", minWidth: 80 },
  { prop: "condition", label: "有效", minWidth: 120 },
];

export const PERSON_TASK_COLUMNS: IntelTableColumnDef[] = [
  { prop: "taskType", label: "类型", width: 56, align: "center" },
  { prop: "name", label: "名称", minWidth: 88 },
  { prop: "target", label: "目标", minWidth: 96 },
  { prop: "status", label: "状态", width: 72, align: "center" },
  { prop: "remaining", label: "剩余", width: 72, align: "center" },
];

export const PERSON_RELATION_COLUMNS: IntelTableColumnDef[] = [
  { prop: "characterName", label: "姓名", minWidth: 96 },
  { prop: "relationType", label: "关系", width: 72 },
  { prop: "relationTone", label: "亲疏", width: 72, align: "center" },
];

/** 本家/对方看法 · 影响条目表。 */
export const STANCE_EFFECT_COLUMNS: IntelTableColumnDef[] = [
  { prop: "name", label: "名称", minWidth: 112 },
  { prop: "effectTarget", label: "影响", width: 88 },
  { prop: "magnitude", label: "程度", width: 108, align: "center" },
  { prop: "description", label: "说明", minWidth: 180 },
];

const PERSON_ALL: IntelTableColumnDef[] = [
  { prop: "name", labelKey: "ui.intel.column.personName", label: "姓名", minWidth: 88 },
  { prop: "personType", label: "身份", width: 72, align: "center" },
  { prop: "forceName", label: "势力", minWidth: 80 },
  { prop: "strongholdName", label: "据点", minWidth: 96 },
  { prop: "isFamily", label: "一门", width: 52, align: "center" },
  { prop: "role", label: "职位", width: 72 },
  { prop: "superior", label: "上司", minWidth: 88 },
  { prop: "location", label: "所在", minWidth: 96 },
  { prop: "locationType", label: "位置", width: 72 },
  { prop: "status", label: "状态", width: 72 },
  { prop: "healthStatus", label: "健康", width: 64 },
  { prop: "loyalty", label: "忠诚", width: 56, align: "center" },
  { prop: "yearsInForce", label: "仕官", width: 64, align: "center" },
  { prop: "age", label: "年龄", width: 56, align: "center" },
  { prop: "birthType", label: "出身", width: 72 },
  { prop: "taskRemainingDays", label: "任务剩余天数", width: 88, align: "center" },
  { prop: "leadership", label: "统率", width: 56, align: "center", band: true },
  { prop: "power", label: "武勇", width: 56, align: "center", band: true },
  { prop: "politics", label: "政治", width: 56, align: "center", band: true },
  { prop: "strategy", label: "智谋", width: 56, align: "center", band: true },
  { prop: "charm", label: "魅力", width: 56, align: "center", band: true },
  { prop: "commandTarget", label: "命令对象", minWidth: 96 },
  { prop: "cultureName", label: "文化", width: 72 },
  { prop: "religionName", label: "信仰", width: 72 },
  { prop: "sex", label: "性别", width: 56 },
  ...PERSONALITY_DEV_COLUMNS,
  { prop: "skillInfantry", label: "步兵", width: 56, align: "center", band: true },
  { prop: "skillRide", label: "骑马", width: 56, align: "center", band: true },
  { prop: "skillArchery", label: "弓术", width: 56, align: "center", band: true },
  { prop: "skillFirelock", label: "火枪", width: 56, align: "center", band: true },
  { prop: "skillSealing", label: "航海", width: 56, align: "center", band: true },
  { prop: "skillMilitary", label: "军略", width: 56, align: "center", band: true },
  { prop: "skillFighting", label: "战斗", width: 56, align: "center", band: true },
  { prop: "skillSpy", label: "谍报", width: 56, align: "center", band: true },
  { prop: "skillAgriculture", label: "农业", width: 56, align: "center", band: true },
  { prop: "skillCommerce", label: "商业", width: 56, align: "center", band: true },
  { prop: "skillConstruct", label: "建筑", width: 56, align: "center", band: true },
  { prop: "skillSmelt", label: "冶炼", width: 56, align: "center", band: true },
  { prop: "skillEloquence", label: "辩才", width: 56, align: "center", band: true },
  { prop: "skillCourt", label: "宫廷", width: 56, align: "center", band: true },
  { prop: "skillSociality", label: "交际", width: 56, align: "center", band: true },
  { prop: "skillHealing", label: "医术", width: 56, align: "center", band: true },
];

export const PERSON_SKILL_DETAIL_KEYS_PART1 = [
  "skillInfantry",
  "skillRide",
  "skillArchery",
  "skillFirelock",
  "skillSealing",
  "skillMilitary",
  "skillFighting",
  "skillSpy",
] as const;

export const PERSON_SKILL_DETAIL_KEYS_PART2 = [
  "skillAgriculture",
  "skillCommerce",
  "skillConstruct",
  "skillSmelt",
  "skillEloquence",
  "skillCourt",
  "skillSociality",
  "skillHealing",
] as const;

/** 个人 Tab 中仅开发阶段显示的列。 */
export const PERSON_PERSONAL_DEV_ONLY_PROPS = [
  ...PERSONALITY_FIELD_KEYS,
  ...HOBBY_CATEGORY_KEYS,
] as const;

export const PERSON_LIST_COLUMN_PRESETS: Record<PersonListPreset, IntelTableColumnDef[]> = {
  status: pickColumns(PERSON_ALL, [
    "name",
    "forceName",
    "strongholdName",
    "personType",
    "leadership",
    "power",
    "politics",
    "strategy",
    "charm",
    "healthStatus",
  ]),
  office: pickColumns(PERSON_ALL, ["name", "role", "superior", "yearsInForce", "age", "isFamily"]),
  order: pickColumns(PERSON_ALL, [
    "name",
    "strongholdName",
    "location",
    "status",
    "commandTarget",
    "taskRemainingDays",
  ]),
  personal: pickColumns(PERSON_ALL, [
    "name",
    "sex",
    "age",
    "birthType",
    "cultureName",
    "religionName",
    ...PERSON_PERSONAL_DEV_ONLY_PROPS,
  ]),
  ability1: pickColumns(PERSON_ALL, ["name", ...PERSON_SKILL_DETAIL_KEYS_PART1]),
  ability2: pickColumns(PERSON_ALL, ["name", ...PERSON_SKILL_DETAIL_KEYS_PART2]),
};

/** 据点 · 城中势力人物表（与人物 Tab 状态列一致）。 */
export const STRONGHOLD_FACTION_PERSON_COLUMNS: IntelTableColumnDef[] =
  PERSON_LIST_COLUMN_PRESETS.status;

/** 势力详情 · 据点表。 */
export const FORCE_STRONGHOLD_COLUMNS: IntelTableColumnDef[] = [
  ...pickColumns(STRONGHOLD_ALL, ["name", "forceName"]),
  { prop: "lordName", labelKey: "ui.intel.column.appointedLord", minWidth: 80 },
  ...pickColumns(STRONGHOLD_ALL, [
    "category",
    "scale",
    "population",
    "garrisonTotal",
    "isLordResidence",
  ]),
];

/** 势力详情 · 现任人物表。 */
export const FORCE_PERSON_COLUMNS: IntelTableColumnDef[] = pickColumns(PERSON_ALL, [
  "name",
  "forceName",
  "strongholdName",
  "personType",
  "role",
  "leadership",
  "power",
  "politics",
]);

export const DIPLOMACY_BRIEF_COLUMNS: IntelTableColumnDef[] = [
  { prop: "forceName", label: "势力", minWidth: 96, link: { kind: "force", idField: "forceId" } },
  { prop: "lordName", label: "当主", minWidth: 80 },
  { prop: "relation", label: "关系", width: 56, align: "center" },
  { prop: "trust", label: "信赖", width: 56, align: "center" },
  { prop: "diplomacyStatus", label: "状态", width: 64, toneField: "diplomacyTone" },
  { prop: "politicalStatus", label: "地位", width: 64 },
  { prop: "arrearsMoney", label: "欠钱", width: 88, align: "right" },
  { prop: "arrearsFood", label: "欠粮", width: 88, align: "right" },
];

export type EntityListPreset = ForceListPreset | StrongholdListPreset | PersonListPreset;

export function listPresetTabsForMainTab(
  mainTab: "force" | "stronghold" | "person",
): { name: string; label: string }[] {
  return listPresetTabsFromBehaviors(mainTab);
}

export function masterDataPresetTabs(): { name: MasterDataListPreset; label: string }[] {
  const presets: MasterDataListPreset[] = [
    "cultureGroups",
    "cultures",
    "religionGroups",
    "religions",
    "weathers",
    "strongholdTypes",
    "defenseFacilityTypes",
    "unitTypes",
    "terrains",
    "climates",
    "regions",
    "roads",
    "landmarks",
    "terrainVegetationFeatures",
    "terrainSurfaceFeatures",
    "enums",
  ];
  return presets.map((name) => ({
    name,
    label: resolveIntelTabLabel("master", name, name),
  }));
}
