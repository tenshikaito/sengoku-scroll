import type { StrategyBattleResult, StrategyEvent } from "@/api/strategyTypes";
import type { StrategyPendingNotification } from "@/components/strategy/StrategyNotificationTray.vue";
import { messageCategoryLabel } from "@/utils/messageCategories";

let notificationSeq = 0;

export function nextNotificationId(): string {
  notificationSeq += 1;
  return `notify-${Date.now()}-${notificationSeq}`;
}

export function eventBriefText(event: StrategyEvent): string {
  return event.brief?.trim() || event.message;
}

export function battleBriefText(result: StrategyBattleResult): string {
  const outcome = result.attackerWon ? "攻方胜" : "守方胜";
  return `⚔ ${result.attackerName} vs ${result.defenderName}（${outcome}）`;
}

const SETTLEMENT_CATEGORIES = new Set(["EconomyMonthly", "EconomyAnnual"]);

export function isSettlementEvent(event: StrategyEvent): boolean {
  return SETTLEMENT_CATEGORIES.has(event.category);
}

export function notificationFromEvent(event: StrategyEvent): StrategyPendingNotification | null {
  if (isSettlementEvent(event)) {
    return {
      id: nextNotificationId(),
      kind: "economy",
      icon: "📋",
      brief: eventBriefText(event),
      event,
    };
  }

  return null;
}

export function notificationFromBattle(result: StrategyBattleResult): StrategyPendingNotification {
  return {
    id: nextNotificationId(),
    kind: "battle",
    icon: "⚔",
    brief: battleBriefText(result),
    battleResult: result,
  };
}

export function messengerFeedBrief(event: StrategyEvent): string {
  const label = messageCategoryLabel(event.category);
  const brief = event.brief?.trim();
  if (brief) return `[${label}] ${brief}`;
  return `[${label}] ${simplifyLegacyMessage(event)}`;
}

function simplifyLegacyMessage(event: StrategyEvent): string {
  switch (event.category) {
    case "LordTributeDispatched":
      return "贡纳运输队出发";
    case "LordTributeArrived":
      return "贡纳运输队抵达";
    case "SupplyConvoyArrived":
      return "补给运输队抵达";
    case "EconomyMonthly":
      return "月度收支结算";
    case "EconomyAnnual":
      return "年度收支结算";
    case "BattleReportArrived":
      return "战报信使抵达";
    case "MessengerArrived":
      return "信使抵达";
    default:
      return event.message.split(/[：:\n]/)[0]?.slice(0, 48) || event.message;
  }
}
