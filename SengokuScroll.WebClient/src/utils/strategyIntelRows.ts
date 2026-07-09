import type {
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import { messengerMissionLabel, messengerStatusLabel } from "@/utils/logisticsLabels";
import {
  formatFoodGo,
  formatMoney,
  formatSoldiers,
} from "@/utils/strategyDisplayUnits";
import { formatInTransitSupplies, supplyStatusLabel } from "@/utils/strategySupplyLabels";

export interface IntelFieldRow {
  label: string;
  value: string;
}

function dash(value: string | null | undefined): string {
  return value?.trim() ? value : "—";
}

function forceName(worldState: StrategyWorldState, forceId: number): string {
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

/** 悬浮框：单位核心情报（竖向）。 */
export function unitHoverIntelRows(
  worldState: StrategyWorldState,
  unit: StrategyUnitState
): IntelFieldRow[] {
  return [
    { label: "势力", value: forceName(worldState, unit.forceId) },
    { label: "总将", value: dash(unit.commanderName) },
    { label: "兵数", value: formatSoldiers(unit.soldiers) },
    { label: "士气", value: statPercent(unit.morale) },
    { label: "训练度", value: statPercent(unit.training) },
    { label: "文化", value: textOrDash(unit.cultureName) },
    { label: "信仰", value: textOrDash(unit.religionName) },
    { label: "金钱", value: formatMoney(unit.money) },
    { label: "粮草", value: formatFoodGo(unit.food) },
    { label: "补给", value: supplyStatusLabel(unit.supplyStatus) },
  ];
}

/** 对话框：单位完整情报。 */
export function unitDetailIntelRows(
  worldState: StrategyWorldState,
  unit: StrategyUnitState
): IntelFieldRow[] {
  const stronghold =
    worldState.strongholds.find((s) => s.x === unit.x && s.y === unit.y) ?? null;

  return [
    ...unitHoverIntelRows(worldState, unit),
    { label: "位置", value: `(${unit.x}, ${unit.y})` },
    { label: "移动力", value: statPercent(unit.movement) },
    { label: "AP", value: statPercent(unit.ap) },
    { label: "状态", value: textOrDash(unit.status) },
    ...(stronghold ? [{ label: "所在据点", value: stronghold.name }] : []),
    { label: "携粮日数", value: `${unit.foodDaysRemaining} 日` },
    {
      label: "运输中",
      value: formatInTransitSupplies(unit.inTransitSupplies),
    },
  ];
}

/** 悬浮框：据点核心情报（竖向）。 */
export function strongholdHoverIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  const rows: IntelFieldRow[] = [
    { label: "势力", value: forceName(worldState, stronghold.forceId) },
    {
      label: "领主",
      value: stronghold.isDirectRule
        ? `${stronghold.lordName}（当主直辖）`
        : stronghold.lordName,
    },
  ];

  if (stronghold.mayorName) {
    rows.push({ label: "代官", value: stronghold.mayorName });
  }

  rows.push(
    { label: "士气", value: statPercent(stronghold.morale) },
    { label: "训练度", value: statPercent(stronghold.training) },
    { label: "文化", value: textOrDash(stronghold.cultureName) },
    { label: "信仰", value: textOrDash(stronghold.religionName) },
    { label: "金钱", value: formatMoney(stronghold.money) },
    { label: "粮草", value: formatFoodGo(stronghold.food) }
  );

  return rows;
}

function strongholdTaxIntelRows(stronghold: StrategyStrongholdState): IntelFieldRow[] {
  return [
    { label: "人头税", value: `${statPercent(stronghold.pollTaxRate)}%` },
    { label: "农业税", value: `${statPercent(stronghold.agricultureTaxRate)}%` },
    { label: "商业税", value: `${statPercent(stronghold.commerceTaxRate)}%` },
    { label: "关税", value: `${statPercent(stronghold.tariffTaxRate)}%` },
  ];
}

/** 对话框：据点完整情报。 */
export function strongholdDetailIntelRows(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState
): IntelFieldRow[] {
  const garrison = worldState.units.filter(
    (u) => u.x === stronghold.x && u.y === stronghold.y
  );

  const rows: IntelFieldRow[] = [
    ...strongholdHoverIntelRows(worldState, stronghold),
    ...strongholdTaxIntelRows(stronghold),
    { label: "位置", value: `(${stronghold.x}, ${stronghold.y})` },
    { label: "人口", value: safePopulation(stronghold.population) },
  ];

  if (garrison.length) {
    rows.push({
      label: "驻军",
      value: garrison
        .map((u) => `${u.name}（${formatSoldiers(u.soldiers)}）`)
        .join("、"),
    });
  }

  return rows;
}

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
    { label: "粮草", value: formatFoodGo(convoy.food) },
  ];
}

/** 对话框：运输队完整情报。 */
export function convoyDetailIntelRows(
  worldState: StrategyWorldState,
  convoy: StrategySupplyConvoyState
): IntelFieldRow[] {
  return [
    ...convoyHoverIntelRows(worldState, convoy),
    { label: "位置", value: `(${convoy.x}, ${convoy.y})` },
    { label: "移动力", value: statPercent(convoy.movement) },
    { label: "AP", value: statPercent(convoy.ap) },
    { label: "状态", value: textOrDash(convoy.status) },
    { label: "人夫", value: formatSoldiers(convoy.porterCount) },
    { label: "护卫", value: formatSoldiers(convoy.escortSoldierCount) },
    { label: "出发", value: dash(convoy.originStrongholdName) },
    { label: "目标", value: dash(convoy.targetUnitName) },
  ];
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
