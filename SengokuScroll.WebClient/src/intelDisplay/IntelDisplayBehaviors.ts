export abstract class DiplomacyRelationLabelBehavior {
  abstract readonly relation: string;
  abstract readonly label: string;
  abstract readonly tone: string;
}

class AlliedRelationBehavior extends DiplomacyRelationLabelBehavior {
  readonly relation = "Allied";
  readonly label = "同盟";
  readonly tone = "allied";
}

class EnemyRelationBehavior extends DiplomacyRelationLabelBehavior {
  readonly relation = "Enemy";
  readonly label = "战争";
  readonly tone = "enemy";
}

class NeutralRelationBehavior extends DiplomacyRelationLabelBehavior {
  readonly relation = "Neutral";
  readonly label = "中立";
  readonly tone = "neutral";
}

const RELATION_BEHAVIORS: DiplomacyRelationLabelBehavior[] = [
  new AlliedRelationBehavior(),
  new EnemyRelationBehavior(),
  new NeutralRelationBehavior(),
];

const DEFAULT_RELATION = new NeutralRelationBehavior();

export function diplomacyStatusLabel(relation: string | undefined | null): string {
  return RELATION_BEHAVIORS.find((b) => b.relation === relation)?.label ?? "—";
}

export function diplomacyToneFromRelation(relation: string | undefined | null): string {
  return RELATION_BEHAVIORS.find((b) => b.relation === relation)?.tone ?? DEFAULT_RELATION.tone;
}

export abstract class DiplomacyToneRowClassBehavior {
  abstract readonly tone: string;
  abstract readonly className: string;
}

class AlliedToneRowClassBehavior extends DiplomacyToneRowClassBehavior {
  readonly tone = "allied";
  readonly className = "dip-allied";
}

class EnemyToneRowClassBehavior extends DiplomacyToneRowClassBehavior {
  readonly tone = "enemy";
  readonly className = "dip-enemy";
}

class NeutralToneRowClassBehavior extends DiplomacyToneRowClassBehavior {
  readonly tone = "neutral";
  readonly className = "dip-neutral";
}

const DIPLOMACY_ROW_CLASS_BEHAVIORS: DiplomacyToneRowClassBehavior[] = [
  new AlliedToneRowClassBehavior(),
  new EnemyToneRowClassBehavior(),
  new NeutralToneRowClassBehavior(),
];

export function diplomacyRowClassName(tone: string | undefined | null): string {
  const key = tone?.trim() ?? "";
  return DIPLOMACY_ROW_CLASS_BEHAVIORS.find((b) => b.tone === key)?.className ?? "";
}

export abstract class IntelBandToneBehavior {
  abstract readonly band: string;
  abstract readonly tone: "high" | "mid" | "low";
}

class HighBandToneBehavior extends IntelBandToneBehavior {
  readonly band = "高";
  readonly tone = "high";
}

class MidBandToneBehavior extends IntelBandToneBehavior {
  readonly band = "中";
  readonly tone = "mid";
}

class LowBandToneBehavior extends IntelBandToneBehavior {
  readonly band = "低";
  readonly tone = "low";
}

const BAND_TONE_BEHAVIORS: IntelBandToneBehavior[] = [
  new HighBandToneBehavior(),
  new MidBandToneBehavior(),
  new LowBandToneBehavior(),
];

export type IntelBandTone = "high" | "mid" | "low";

export function resolveIntelBandTone(value: string | undefined | null): IntelBandTone | null {
  const trimmed = value?.trim();
  return BAND_TONE_BEHAVIORS.find((b) => b.band === trimmed)?.tone ?? null;
}

export abstract class BattlefieldKindLabelBehavior {
  abstract readonly kind: string;
  abstract readonly label: string;
}

class SiegeBattlefieldKindBehavior extends BattlefieldKindLabelBehavior {
  readonly kind = "Siege";
  readonly label = "攻城战";
}

class FieldBattlefieldKindBehavior extends BattlefieldKindLabelBehavior {
  readonly kind = "Field";
  readonly label = "野战";
}

const BATTLEFIELD_KIND_BEHAVIORS: BattlefieldKindLabelBehavior[] = [
  new SiegeBattlefieldKindBehavior(),
  new FieldBattlefieldKindBehavior(),
];

