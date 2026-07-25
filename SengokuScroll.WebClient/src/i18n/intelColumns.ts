import type { IntelTableColumnDef } from "@/utils/strategyIntelSystemColumns";
import { localeTick } from "@/i18n/localeSignal";
import { t } from "@/i18n/textLocalizer";

export function resolveIntelColumnLabel(col: IntelTableColumnDef): string {
  localeTick.value;
  if (col.labelKey) {
    const keyed = t(col.labelKey);
    if (keyed !== col.labelKey) return keyed;
  }

  const autoKey = `ui.intel.column.${col.prop}`;
  const auto = t(autoKey);
  if (auto !== autoKey) return auto;

  if (col.label !== undefined) return col.label;

  return col.prop;
}

export function resolveIntelTabLabel(scope: string, name: string, fallback?: string): string {
  localeTick.value;
  const key = `ui.intel.tab.${scope}.${name}`;
  const resolved = t(key);
  if (resolved !== key) return resolved;

  const masterKey = `ui.intel.masterTab.${name}`;
  const master = t(masterKey);
  if (master !== masterKey) return master;

  return fallback ?? name;
}
