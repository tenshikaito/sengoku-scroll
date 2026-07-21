import { enumLabel, t } from "@/i18n/textLocalizer";

/** 信使载荷类型展示标签（M3-b）。 */
export function messengerPayloadLabel(payloadType: string): string {
  return enumLabel("enum.messenger.payload", payloadType, payloadType);
}

/** 信使状态展示标签。 */
export function messengerStatusLabel(status: string): string {
  return enumLabel("enum.messenger.status", status, status);
}

/** 运输队状态展示标签。 */
export function convoyStatusLabel(status: string, isReturningToOrigin?: boolean): string {
  if (isReturningToOrigin) return t("enum.convoy.returning");
  return enumLabel("enum.convoy.status", status, status);
}

/** 运输队任务摘要（对应兵队「补给/方针」栏位）。 */
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

/** 信使任务摘要。 */
export function messengerMissionLabel(messenger: {
  payloadType: string;
  targetUnitName?: string | null;
  originStrongholdName?: string | null;
  pendingDirective?: string | null;
}): string {
  switch (messenger.payloadType) {
    case "PolicyChange":
      return messenger.targetUnitName
        ? t("logistics.messenger.policyTo", { name: messenger.targetUnitName })
        : t("logistics.messenger.policy");
    case "BattleReport":
      return t("logistics.messenger.battleReportToLord");
    case "FalseIntelligence":
      return messengerPayloadLabel(messenger.payloadType);
    case "StrategicOrder":
      return messengerPayloadLabel(messenger.payloadType);
    default:
      return messengerPayloadLabel(messenger.payloadType);
  }
}
