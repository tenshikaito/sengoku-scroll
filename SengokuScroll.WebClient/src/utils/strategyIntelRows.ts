import { buildStrongholdGarrisonIntelRows } from "@/intelDisplay/StrongholdGarrisonIntelBehavior";
import {
  buildBattlefieldStatusRows,
  formatBattlefieldParticipantSoldiers,
} from "@/intelDisplay/BattlefieldKindPresentationBehavior";
import {
  battlefieldKindLabel as battlefieldKindLabelFromBehaviors,
  siegeThreatLabel,
} from "@/intelDisplay/IntelDisplayBehaviors";
import {
  compactCellEntityRows as compactCellEntityRowsFromBehaviors,
  type CompactCellEntityEntry,
} from "@/intelDisplay/CompactCellEntityBehaviors";
import type {
  StrategyBattlefieldState,
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import {
  convoyMissionLabel,
  convoyStatusLabel,
  messengerMissionLabel,
  messengerStatusLabel,
} from "@/utils/logisticsLabels";
import {
  formatFoodGo,
  formatMoney,
  formatSoldiers,
} from "@/utils/strategyDisplayUnits";
import { formatInTransitSupplies, supplyStatusLabel } from "@/utils/strategySupplyLabels";
import { unitStanceLabel, unitStatusLabel } from "@/utils/strategyUnitLabels";
import { directiveLabel, pendingPolicyText, siegeModeLabel, unitTargetSummary } from "@/utils/unitDirective";
import {
  hasStrongholdMilitaryEspionageIntel,
  hoverFoodLabel,
  hoverMoneyLabel,
  hoverMoraleLabel,
  hoverSoldiersLabel,
  hoverTrainingLabel,
  isForeignIntelRestricted,
  resolveStrongholdTypeLabel,
  resolveStrongholdScaleLabel,
  shouldObscureStrongholdPersonnel,
  strongholdHoverFieldValue,
  UNKNOWN_INTEL,
} from "@/utils/strategyIntelDisplay";
import { formatStrongholdCityGenerals } from "@/utils/strategyIntelSystemData";
import { governancePriorityLabel } from "@/utils/strategyStrongholdLabels";

export interface IntelFieldRow {
  label: string;
  value: string;
  /** 开发阶段字段（描述列表标题格样式区分）。 */
  dev?: boolean;
}

function battlefieldKindLabel(kind: string | undefined | null): string {
  return battlefieldKindLabelFromBehaviors(kind, textOrDash);
}

function dash(value: string | null | undefined): string {
  return value?.trim() ? value : "—";
}

function forceName(worldState: StrategyWorldState, forceId: number): string {
  if (forceId === 0) return "—";
  return worldState.forces.find((f) => f.id === forceId)?.name ?? "未知势力";
}

function statPercent(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? String(Math.trunc(n)) : "—";
}

function textOrDash(value: unknown): string {
  if (value == null) return "—";
  const s = String(value).trim();
  if (!s || s === "undefined" || s === "null") return "—";
  return s;
}

function safePopulation(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n).toLocaleString() : "—";
}

function formatRoute(points: Array<{ x: number; y: number }> | null | undefined): string {
  if (!points?.length) return "—";
  return points.map((p) => `(${p.x},${p.y})`).join(" → ");
}

function formatFoodDays(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? `${Math.max(0, Math.trunc(n))} 日` : "—";
}

function findBattlefieldAtCell(
  worldState: StrategyWorldState,
  x: number,
  y: number
): StrategyBattlefieldState | null {
  return worldState.battlefields?.find((b) => b.x === x && b.y === y) ?? null;
}

function findBattlefieldById(
  worldState: StrategyWorldState,
  battlefieldId: number | null | undefined
): StrategyBattlefieldState | null {
  if (!battlefieldId || battlefieldId <= 0) return null;
  return worldState.battlefields?.find((b) => b.id === battlefieldId) ?? null;
}

function battlefieldIntelRows(
  battlefield: StrategyBattlefieldState,
  playerForceId: number,
): IntelFieldRow[] {
  const rows: IntelFieldRow[] = [{ label: "战场", value: battlefieldKindLabel(battlefield.kind) }];
  rows.push(...buildBattlefieldStatusRows(battlefield));

  if (battlefield.participants?.length) {
    rows.push({
      label: "参战",
      value: battlefield.participants
        .map((p) =>
          formatBattlefieldParticipantSoldiers(
            battlefield,
            p.forceName,
            p.soldiers,
            p.forceId,
            playerForceId,
          ),
        )
        .join(" / "),
    });
  }

  return rows;
}

