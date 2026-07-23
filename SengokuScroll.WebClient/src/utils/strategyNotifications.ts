import type { StrategyBattleResult, StrategyEvent, StrategyWorldState } from "@/api/strategyTypes";
import type { StrategyPendingNotification } from "@/components/strategy/StrategyNotificationTray.vue";
import {
  EventNotificationRegistry,
  simplifyLegacyMessage,
} from "@/eventNotifications/EventNotificationBehaviors";
import { battleOutcomeBrief } from "@/utils/battleResult";
import { messageCategoryLabel } from "@/utils/messageCategories";

let notificationSeq = 0;

export function nextNotificationId(): string {
  notificationSeq += 1;
  return `notify-${Date.now()}-${notificationSeq}`;
}

/** 募兵/征兵完成汇报：气泡与通知 tooltip 固定口语「主公…」，不用 Title。 */
export function recruitCompletionBubbleMessage(event: StrategyEvent): string {
  const brief = event.brief?.trim();
  const title = event.title?.trim();
  if (brief?.startsWith("主公")) return brief;
  if (brief && brief !== title) return brief;

  const detail = event.detailMessage?.trim() || event.message;
  const taskMatch = detail.match(/已完成\s+(.+?)\s+(募兵|征兵)任务/);
  const soldiersMatch = detail.match(/获得兵士\s+([\d,]+)\s*人/);
  if (taskMatch && soldiersMatch) {
    const soldiers = Number(soldiersMatch[1]!.replace(/,/g, ""));
    if (Number.isFinite(soldiers) && soldiers > 0) {
      return `主公，${taskMatch[1]}的${taskMatch[2]}完成了！共得 ${soldiers.toLocaleString()} 人。`;
    }
    return `主公，${taskMatch[1]}的${taskMatch[2]}任务已结束，未能募得兵士。`;
  }

  return "主公，任务已完成！";
}

export function eventBriefText(event: StrategyEvent): string {
  if (event.category === "RecruitTaskCompleted") {
    return event.title?.trim() || event.brief?.trim() || event.message;
  }
  return event.brief?.trim() || event.message;
}

export function battleBriefText(
  result: StrategyBattleResult,
  playerForceId: number,
  worldState?: StrategyWorldState,
): string {
  const outcome = battleOutcomeBrief(result, playerForceId, worldState);
  return `⚔ ${result.attackerName} vs ${result.defenderName}（${outcome}）`;
}

export function notificationFromBattle(
  result: StrategyBattleResult,
  playerForceId: number,
  worldState?: StrategyWorldState,
): StrategyPendingNotification {
  return {
    id: nextNotificationId(),
    kind: "battle",
    icon: "⚔",
    brief: battleBriefText(result, playerForceId, worldState),
    battleResult: result,
  };
}

const SETTLEMENT_CATEGORIES = new Set(["EconomyMonthly", "EconomyAnnual"]);

export function isSettlementEvent(event: StrategyEvent): boolean {
  return SETTLEMENT_CATEGORIES.has(event.category);
}

/** 是否可在消息区点击打开详情（战报、收支、战略情报等）。 */
export function eventHasDetail(event: StrategyEvent): boolean {
  if (isSettlementEvent(event)) return true;
  if (event.category === "InstantEventSummary") return true;
  if (event.category === "RecruitTaskCompleted") return true;
  if (event.category === "BattleReportArrived" && event.battleResult) return true;
  if (event.category === "StrategicReportArrived") return true;
  return false;
}

function buildNotificationContext(
  event: StrategyEvent,
  playerForceId?: number,
  worldState?: StrategyWorldState,
) {
  return {
    event,
    playerForceId,
    worldState,
    nextId: nextNotificationId,
    fromBattle: notificationFromBattle,
    briefText: eventBriefText,
    isSettlement: isSettlementEvent,
  };
}

export function notificationFromEventDetail(
  event: StrategyEvent,
  playerForceId?: number,
  worldState?: StrategyWorldState,
): StrategyPendingNotification | null {
  return EventNotificationRegistry.buildNotification(
    buildNotificationContext(event, playerForceId, worldState),
  );
}

/** 战略情报抵达后的详情文案（溃灭、占城、围城开始等）。 */
export function strategicReportDetailText(event: StrategyEvent): string {
  if (event.detailMessage?.trim()) return event.detailMessage.trim();
  if (event.detailCategory === "SiegeEncircle" || event.detailCategory === "SiegeAssault") {
    return event.message;
  }
  return event.message;
}

export function notificationFromEvent(
  event: StrategyEvent,
  playerForceId?: number,
  worldState?: StrategyWorldState,
): StrategyPendingNotification | null {
  return EventNotificationRegistry.buildNotification(
    buildNotificationContext(event, playerForceId, worldState),
  );
}

export function messengerFeedBrief(event: StrategyEvent): string {
  const label = messageCategoryLabel(event.category);
  const brief =
    event.category === "RecruitTaskCompleted"
      ? event.title?.trim() || event.brief?.trim()
      : event.brief?.trim();
  if (brief) return `[${label}] ${brief}`;
  return `[${label}] ${simplifyLegacyMessage(event)}`;
}

export type { StrategyPendingNotification };