export function battlefieldKindLabel(
  kind: string | undefined | null,
  fallback: (v: unknown) => string,
): string {
  const found = BATTLEFIELD_KIND_BEHAVIORS.find((b) => b.kind === kind);
  if (found) return found.label;
  return fallback(kind);
}

export abstract class SiegeThreatLabelBehavior {
  abstract readonly threat: string;
  abstract readonly label: string;
}

class AssaultSiegeThreatBehavior extends SiegeThreatLabelBehavior {
  readonly threat = "Assault";
  readonly label = "强攻";
}

class EncircleSiegeThreatBehavior extends SiegeThreatLabelBehavior {
  readonly threat = "Encircle";
  readonly label = "围城";
}

const SIEGE_THREAT_BEHAVIORS: SiegeThreatLabelBehavior[] = [
  new AssaultSiegeThreatBehavior(),
  new EncircleSiegeThreatBehavior(),
];

export function siegeThreatLabel(threat: string | undefined | null): string {
  return SIEGE_THREAT_BEHAVIORS.find((b) => b.threat === threat)?.label ?? "—";
}

abstract class EnumLabelBehavior {
  abstract readonly key: string;
  abstract readonly label: string;
}

function enumLabel(
  behaviors: EnumLabelBehavior[],
  value: string | undefined | null,
  passthrough = false,
): string {
  const found = behaviors.find((b) => b.key === value);
  if (found) return found.label;
  const trimmed = value?.trim();
  if (passthrough && trimmed) return trimmed;
  return "—";
}

class MaleSexLabelBehavior extends EnumLabelBehavior {
  readonly key = "Male";
  readonly label = "男";
}

class FemaleSexLabelBehavior extends EnumLabelBehavior {
  readonly key = "Female";
  readonly label = "女";
}

const SEX_LABEL_BEHAVIORS: EnumLabelBehavior[] = [
  new MaleSexLabelBehavior(),
  new FemaleSexLabelBehavior(),
];

export function sexLabel(value: string | undefined | null): string {
  return enumLabel(SEX_LABEL_BEHAVIORS, value, true);
}

class RoyalFamilyBirthTypeBehavior extends EnumLabelBehavior {
  readonly key = "RoyalFamily";
  readonly label = "皇族";
}

class NobleBirthTypeBehavior extends EnumLabelBehavior {
  readonly key = "Noble";
  readonly label = "贵族";
}

class LandlordBirthTypeBehavior extends EnumLabelBehavior {
  readonly key = "Landlord";
  readonly label = "勋贵";
}

class NormalBirthTypeBehavior extends EnumLabelBehavior {
  readonly key = "Normal";
  readonly label = "平民";
}

class SlaveBirthTypeBehavior extends EnumLabelBehavior {
  readonly key = "Slave";
  readonly label = "奴隶";
}

const BIRTH_TYPE_BEHAVIORS: EnumLabelBehavior[] = [
  new RoyalFamilyBirthTypeBehavior(),
  new NobleBirthTypeBehavior(),
  new LandlordBirthTypeBehavior(),
  new NormalBirthTypeBehavior(),
  new SlaveBirthTypeBehavior(),
];

export function birthTypeLabel(value: string | undefined | null): string {
  return enumLabel(BIRTH_TYPE_BEHAVIORS, value, true);
}

class IndependenceForceStatusBehavior extends EnumLabelBehavior {
  readonly key = "Independence";
  readonly label = "独立";
}

class InnerVassalForceStatusBehavior extends EnumLabelBehavior {
  readonly key = "InnerVassal";
  readonly label = "内藩";
}

class OuterVassalForceStatusBehavior extends EnumLabelBehavior {
  readonly key = "OuterVassal";
  readonly label = "外藩";
}

const FORCE_POLITICAL_STATUS_BEHAVIORS: EnumLabelBehavior[] = [
  new IndependenceForceStatusBehavior(),
  new InnerVassalForceStatusBehavior(),
  new OuterVassalForceStatusBehavior(),
];

export function forcePoliticalStatusLabel(status: string | undefined | null): string {
  return enumLabel(FORCE_POLITICAL_STATUS_BEHAVIORS, status, true);
}