function cellEnemyUnitRows(
  worldState: StrategyWorldState,
  x: number,
  y: number,
  ownForceId: number
): IntelFieldRow[] {
  const enemyUnits = worldState.units.filter(
    (u) => u.x === x && u.y === y && u.forceId !== ownForceId
  );
  if (!enemyUnits.length) return [];

  return [
    {
      label: "同格敌军",
      value: enemyUnits
        .map(
          (u) =>
            `${u.name}（${forceName(worldState, u.forceId)}·${formatSoldiers(u.soldiers)}）`
        )
        .join("、"),
    },
  ];
}

/** 悬浮框：单位核心情报（竖向）。 */
export function unitHoverIntelRows(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
  options?: { includeDebugFields?: boolean }
): IntelFieldRow[] {
  const rows: IntelFieldRow[] = [
    { label: "势力", value: forceName(worldState, unit.forceId) },
    { label: "总将", value: dash(unit.commanderName) },
    { label: "兵数", value: hoverSoldiersLabel(worldState, unit) },
    { label: "状态", value: unitStatusLabel(unit.status) },
  ];

  if (options?.includeDebugFields) {
    rows.push({ label: "姿态", value: textOrDash(unit.stance) });
  }

  rows.push(
    { label: "士气", value: hoverMoraleLabel(worldState, unit) },
    { label: "训练度", value: hoverTrainingLabel(worldState, unit) },
    { label: "方针", value: directiveLabel(unit.directive) },
    { label: "目标", value: unitTargetSummary(unit) },
    { label: "攻城", value: siegeModeLabel(unit.siegeMode) },
    { label: "文化", value: textOrDash(unit.cultureName) },
    { label: "信仰", value: textOrDash(unit.religionName) },
    { label: "金钱", value: hoverMoneyLabel(worldState, unit) },
    { label: "粮草", value: hoverFoodLabel(worldState, unit) },
    {
      label: "补给",
      value: isForeignIntelRestricted(worldState, unit.forceId)
        ? UNKNOWN_INTEL
        : supplyStatusLabel(unit.supplyStatus),
    },
  );

  return rows;
}

/** 对话框：单位完整情报。 */
export function unitDetailIntelRows(
  worldState: StrategyWorldState,
  unit: StrategyUnitState
): IntelFieldRow[] {
  const stronghold =
    worldState.strongholds.find((s) => s.x === unit.x && s.y === unit.y) ?? null;
  const battlefield = findBattlefieldById(worldState, unit.battlefieldId);
  const pending = pendingPolicyText(worldState.messageCarriers, unit.id);

  const rows: IntelFieldRow[] = [
    { label: "势力", value: forceName(worldState, unit.forceId) },
    { label: "总将", value: dash(unit.commanderName) },
    { label: "兵数", value: formatSoldiers(unit.soldiers) },
    { label: "士气", value: statPercent(unit.morale) },
    { label: "训练度", value: statPercent(unit.training) },
    { label: "文化", value: textOrDash(unit.cultureName) },
    { label: "信仰", value: textOrDash(unit.religionName) },
    { label: "状态", value: unitStatusLabel(unit.status) },
    { label: "姿态", value: unitStanceLabel(unit.stance) },
    { label: "方针", value: directiveLabel(unit.directive) },
    { label: "目标", value: unitTargetSummary(unit) },
    { label: "攻城", value: siegeModeLabel(unit.siegeMode) },
  ];

  if (pending) {
    rows.push({ label: "信使", value: pending });
  }

  if (unit.route?.length) {
    rows.push({ label: "路径", value: formatRoute(unit.route) });
  }

  if (battlefield) {
    rows.push(...battlefieldIntelRows(battlefield, worldState.playerForceId));
  }

  rows.push(
    { label: "金钱", value: formatMoney(unit.money) },
    { label: "粮草", value: formatFoodGo(unit.food) },
    { label: "补给", value: supplyStatusLabel(unit.supplyStatus) },
    { label: "携粮日数", value: formatFoodDays(unit.foodDaysRemaining) },
    { label: "运输中", value: formatInTransitSupplies(unit.inTransitSupplies) },
    { label: "位置", value: `(${unit.x}, ${unit.y})` },
    ...(stronghold ? [{ label: "所在据点", value: stronghold.name }] : []),
    { label: "移动力", value: statPercent(unit.movement) },
    { label: "AP", value: statPercent(unit.ap) }
  );

  return rows;
}

function formatDefense(value: unknown): string {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n).toLocaleString() : "—";
}

function strongholdGarrisonSoldiers(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState
): number {
  const city = Number.isFinite(Number(stronghold.garrisonSoldiers))
    ? Math.max(0, Math.trunc(Number(stronghold.garrisonSoldiers)))
    : 0;
  if (
    isForeignIntelRestricted(worldState, stronghold.forceId) &&
    !hasStrongholdMilitaryEspionageIntel(stronghold)
  ) {
    return city;
  }
  const field = worldState.units
    .filter(
      (u) =>
        u.x === stronghold.x &&
        u.y === stronghold.y &&
        u.forceId === stronghold.forceId
    )
    .reduce(
      (sum, u) => sum + (Number.isFinite(Number(u.soldiers)) ? Math.max(0, Math.trunc(Number(u.soldiers))) : 0),
      0
    );
  return city + field;
}

