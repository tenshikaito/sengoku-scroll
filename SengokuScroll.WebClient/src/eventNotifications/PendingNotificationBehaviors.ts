import type { StrategyBattleResult, StrategyEvent } from "@/api/strategyTypes";
import type { StrategyPendingNotification } from "@/components/strategy/StrategyNotificationTray.vue";

export interface PendingNotificationOpenContext {
  showResolvedBattle: (battle: StrategyBattleResult) => void;
  openEventDetailDialog: (event: StrategyEvent) => void;
  openSettlementDialog: (event: StrategyEvent) => void;
}

export abstract class PendingNotificationKindBehavior {
  abstract readonly kind: StrategyPendingNotification["kind"];

  /** 返回去重键；null 表示不参与去重。 */
  abstract dedupeKey(notification: StrategyPendingNotification): string | null;

  abstract handleOpen(
    notification: StrategyPendingNotification,
    ctx: PendingNotificationOpenContext,
  ): void;
}

class BattlePendingNotificationBehavior extends PendingNotificationKindBehavior {
  readonly kind = "battle";

  dedupeKey(notification: StrategyPendingNotification): string | null {
    const br = notification.battleResult;
    if (!br) return null;
    return `${br.resolutionSeed}:${br.attackerUnitId}:${br.defenderUnitId}`;
  }

  handleOpen(notification: StrategyPendingNotification, ctx: PendingNotificationOpenContext): void {
    const br = notification.battleResult;
    if (!br) return;

    if (
      (br.engagementKind === "SiegeEncircle" || br.engagementKind === "SiegeAssault")
      && br.attackerCasualties === 0
      && br.defenderCasualties === 0
      && br.logEntries?.length === 1
    ) {
      ctx.openEventDetailDialog({
        category: "StrategicReportArrived",
        message: br.logEntries[0]?.message ?? `${br.attackerName} 对 ${br.defenderName} 发动攻城。`,
        brief: notification.brief,
        detailCategory: br.engagementKind,
      } as StrategyEvent);
      return;
    }

    ctx.showResolvedBattle(br);
  }
}

class EconomyPendingNotificationBehavior extends PendingNotificationKindBehavior {
  readonly kind = "economy";

  dedupeKey(): string | null {
    return null;
  }

  handleOpen(notification: StrategyPendingNotification, ctx: PendingNotificationOpenContext): void {
    if (notification.event) {
      ctx.openSettlementDialog(notification.event);
    }
  }
}

class MessagePendingNotificationBehavior extends PendingNotificationKindBehavior {
  readonly kind = "message";

  dedupeKey(): string | null {
    return null;
  }

  handleOpen(notification: StrategyPendingNotification, ctx: PendingNotificationOpenContext): void {
    if (notification.event) {
      ctx.openEventDetailDialog(notification.event);
    }
  }
}

const PENDING_NOTIFICATION_BEHAVIORS: PendingNotificationKindBehavior[] = [
  new BattlePendingNotificationBehavior(),
  new EconomyPendingNotificationBehavior(),
  new MessagePendingNotificationBehavior(),
];

export function shouldSkipPendingNotification(
  notification: StrategyPendingNotification,
  existing: readonly StrategyPendingNotification[],
): boolean {
  const behavior = PENDING_NOTIFICATION_BEHAVIORS.find((b) => b.kind === notification.kind);
  const key = behavior?.dedupeKey(notification);
  if (!key) return false;

  return existing.some((item) => {
    if (item.kind !== notification.kind) return false;
    const itemBehavior = PENDING_NOTIFICATION_BEHAVIORS.find((b) => b.kind === item.kind);
    return itemBehavior?.dedupeKey(item) === key;
  });
}

export function handlePendingNotificationOpen(
  notification: StrategyPendingNotification,
  ctx: PendingNotificationOpenContext,
): void {
  const behavior =
    PENDING_NOTIFICATION_BEHAVIORS.find((b) => b.kind === notification.kind)
    ?? new MessagePendingNotificationBehavior();
  behavior.handleOpen(notification, ctx);
}