class IdlePersonStatusBehavior extends EnumLabelBehavior {
  readonly key = "Idle";
  readonly label = "空闲";
}

class UnitActionPersonStatusBehavior extends EnumLabelBehavior {
  readonly key = "UnitAction";
  readonly label = "出阵";
}

class TaskPersonStatusBehavior extends EnumLabelBehavior {
  readonly key = "Task";
  readonly label = "任务中";
}

class PrisonerPersonStatusBehavior extends EnumLabelBehavior {
  readonly key = "Prisoner";
  readonly label = "俘虏";
}

const PERSON_STATUS_BEHAVIORS: EnumLabelBehavior[] = [
  new IdlePersonStatusBehavior(),
  new UnitActionPersonStatusBehavior(),
  new TaskPersonStatusBehavior(),
  new PrisonerPersonStatusBehavior(),
];

export function personStatusLabel(status: string | undefined | null): string {
  return enumLabel(PERSON_STATUS_BEHAVIORS, status, true);
}

class StrongholdLocationTypeBehavior extends EnumLabelBehavior {
  readonly key = "Stronghold";
  readonly label = "据点";
}

class UnitLocationTypeBehavior extends EnumLabelBehavior {
  readonly key = "Unit";
  readonly label = "部队";
}

class MapLocationTypeBehavior extends EnumLabelBehavior {
  readonly key = "Map";
  readonly label = "地图";
}

const PERSON_LOCATION_TYPE_BEHAVIORS: EnumLabelBehavior[] = [
  new StrongholdLocationTypeBehavior(),
  new UnitLocationTypeBehavior(),
  new MapLocationTypeBehavior(),
];

export function personLocationTypeLabel(value: string | undefined | null): string {
  return enumLabel(PERSON_LOCATION_TYPE_BEHAVIORS, value, true);
}

export type IntelMainTabId = "force" | "stronghold" | "person";

export interface IntelTabDef {
  name: string;
  label: string;
}

export abstract class IntelMainTabBehavior {
  abstract readonly mainTab: IntelMainTabId;
  abstract readonly tabs: IntelTabDef[];
}

export function resolveIntelTabLabel(
  _entity: IntelMainTabId,
  _tab: string,
  fallback: string,
): string {
  return fallback;
}

class ForceMainTabBehavior extends IntelMainTabBehavior {
  readonly mainTab = "force" as const;
  readonly tabs = [
    { name: "status", label: resolveIntelTabLabel("force", "status", "状态") },
    { name: "military", label: resolveIntelTabLabel("force", "military", "军备") },
  ];
}

class StrongholdMainTabBehavior extends IntelMainTabBehavior {
  readonly mainTab = "stronghold" as const;
  readonly tabs = [
    { name: "status", label: resolveIntelTabLabel("stronghold", "status", "状态") },
    { name: "supplies", label: resolveIntelTabLabel("stronghold", "supplies", "内政") },
    { name: "military", label: resolveIntelTabLabel("stronghold", "military", "军备") },
  ];
}

class PersonMainTabBehavior extends IntelMainTabBehavior {
  readonly mainTab = "person" as const;
  readonly tabs = [
    { name: "status", label: resolveIntelTabLabel("person", "status", "状态") },
    { name: "office", label: resolveIntelTabLabel("person", "office", "仕官") },
    { name: "order", label: resolveIntelTabLabel("person", "order", "命令") },
    { name: "personal", label: resolveIntelTabLabel("person", "personal", "个人") },
    { name: "ability1", label: resolveIntelTabLabel("person", "ability1", "能力1") },
    { name: "ability2", label: resolveIntelTabLabel("person", "ability2", "能力2") },
  ];
}

const MAIN_TAB_BEHAVIORS: Record<IntelMainTabId, IntelMainTabBehavior> = {
  force: new ForceMainTabBehavior(),
  stronghold: new StrongholdMainTabBehavior(),
  person: new PersonMainTabBehavior(),
};

export function listPresetTabsForMainTab(mainTab: IntelMainTabId): IntelTabDef[] {
  return MAIN_TAB_BEHAVIORS[mainTab]?.tabs ?? [];
}