/** 据点核心情报（不含城防）。 */
function strongholdCoreIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  const obscurePersonnel = shouldObscureStrongholdPersonnel(worldState, stronghold);

  const rows: IntelFieldRow[] = [
    { label: "名称", value: stronghold.name },
    { label: "势力", value: forceName(worldState, stronghold.forceId) },
    {
      label: "领主",
      value: obscurePersonnel ? UNKNOWN_INTEL : stronghold.lordName,
    },
    {
      label: "代官",
      value: obscurePersonnel ? UNKNOWN_INTEL : (stronghold.mayorName?.trim() || "—"),
    },
    {
      label: "方针",
      value: obscurePersonnel
        ? UNKNOWN_INTEL
        : governancePriorityLabel(stronghold.governancePriority),
    },
    {
      label: "现任",
      value: obscurePersonnel
        ? UNKNOWN_INTEL
        : formatStrongholdCityGenerals(worldState, stronghold.id),
    },
    { label: "类型", value: resolveStrongholdTypeLabel(stronghold, worldState) },
    {
      label: "规模",
      value: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "规模",
        resolveStrongholdScaleLabel(stronghold.population),
      ),
    },
  ];

  rows.push(
    { label: "人口", value: strongholdHoverFieldValue(worldState, stronghold, "人口", safePopulation(stronghold.population)) },
    { label: "金钱", value: strongholdHoverFieldValue(worldState, stronghold, "金钱", formatMoney(stronghold.money)) },
    { label: "粮食", value: strongholdHoverFieldValue(worldState, stronghold, "粮食", formatFoodGo(stronghold.food)) },
    {
      label: "兵力",
      value: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "兵力",
        formatSoldiers(strongholdGarrisonSoldiers(worldState, stronghold)),
      ),
    },
    { label: "士气", value: strongholdHoverFieldValue(worldState, stronghold, "士气", statPercent(stronghold.morale)) },
    { label: "训练度", value: strongholdHoverFieldValue(worldState, stronghold, "训练度", statPercent(stronghold.training)) },
  );

  if (stronghold.siegeThreat) {
    rows.push({ label: "被攻状态", value: siegeThreatLabel(stronghold.siegeThreat) });
  }

  return rows;
}

/** 悬浮框：据点核心情报（竖向）。 */
export function strongholdHoverIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  const rows = strongholdCoreIntelRows(worldState, stronghold);
  rows.push({
    label: "城防",
    value: strongholdHoverFieldValue(worldState, stronghold, "城防", formatDefense(stronghold.defense)),
  });
  return rows;
}

function strongholdTaxIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): IntelFieldRow[] {
  return [
    {
      label: "人头税",
      value: strongholdHoverFieldValue(worldState, stronghold, "人头税", `${statPercent(stronghold.pollTaxRate)}%`),
    },
    {
      label: "农业税",
      value: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "农业税",
        `${statPercent(stronghold.agricultureTaxRate)}%`
      ),
    },
    {
      label: "商业税",
      value: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "商业税",
        `${statPercent(stronghold.commerceTaxRate)}%`
      ),
    },
    {
      label: "关税",
      value: strongholdHoverFieldValue(
        worldState,
        stronghold,
        "关税",
        `${statPercent(stronghold.tariffTaxRate)}%`
      ),
    },
  ];
}

/** 对话框「基本信息」Tab。 */
export function strongholdDetailIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
  options?: { includeBattleIntel?: boolean }
): IntelFieldRow[] {
  const includeBattleIntel = options?.includeBattleIntel ?? true;
  const cityGarrison = Number.isFinite(Number(stronghold.garrisonSoldiers))
    ? Math.max(0, Math.trunc(Number(stronghold.garrisonSoldiers)))
    : 0;

  const rows: IntelFieldRow[] = [
    ...strongholdCoreIntelRows(worldState, stronghold).filter((row) => row.label !== "兵力"),
    ...strongholdTaxIntelRows(worldState, stronghold),
    { label: "虚构", value: stronghold.isHistorical ? "×" : "○" },
  ];

  rows.push(
    ...buildStrongholdGarrisonIntelRows({
      worldState,
      stronghold,
      cityGarrison,
      includeBattleIntel,
      formatSoldiers,
      strongholdHoverFieldValue,
      cellEnemyUnitRows,
      battlefieldIntelRows,
      findBattlefieldAtCell,
    }),
  );

  return rows;
}

/** 对话框「城防信息」Tab。 */
export function strongholdDefenseIntelRows(
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  return [
    { label: "城防", value: formatDefense(stronghold.defense) },
    ...strongholdDefenseFacilityRows(stronghold),
  ];
}

