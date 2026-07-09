/** 信使载荷类型展示标签（M3-b）。 */
export function messengerPayloadLabel(payloadType: string): string {
  switch (payloadType) {
    case "PolicyChange":
      return "方针变更";
    case "BattleReport":
      return "战报";
    case "StrategicOrder":
      return "战略指令";
    case "FalseIntelligence":
      return "假情报";
    default:
      return payloadType;
  }
}

/** 信使状态展示标签。 */
export function messengerStatusLabel(status: string): string {
  switch (status) {
    case "Moving":
      return "在途";
    case "Arrived":
      return "已抵达";
    default:
      return status;
  }
}

/** 运输队状态展示标签。 */
export function convoyStatusLabel(status: string, isReturningToOrigin?: boolean): string {
  if (isReturningToOrigin) return "返程中";
  switch (status) {
    case "Moving":
      return "运输中";
    case "Waiting":
      return "待命";
    case "Arrived":
      return "已抵达";
    case "Deceived":
      return "迷惑中";
    default:
      return status;
  }
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
      ? `返程→${convoy.originStrongholdName}`
      : "返程";
  }

  if (convoy.targetUnitName) {
    return `输送→${convoy.targetUnitName}`;
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
        ? `传达方针→${messenger.targetUnitName}`
        : "传达方针";
    case "BattleReport":
      return "战报→当主";
    case "FalseIntelligence":
      return "假情报";
    case "StrategicOrder":
      return "战略指令";
    default:
      return messengerPayloadLabel(messenger.payloadType);
  }
}
