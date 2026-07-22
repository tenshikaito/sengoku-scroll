import { enumLabel, t } from "@/i18n/textLocalizer";

export abstract class MessengerPayloadMissionBehavior {
  abstract readonly payloadType: string;

  abstract missionLabel(messenger: {
    payloadType: string;
    targetUnitName?: string | null;
    originStrongholdName?: string | null;
    pendingDirective?: string | null;
  }): string;
}

class PolicyChangeMissionBehavior extends MessengerPayloadMissionBehavior {
  readonly payloadType = "PolicyChange";

  missionLabel(messenger: {
    targetUnitName?: string | null;
  }): string {
    return messenger.targetUnitName
      ? t("logistics.messenger.policyTo", { name: messenger.targetUnitName })
      : t("logistics.messenger.policy");
  }
}

class BattleReportMissionBehavior extends MessengerPayloadMissionBehavior {
  readonly payloadType = "BattleReport";

  missionLabel(): string {
    return t("logistics.messenger.battleReportToLord");
  }
}

class DefaultPayloadMissionBehavior extends MessengerPayloadMissionBehavior {
  constructor(readonly payloadType: string) {
    super();
  }

  missionLabel(messenger: { payloadType: string }): string {
    return messengerPayloadLabel(messenger.payloadType);
  }
}

const MISSION_BEHAVIORS: Record<string, MessengerPayloadMissionBehavior> = {
  PolicyChange: new PolicyChangeMissionBehavior(),
  BattleReport: new BattleReportMissionBehavior(),
  FalseIntelligence: new DefaultPayloadMissionBehavior("FalseIntelligence"),
  StrategicOrder: new DefaultPayloadMissionBehavior("StrategicOrder"),
};

export function messengerPayloadLabel(payloadType: string): string {
  return enumLabel("enum.messenger.payload", payloadType, payloadType);
}

export function messengerStatusLabel(status: string): string {
  return enumLabel("enum.messenger.status", status, status);
}

export function convoyStatusLabel(status: string, isReturningToOrigin?: boolean): string {
  if (isReturningToOrigin) return t("enum.convoy.returning");
  return enumLabel("enum.convoy.status", status, status);
}

export function convoyMissionLabel(convoy: {
  isReturningToOrigin: boolean;
  targetUnitName?: string | null;
  originStrongholdName?: string | null;
  status: string;
}): string {
  if (convoy.isReturningToOrigin) {
    return convoy.originStrongholdName
      ? t("logistics.convoy.returnTo", { name: convoy.originStrongholdName })
      : t("common.return");
  }

  if (convoy.targetUnitName) {
    return t("logistics.convoy.deliverTo", { name: convoy.targetUnitName });
  }

  return convoyStatusLabel(convoy.status, convoy.isReturningToOrigin);
}

export function messengerMissionLabel(messenger: {
  payloadType: string;
  targetUnitName?: string | null;
  originStrongholdName?: string | null;
  pendingDirective?: string | null;
}): string {
  const behavior =
    MISSION_BEHAVIORS[messenger.payloadType]
    ?? new DefaultPayloadMissionBehavior(messenger.payloadType);
  return behavior.missionLabel(messenger);
}