const defenseFacilityCategoryLabels: Record<string, string> = {
  Castle: "城堡",
  Wall: "城墙",
  Gate: "城门",
  Moat: "护城河",
  Defender: "防御设施",
};

/** 城防设施类别显示名。 */
export function defenseFacilityCategoryLabel(category: string): string {
  return defenseFacilityCategoryLabels[category] ?? category;
}

/** 对话框「城防信息」Tab：设施列表行。 */
export function strongholdDefenseFacilityRows(
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  if (!stronghold.defenseFacilities.length) {
    return [{ label: "设施", value: "暂无城防设施" }];
  }

  return stronghold.defenseFacilities.map((facility) => ({
    label: defenseFacilityCategoryLabel(facility.category),
    value: `${facility.name} · Lv.${facility.level} · 城防+${facility.defense}`,
  }));
}

/** 多单位同格紧凑悬浮：名称、势力、将领、兵数。 */
export function compactCellEntityRows(
  worldState: StrategyWorldState,
  entry: CompactCellEntityEntry
): IntelFieldRow[] {
  return compactCellEntityRowsFromBehaviors(worldState, entry);
}

export type { CompactCellEntityEntry };

/** 悬浮框：运输队（精简字段）。 */
export function convoyHoverIntelRows(
  worldState: StrategyWorldState,
  convoy: StrategySupplyConvoyState
): IntelFieldRow[] {
  return [
    { label: "势力", value: forceName(worldState, convoy.forceId) },
    { label: "总将", value: dash(convoy.commanderName) },
    { label: "兵数", value: formatSoldiers(convoy.soldiers) },
    { label: "士气", value: statPercent(convoy.morale) },
    { label: "金钱", value: formatMoney(convoy.money) },
    { label: "粮草", value: formatFoodGo(convoy.food) },
  ];
}

/** 对话框：运输队完整情报。 */
export function convoyDetailIntelRows(
  worldState: StrategyWorldState,
  convoy: StrategySupplyConvoyState
): IntelFieldRow[] {
  const rows: IntelFieldRow[] = [
    ...convoyHoverIntelRows(worldState, convoy),
    { label: "任务", value: convoyMissionLabel(convoy) },
    {
      label: "状态",
      value: convoyStatusLabel(convoy.status, convoy.isReturningToOrigin),
    },
    { label: "载粮", value: formatFoodGo(convoy.cargoFoodGo) },
    { label: "位置", value: `(${convoy.x}, ${convoy.y})` },
    { label: "移动力", value: statPercent(convoy.movement) },
    { label: "AP", value: statPercent(convoy.ap) },
    { label: "人夫", value: formatSoldiers(convoy.porterCount) },
    { label: "护卫", value: formatSoldiers(convoy.escortSoldierCount) },
    { label: "出发", value: dash(convoy.originStrongholdName) },
    { label: "目标", value: dash(convoy.targetUnitName) },
  ];

  if (convoy.route?.length) {
    rows.push({ label: "路径", value: formatRoute(convoy.route) });
  }

  return rows;
}

/** 对话框：信使完整情报。 */
export function messengerDetailIntelRows(
  worldState: StrategyWorldState,
  messenger: StrategyMessengerState
): IntelFieldRow[] {
  const rows: IntelFieldRow[] = [
    ...messengerHoverIntelRows(worldState, messenger),
    { label: "位置", value: `(${messenger.x}, ${messenger.y})` },
    { label: "移动力", value: statPercent(messenger.movement) },
    { label: "AP", value: statPercent(messenger.ap) },
    { label: "出发", value: dash(messenger.originStrongholdName) },
    { label: "目标部队", value: dash(messenger.targetUnitName) },
  ];

  if (messenger.pendingDirective) {
    rows.push({ label: "待传达", value: directiveLabel(messenger.pendingDirective) });
  }

  if (messenger.route?.length) {
    rows.push({ label: "路径", value: formatRoute(messenger.route) });
  }

  return rows;
}

/** 悬浮框：信使（非军事单位，无总将；NPC 传令兵/护卫）。 */
export function messengerHoverIntelRows(
  worldState: StrategyWorldState,
  messenger: StrategyMessengerState
): IntelFieldRow[] {
  return [
    { label: "势力", value: forceName(worldState, messenger.forceId) },
    { label: "兵数", value: formatSoldiers(messenger.soldiers) },
    { label: "传令", value: formatSoldiers(messenger.courierCount) },
    { label: "护卫", value: formatSoldiers(messenger.escortSoldierCount) },
    { label: "士气", value: statPercent(messenger.morale) },
    { label: "训练度", value: statPercent(messenger.training) },
    { label: "任务", value: messengerMissionLabel(messenger) },
    { label: "状态", value: messengerStatusLabel(messenger.status) },
  ];
}
