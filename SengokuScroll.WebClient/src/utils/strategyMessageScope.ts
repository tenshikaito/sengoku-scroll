import type { StrategyEvent } from "@/api/strategyTypes";

export interface MessageScopeSelection {
  player: boolean;
  world: boolean;
}

/** 左上角简报区：我方相关事件。 */
const PLAYER_FEED_CATEGORIES = new Set([
  "PolicyApplied",
  "PolicyDispatched",
  "PolicyDelivered",
  "BattleReportArrived",
  "StrategicReportArrived",
  "MessengerArrived",
  "LordTributeDispatched",
  "LordTributeArrived",
  "SupplyConvoyArrived",
  "EconomyMonthly",
  "EconomyAnnual",
]);

/** 世界/全局类事件（战报过程等）。 */
const WORLD_FEED_CATEGORIES = new Set(["BattleLog", "BattleResolved"]);

export function filterEventsByMessageScope(
  events: StrategyEvent[],
  selection: MessageScopeSelection
): StrategyEvent[] {
  if (!selection.player && !selection.world) return [];

  return events.filter((evt) => {
    if (selection.player && PLAYER_FEED_CATEGORIES.has(evt.category)) return true;
    if (selection.world && WORLD_FEED_CATEGORIES.has(evt.category)) return true;
    return false;
  });
}
