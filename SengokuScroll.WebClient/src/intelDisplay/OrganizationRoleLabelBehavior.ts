/** 组织类型 · 三级职务称谓（官府 / 商家 / 寺社）。 */
export type OrganizationRoleContext = "Government" | "Merchant" | "Religion";

export interface OrganizationRoleLabels {
  primary: string;
  secondary: string;
  tertiary: string;
}

const ORGANIZATION_ROLE_LABELS: Record<OrganizationRoleContext, OrganizationRoleLabels> = {
  Government: { primary: "当主", secondary: "领主", tertiary: "代官" },
  Merchant: { primary: "老板", secondary: "店长", tertiary: "掌柜" },
  Religion: { primary: "住持", secondary: "别当", tertiary: "执事" },
};

export function resolveOrganizationRoleContext(
  kind: string | undefined | null,
): OrganizationRoleContext | null {
  switch (kind) {
    case "Government":
      return "Government";
    case "Merchant":
      return "Merchant";
    case "Religion":
      return "Religion";
    default:
      return null;
  }
}

export function organizationRoleLabels(
  kind: string | OrganizationRoleContext,
): OrganizationRoleLabels {
  const context =
    typeof kind === "string" && kind in ORGANIZATION_ROLE_LABELS
      ? (kind as OrganizationRoleContext)
      : resolveOrganizationRoleContext(kind);
  if (!context) {
    return ORGANIZATION_ROLE_LABELS.Government;
  }
  return ORGANIZATION_ROLE_LABELS[context];
}

export function organizationRoleLabelAtIndex(
  kind: string | OrganizationRoleContext,
  index: number,
): string {
  const labels = organizationRoleLabels(kind);
  if (index <= 0) return labels.primary;
  if (index === 1) return labels.secondary;
  return labels.tertiary;
}

export function organizationPrimaryRoleLabel(kind: string): string {
  return organizationRoleLabels(kind).primary;
}
