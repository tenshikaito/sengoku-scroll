import { enumLabel, t } from "@/i18n/textLocalizer";

/** 与 Domain UnitDirective 对齐的展示用标签（M3-b）。 */
export type UnitDirectiveValue = "Move" | "Occupy" | "Raid" | "Support" | "Retreat";

export interface UnitDirectiveOption {
  value: UnitDirectiveValue;
  label: string;
  description: string;
}

const UNIT_DIRECTIVE_VALUES: UnitDirectiveValue[] = [
  "Move",
  "Occupy",
  "Raid",
  "Support",
  "Retreat",
];

export function getUnitDirectiveOptions(): UnitDirectiveOption[] {
  return UNIT_DIRECTIVE_VALUES.map((value) => ({
    value,
    label: t(`enum.unit.directive.${value}`),
    description: t(`enum.unit.directive.${value}.desc`),
  }));
}

export function directiveLabel(value: string | undefined | null): string {
  if (!value) return t("common.emDash");
  return enumLabel("enum.unit.directive", value, value);
}

export function pendingPolicyText(
  messengers: Array<{ targetUnitId: number; payloadType: string; status: string; pendingDirective?: string | null }>,
  unitId: number
): string | null {
  const pending = messengers.find(
    (m) =>
      m.targetUnitId === unitId &&
      m.payloadType === "PolicyChange" &&
      m.status === "Moving" &&
      m.pendingDirective
  );
  if (!pending?.pendingDirective) return null;
  return t("unit.policy.pending", {
    directive: directiveLabel(pending.pendingDirective),
  });
}

export function siegeModeLabel(value: string | undefined | null): string {
  if (!value || value === "None") return t("common.emDash");
  return enumLabel("enum.siege.mode", value, value);
}

export function unitTargetSummary(unit: {
  targetStrongholdName?: string | null;
  targetUnitName?: string | null;
  directiveTargetId?: number;
}): string {
  if (unit.targetUnitName) {
    return t("unit.target.unit", { name: unit.targetUnitName });
  }
  if (unit.targetStrongholdName) {
    return t("unit.target.stronghold", { name: unit.targetStrongholdName });
  }
  if (unit.directiveTargetId && unit.directiveTargetId > 0) {
    return t("unit.target.id", { id: unit.directiveTargetId });
  }
  return t("common.emDash");
}
