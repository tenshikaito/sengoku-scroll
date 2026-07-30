import type { IntelTableColumnDef, MasterDataListPreset } from "@/utils/strategyIntelSystemColumns";
import { UNKNOWN_INTEL } from "@/utils/strategyIntelDisplay";
import type { IntelFieldRow } from "@/utils/strategyIntelRows";

export type IntelNavigateKind = "force" | "stronghold" | "person" | "masterData";

export interface IntelNavigateTarget {
  kind: IntelNavigateKind;
  entityId: number;
  /** masterData 跳转时的子分类 Tab。 */
  masterPreset?: MasterDataListPreset;
}

export interface IntelNavigateRequest extends IntelNavigateTarget {
  seq: number;
}

export interface IntelExcludeEntity {
  kind: IntelNavigateKind;
  entityId: number;
  masterPreset?: MasterDataListPreset;
}

export interface IntelColumnLink {
  kind: IntelNavigateKind;
  idField: string;
  masterPreset?: MasterDataListPreset;
}

const INTEL_PROP_LINK_DEFAULTS: Partial<Record<string, IntelColumnLink>> = {
  forceName: { kind: "force", idField: "forceNameLinkId" },
  hostForceName: { kind: "force", idField: "hostForceNameLinkId" },
  lordName: { kind: "person", idField: "lordNameLinkId" },
  hostLordName: { kind: "person", idField: "hostLordNameLinkId" },
  mayorName: { kind: "person", idField: "mayorNameLinkId" },
  suzerainName: { kind: "force", idField: "suzerainNameLinkId" },
  residenceName: { kind: "stronghold", idField: "residenceNameLinkId" },
  successorName: { kind: "person", idField: "successorNameLinkId" },
  strongholdName: { kind: "stronghold", idField: "strongholdNameLinkId" },
  nameWithStronghold: { kind: "stronghold", idField: "nameWithStrongholdLinkId" },
  superior: { kind: "person", idField: "superiorLinkId" },
  characterName: { kind: "person", idField: "characterNameLinkId" },
  primaryLeaderName: { kind: "person", idField: "primaryLeaderNameLinkId" },
  secondaryLeaderName: { kind: "person", idField: "secondaryLeaderNameLinkId" },
  tertiaryLeaderName: { kind: "person", idField: "tertiaryLeaderNameLinkId" },
  target: { kind: "stronghold", idField: "targetLinkId" },
  cultureName: { kind: "masterData", idField: "cultureNameLinkId", masterPreset: "cultures" },
  religionName: { kind: "masterData", idField: "religionNameLinkId", masterPreset: "religions" },
  cultureGroup: { kind: "masterData", idField: "cultureGroupId", masterPreset: "cultureGroups" },
  religionGroup: { kind: "masterData", idField: "religionGroupId", masterPreset: "religionGroups" },
  climateId: { kind: "masterData", idField: "climateId", masterPreset: "climates" },
};

export function intelNavigateTab(
  target: Pick<IntelNavigateTarget, "kind" | "masterPreset">,
): string {
  if (target.kind === "masterData") return target.masterPreset ?? "cultures";
  return target.kind;
}

export function isIntelEntityTab(tab: string): tab is "force" | "stronghold" | "person" {
  return tab === "force" || tab === "stronghold" || tab === "person";
}

export function resolveIntelColumnLink(col: IntelTableColumnDef): IntelColumnLink | null {
  if (col.link) return col.link;
  return INTEL_PROP_LINK_DEFAULTS[col.prop] ?? null;
}

export function resolveIntelLinkId(
  row: Record<string, unknown>,
  idField: string,
): number | null {
  const raw = row[idField];
  const id = Number(raw);
  if (!Number.isFinite(id) || id <= 0) return null;
  return id;
}

const INTEL_LINK_ID_FALLBACKS: Partial<Record<string, string[]>> = {
  forceNameLinkId: ["forceId"],
  strongholdNameLinkId: ["strongholdId", "id"],
  lordNameLinkId: ["lordId"],
  mayorNameLinkId: ["mayorId"],
  residenceNameLinkId: ["residenceStrongholdId"],
};

function resolveIntelLinkIdWithFallbacks(
  row: Record<string, unknown>,
  idField: string,
): number | null {
  const direct = resolveIntelLinkId(row, idField);
  if (direct != null) return direct;
  for (const fallbackField of INTEL_LINK_ID_FALLBACKS[idField] ?? []) {
    const fallback = resolveIntelLinkId(row, fallbackField);
    if (fallback != null) return fallback;
  }
  return null;
}

export function isIntelLinkableCellValue(value: unknown): boolean {
  const trimmed = String(value ?? "").trim();
  if (!trimmed || trimmed === "—" || trimmed === UNKNOWN_INTEL) return false;
  return true;
}

export function shouldExcludeIntelLink(
  target: IntelNavigateTarget,
  exclude?: IntelExcludeEntity | null,
): boolean {
  if (!exclude) return false;
  if (target.kind !== exclude.kind || target.entityId !== exclude.entityId) return false;
  if (target.kind === "masterData") {
    return (target.masterPreset ?? "") === (exclude.masterPreset ?? "");
  }
  return true;
}

export function intelMasterNavigateTarget(
  preset: MasterDataListPreset,
  entityId?: number | null,
): IntelNavigateTarget | null {
  if (entityId == null || entityId <= 0) return null;
  return { kind: "masterData", entityId, masterPreset: preset };
}

export function intelFieldRow(
  label: string,
  value: string,
  link?: IntelNavigateTarget | null,
  options?: Pick<IntelFieldRow, "dev">,
): IntelFieldRow {
  const row: IntelFieldRow = { label, value, ...options };
  if (link && link.entityId > 0 && isIntelLinkableCellValue(value)) {
    row.link = link;
  }
  return row;
}

export function intelFieldLinkFromId(
  label: string,
  value: string,
  kind: IntelNavigateKind,
  entityId?: number | null,
  options?: Pick<IntelFieldRow, "dev"> & { masterPreset?: MasterDataListPreset },
): IntelFieldRow {
  const { masterPreset, ...fieldOptions } = options ?? {};
  const link =
    kind === "masterData"
      ? intelMasterNavigateTarget(masterPreset ?? "cultures", entityId)
      : entityId != null && entityId > 0
        ? { kind, entityId }
        : null;
  return intelFieldRow(label, value, link, fieldOptions);
}

export function resolveIntelCellNavigateTarget(
  row: Record<string, unknown>,
  col: IntelTableColumnDef,
  exclude?: IntelExcludeEntity | null,
): IntelNavigateTarget | null {
  const link = resolveIntelColumnLink(col);
  if (!link) return null;
  if (!isIntelLinkableCellValue(row[col.prop])) return null;
  const entityId = resolveIntelLinkIdWithFallbacks(row, link.idField);
  if (entityId == null) return null;
  const target: IntelNavigateTarget =
    link.kind === "masterData"
      ? { kind: "masterData", entityId, masterPreset: link.masterPreset }
      : { kind: link.kind, entityId };
  if (shouldExcludeIntelLink(target, exclude)) return null;
  return target;
}
