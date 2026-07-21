import { enumLabel, t } from "@/i18n/textLocalizer";

/** 军事单位姿态展示标签（UnitStance）。 */
export function unitStanceLabel(stance: string | undefined | null): string {
  return enumLabel("enum.unit.stance", stance, t("common.emDash"));
}

/** 军事单位状态展示标签。 */
export function unitStatusLabel(status: string | undefined | null): string {
  return enumLabel("enum.unit.status", status, t("common.emDash"));
}

/** 开发/简单模式：悬浮情报框显示单位状态等调试字段。 */
export function isStrategySimpleIntelMode(): boolean {
  return import.meta.env.DEV;
}
