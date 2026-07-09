/** 与 Domain UnitDirective 对齐的展示用标签（M3-b）。 */
export type UnitDirectiveValue = "Move" | "Occupy" | "Raid" | "Support" | "Retreat";

export interface UnitDirectiveOption {
  value: UnitDirectiveValue;
  label: string;
  description: string;
}

export const UNIT_DIRECTIVE_OPTIONS: UnitDirectiveOption[] = [
  { value: "Move", label: "移动", description: "按目标推进，遇敌按默认交战" },
  { value: "Occupy", label: "占领", description: "优先占领途经据点与要地" },
  { value: "Raid", label: "劫掠", description: "袭扰敌军与补给，避免决战" },
  { value: "Support", label: "支援", description: "协同友军，巩固防线" },
  { value: "Retreat", label: "撤退", description: "保存兵力，脱离接触" },
];

export function directiveLabel(value: string | undefined | null): string {
  if (!value) return "—";
  return UNIT_DIRECTIVE_OPTIONS.find((o) => o.value === value)?.label ?? value;
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
  return `信使传递中：${directiveLabel(pending.pendingDirective)}`;
}
