import type { StrategyEvent } from "@/api/strategyTypes";
import type { StrategyBattleResult, StrategyWorldState } from "@/api/strategy";
import type { StrategyPendingNotification } from "@/components/strategy/StrategyNotificationTray.vue";

export interface EventNotificationContext {
  event: StrategyEvent;
  playerForceId?: number;
  worldState?: StrategyWorldState;
  nextId: () => string;
  fromBattle: (
    battle: StrategyBattleResult,
    playerForceId: number,
    worldState?: StrategyWorldState,
  ) => StrategyPendingNotification;
  briefText: (event: StrategyEvent) => string;
  isSettlement: (event: StrategyEvent) => boolean;
}

export abstract class EventNotificationBehavior {
  abstract readonly category: string;

  abstract buildNotification(ctx: EventNotificationContext): StrategyPendingNotification | null;

  matches(ctx: EventNotificationContext): boolean {
    return ctx.event.category === this.category;
  }
}

class InstantEventSummaryBehavior extends EventNotificationBehavior {
  readonly category = "InstantEventSummary";

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification {
    return {
      id: ctx.nextId(),
      kind: "message",
      icon: "⚡",
      brief: ctx.briefText(ctx.event),
      event: ctx.event,
    };
  }
}

class BattleReportArrivedBehavior extends EventNotificationBehavior {
  readonly category = "BattleReportArrived";

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification | null {
    if (!ctx.event.battleResult || ctx.playerForceId == null) return null;
    return ctx.fromBattle(ctx.event.battleResult, ctx.playerForceId, ctx.worldState);
  }
}

class SettlementEventBehavior extends EventNotificationBehavior {
  readonly category = "__settlement__";

  override matches(ctx: EventNotificationContext): boolean {
    return ctx.isSettlement(ctx.event);
  }

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification | null {
    if (!this.matches(ctx)) return null;
    return {
      id: ctx.nextId(),
      kind: "economy",
      icon: "📋",
      brief: ctx.briefText(ctx.event),
      event: ctx.event,
    };
  }
}

abstract class StrategicDetailIconBehavior {
  abstract readonly detailCategory: string;
  abstract readonly icon: string;
}

const STRATEGIC_DETAIL_ICONS: StrategicDetailIconBehavior[] = [
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "SiegeOrderStarted";
    icon = "⭕";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "SiegeEncircle";
    icon = "⭕";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "SiegeAssault";
    icon = "⚔";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "StrongholdCaptured";
    icon = "🏯";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "UnitDestroyed";
    icon = "💥";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "UnitFledToStronghold";
    icon = "🏯";
  })(), {}),
  Object.assign(new (class extends StrategicDetailIconBehavior {
    detailCategory = "UnitForcedDisband";
    icon = "💥";
  })(), {}),
];

class StrategicReportArrivedBehavior extends EventNotificationBehavior {
  readonly category = "StrategicReportArrived";

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification {
    const detail = ctx.event.detailCategory;
    const icon =
      STRATEGIC_DETAIL_ICONS.find((b) => b.detailCategory === detail)?.icon ?? "📨";
    return {
      id: ctx.nextId(),
      kind: "message",
      icon,
      brief: ctx.briefText(ctx.event),
      event: ctx.event,
    };
  }
}

class RecruitTaskCompletedBehavior extends EventNotificationBehavior {
  readonly category = "RecruitTaskCompleted";

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification {
    return {
      id: ctx.nextId(),
      kind: "message",
      icon: "📋",
      brief: ctx.briefText(ctx.event),
      event: ctx.event,
    };
  }
}

class TariffTransitChargedBehavior extends EventNotificationBehavior {
  readonly category = "TariffTransitCharged";

  buildNotification(ctx: EventNotificationContext): StrategyPendingNotification {
    return {
      id: ctx.nextId(),
      kind: "message",
      icon: "🛃",
      brief: ctx.briefText(ctx.event),
      event: ctx.event,
    };
  }
}

const ORDERED_BEHAVIORS: EventNotificationBehavior[] = [
  new InstantEventSummaryBehavior(),
  new BattleReportArrivedBehavior(),
  new SettlementEventBehavior(),
  new StrategicReportArrivedBehavior(),
  new RecruitTaskCompletedBehavior(),
  new TariffTransitChargedBehavior(),
];

export class EventNotificationRegistry {
  static buildNotification(ctx: EventNotificationContext): StrategyPendingNotification | null {
    for (const behavior of ORDERED_BEHAVIORS) {
      if (!behavior.matches(ctx)) continue;
      const result = behavior.buildNotification(ctx);
      if (result) return result;
    }
    return null;
  }
}

const LEGACY_MESSAGE_TEXT: Record<string, string> = {
  LordTributeDispatched: "贡纳运输队出发",
  LordTributeArrived: "贡纳运输队抵达",
  SupplyConvoyArrived: "补给运输队抵达",
  EconomyMonthly: "月度收支结算",
  EconomyAnnual: "年度收支结算",
  BattleReportArrived: "战报信使抵达",
  InstantEventSummary: "事件摘要",
  StrategicReportArrived: "情报信使抵达",
  TariffTransitCharged: "过境关税",
  MessengerArrived: "信使抵达",
};

export function simplifyLegacyMessage(event: StrategyEvent): string {
  const mapped = LEGACY_MESSAGE_TEXT[event.category];
  if (mapped) return mapped;
  return event.message.split(/[：:\n]/)[0]?.slice(0, 48) || event.message;
}
