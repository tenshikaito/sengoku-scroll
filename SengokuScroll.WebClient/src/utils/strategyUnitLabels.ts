/** 军事单位姿态展示标签（UnitStance）。 */
export function unitStanceLabel(stance: string | undefined | null): string {
  switch (stance) {
    case "Normal":
      return "普通";
    case "Attacking":
      return "攻击中";
    case "Surrounding":
      return "包围中";
    case "Maneuver":
      return "机动";
    case "Alert":
      return "警惕";
    case "Hold":
      return "坚守";
    default:
      return stance?.trim() ? stance : "—";
  }
}

/** 军事单位状态展示标签。 */
export function unitStatusLabel(status: string | undefined | null): string {
  switch (status) {
    case "Waiting":
      return "待机";
    case "Moving":
      return "移动中";
    case "Inspiring":
      return "斗志高昂";
    case "Fearful":
      return "恐惧";
    case "Chaos":
      return "混乱";
    case "Ambushing":
      return "埋伏";
    case "BeingSurround":
      return "被包围";
    case "Standoff":
      return "对峙";
    default:
      return status?.trim() ? status : "—";
  }
}

/** 开发/简单模式：悬浮情报框显示单位状态等调试字段。 */
export function isStrategySimpleIntelMode(): boolean {
  return import.meta.env.DEV;
}
