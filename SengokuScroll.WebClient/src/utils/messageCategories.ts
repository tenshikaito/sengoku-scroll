import type { StrategyEvent } from "@/api/strategyTypes";

export type MessageCategoryGroup = "policy" | "battle" | "messenger" | "other";

const POLICY_CATEGORIES = new Set([
  "PolicyApplied",
  "PolicyDispatched",
  "PolicyDelivered",
]);

const BATTLE_CATEGORIES = new Set(["BattleReportArrived", "BattleResolved", "BattleLog"]);

const MESSENGER_CATEGORIES = new Set(["MessengerArrived"]);

export function messageCategoryGroup(category: string): MessageCategoryGroup {
  if (POLICY_CATEGORIES.has(category)) return "policy";
  if (BATTLE_CATEGORIES.has(category)) return "battle";
  if (MESSENGER_CATEGORIES.has(category)) return "messenger";
  return "other";
}

export function messageCategoryLabel(category: string): string {
  switch (category) {
    case "PolicyApplied":
      return "方针·即时";
    case "PolicyDispatched":
      return "方针·派出";
    case "PolicyDelivered":
      return "方针·送达";
    case "BattleReportArrived":
      return "战报·抵达";
    case "BattleResolved":
      return "战斗";
    case "BattleLog":
      return "战斗过程";
    case "MessengerArrived":
      return "信使";
    case "EconomyMonthly":
      return "月度收支";
    case "EconomyAnnual":
      return "年度收支";
    case "LordTributeDispatched":
      return "贡纳运输";
    case "LordTributeArrived":
      return "贡纳抵达";
    case "SupplyConvoyArrived":
      return "补给抵达";
    default:
      return category;
  }
}

export function filterEventsByGroups(
  events: StrategyEvent[],
  groups: readonly MessageCategoryGroup[]
): StrategyEvent[] {
  if (groups.length === 0) return [];
  const set = new Set(groups);
  return events.filter((evt) => set.has(messageCategoryGroup(evt.category)));
}

export function formatEventsAsPlainText(events: StrategyEvent[]): string {
  if (!events.length) return "";
  return events
    .map((evt) => `[${messageCategoryLabel(evt.category)}] ${evt.message}`)
    .join("\n");
}

export const MESSAGE_CATEGORY_OPTIONS: { id: MessageCategoryGroup; label: string }[] = [
  { id: "policy", label: "方针" },
  { id: "battle", label: "战报" },
  { id: "messenger", label: "信使" },
  { id: "other", label: "其他" },
];

export const DEFAULT_MESSAGE_CATEGORY_SELECTION: MessageCategoryGroup[] =
  MESSAGE_CATEGORY_OPTIONS.map((o) => o.id);
