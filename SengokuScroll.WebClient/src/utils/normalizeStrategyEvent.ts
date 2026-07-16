import type {
  StrategyEconomySettlementDetail,
  StrategyEvent,
  StrategyTributeLine,
} from "@/api/strategyTypes";
import { normalizeBattleResult } from "@/utils/battleResult";

function pick(obj: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (key in obj) return obj[key];
  }
  return undefined;
}

function safeInt(value: unknown, fallback = 0): number {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function optionalString(value: unknown): string | undefined {
  if (typeof value !== "string") return undefined;
  const s = value.trim();
  return s ? s : undefined;
}

function normalizeTributeLine(raw: unknown): StrategyTributeLine {
  const row = raw as Record<string, unknown>;
  return {
    originName: String(pick(row, "originName", "OriginName") ?? "未知据点"),
    forceName: String(pick(row, "forceName", "ForceName") ?? "—"),
    lordName: String(pick(row, "lordName", "LordName") ?? "—"),
    food: safeInt(pick(row, "food", "Food")),
    money: safeInt(pick(row, "money", "Money")),
  };
}

function normalizePeriod(raw: unknown, category: string): "Monthly" | "Annual" {
  const period = String(raw ?? "").trim();
  if (period === "Annual" || period === "annual") return "Annual";
  if (period === "Monthly" || period === "monthly") return "Monthly";
  return category === "EconomyAnnual" ? "Annual" : "Monthly";
}

export function normalizeEconomySettlementDetail(
  raw: unknown,
  category = ""
): StrategyEconomySettlementDetail | undefined {
  if (!raw || typeof raw !== "object") return undefined;
  const d = raw as Record<string, unknown>;
  const linesRaw = pick(d, "tributeLines", "TributeLines");
  const tributeLines = Array.isArray(linesRaw) ? linesRaw.map(normalizeTributeLine) : [];

  return {
    period: normalizePeriod(pick(d, "period", "Period"), category),
    reportingYear: safeInt(pick(d, "reportingYear", "ReportingYear")),
    reportingMonth: safeInt(pick(d, "reportingMonth", "ReportingMonth")),
    totalFood: safeInt(pick(d, "totalFood", "TotalFood")),
    totalMoney: safeInt(pick(d, "totalMoney", "TotalMoney")),
    expenseMoney: safeInt(pick(d, "expenseMoney", "ExpenseMoney")),
    armyMaintenanceMoney: safeInt(pick(d, "armyMaintenanceMoney", "ArmyMaintenanceMoney")),
    treasuryMoney: safeInt(pick(d, "treasuryMoney", "TreasuryMoney")),
    treasuryFood: safeInt(pick(d, "treasuryFood", "TreasuryFood")),
    convoyCount: safeInt(pick(d, "convoyCount", "ConvoyCount"), tributeLines.length),
    tributeLines,
  };
}

/** 从事件 Message 文本解析收支结算（后端未带结构化字段时的兜底）。 */
export function parseEconomySettlementFromEvent(
  event: StrategyEvent
): StrategyEconomySettlementDetail | null {
  if (event.economySettlement) return event.economySettlement;

  const msg = event.message;
  if (!msg) return null;

  const isAnnual = event.category === "EconomyAnnual" || /年度收支结算/.test(msg);
  const period: "Monthly" | "Annual" = isAnnual ? "Annual" : "Monthly";

  const yearMatch = msg.match(/(\d{3,4})年/);
  const monthMatch = msg.match(/(\d{1,2})月/);
  const reportingYear = yearMatch ? safeInt(yearMatch[1]) : 0;
  const reportingMonth = isAnnual ? 0 : monthMatch ? safeInt(monthMatch[1]) : 0;

  const incomeMatch =
    msg.match(/(?:合计收入|贡纳收入)\s*🌾([\d,]+)(?:合)?\s*💰([\d,]+)(?:文)?/) ??
    msg.match(/合计收入\s*🌾([\d,]+)\s*💰([\d,]+)/);
  const expenseMatch =
    msg.match(/(?:维持费支出|支出)\s*💰([\d,]+)(?:文)?/) ?? msg.match(/支出\s*💰([\d,]+)/);
  const armyMatch = msg.match(/军队维护\s*💰([\d,]+)(?:文)?/);
  const treasuryMatch =
    msg.match(/(?:结算后库藏|库藏)\s*💰([\d,]+)(?:文)?\s*🌾([\d,]+)(?:合)?/) ??
    msg.match(/库藏\s*💰([\d,]+)\s*🌾([\d,]+)/);
  const convoyMatch = msg.match(/共\s*(\d+)\s*批运输队/);

  const parseNum = (s: string | undefined) => safeInt(s?.replace(/,/g, ""));

  const tributeLines: StrategyTributeLine[] = [];
  for (const line of msg.split("\n")) {
    const m = line.match(/·\s*(.+?)：🌾([\d,]+)\s*💰([\d,]+)/);
    if (!m) continue;
    tributeLines.push({
      originName: m[1]!.trim(),
      forceName: "—",
      lordName: "—",
      food: parseNum(m[2]),
      money: parseNum(m[3]),
    });
  }

  if (!yearMatch && !incomeMatch && tributeLines.length === 0) return null;

  return {
    period,
    reportingYear,
    reportingMonth,
    totalFood: parseNum(incomeMatch?.[1]),
    totalMoney: parseNum(incomeMatch?.[2]),
    expenseMoney: parseNum(expenseMatch?.[1]),
    armyMaintenanceMoney: parseNum(armyMatch?.[1]),
    treasuryMoney: parseNum(treasuryMatch?.[1]),
    treasuryFood: parseNum(treasuryMatch?.[2]),
    convoyCount: convoyMatch ? safeInt(convoyMatch[1]) : tributeLines.length,
    tributeLines,
  };
}

export function normalizeStrategyEvent(raw: unknown): StrategyEvent {
  const evt = raw as Record<string, unknown>;
  const briefRaw = pick(evt, "brief", "Brief");
  const category = String(pick(evt, "category", "Category") ?? "");
  const economyRaw =
    pick(evt, "economySettlement", "EconomySettlement") ??
    pick(evt, "economyMonthly", "EconomyMonthly");
  const battleRaw = pick(evt, "battleResult", "BattleResult");
  const detailCategory = optionalString(pick(evt, "detailCategory", "DetailCategory"));
  const detailMessage = optionalString(pick(evt, "detailMessage", "DetailMessage"));

  return {
    category,
    message: String(pick(evt, "message", "Message") ?? ""),
    brief: optionalString(briefRaw),
    economySettlement: normalizeEconomySettlementDetail(economyRaw, category),
    battleResult: battleRaw ? normalizeBattleResult(battleRaw) : undefined,
    detailCategory,
    detailMessage,
  };
}
