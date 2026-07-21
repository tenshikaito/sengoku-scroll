import type { StrategyEvent } from "@/api/strategyTypes";
import { enumLabel, t } from "@/i18n/textLocalizer";

export type MessageCategoryGroup = "policy" | "battle" | "messenger" | "other";

const POLICY_CATEGORIES = new Set([
  "PolicyApplied",
  "PolicyDispatched",
  "PolicyDelivered",
]);

const BATTLE_CATEGORIES = new Set([
  "BattleReportArrived",
  "StrategicReportArrived",
  "BattleResolved",
  "BattleLog",
  "UnitDestroyed",
  "UnitFledToStronghold",
  "StrongholdCaptured",
  "SiegeOrderStarted",
]);

const MESSENGER_CATEGORIES = new Set(["MessengerArrived"]);

export function messageCategoryGroup(category: string): MessageCategoryGroup {
  if (POLICY_CATEGORIES.has(category)) return "policy";
  if (BATTLE_CATEGORIES.has(category)) return "battle";
  if (MESSENGER_CATEGORIES.has(category)) return "messenger";
  return "other";
}

export function messageCategoryLabel(category: string): string {
  return enumLabel("enum.message.category", category, category);
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

const MESSAGE_CATEGORY_GROUP_IDS: MessageCategoryGroup[] = [
  "policy",
  "battle",
  "messenger",
  "other",
];

export function getMessageCategoryOptions(): { id: MessageCategoryGroup; label: string }[] {
  return MESSAGE_CATEGORY_GROUP_IDS.map((id) => ({
    id,
    label: t(`enum.message.group.${id}`),
  }));
}

export const DEFAULT_MESSAGE_CATEGORY_SELECTION: MessageCategoryGroup[] = [
  ...MESSAGE_CATEGORY_GROUP_IDS,
];
