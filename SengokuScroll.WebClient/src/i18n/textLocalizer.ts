import enUS from "@/locales/en-US.json";
import zhCN from "@/locales/zh-CN.json";
import { bumpLocaleTick, localeTick } from "@/i18n/localeSignal";
import {
  DEFAULT_LOCALE,
  FALLBACK_LOCALE,
  readStoredLocale,
  storeLocale,
} from "@/i18n/localePreference";

type MessageCatalog = Record<string, string>;

const catalogs: Record<string, MessageCatalog> = {
  [DEFAULT_LOCALE]: zhCN,
  [FALLBACK_LOCALE]: enUS,
};

let currentLocale = readStoredLocale();
let localeEpoch = 0;

export function getLocaleEpoch(): number {
  return localeEpoch;
}

export function getLocale(): string {
  return currentLocale;
}

export function setLocale(locale: string): void {
  const next = locale in catalogs ? locale : DEFAULT_LOCALE;
  if (next === currentLocale) return;
  currentLocale = next;
  storeLocale(next);
  localeEpoch += 1;
  bumpLocaleTick();
}

function buildFallbackChain(locale: string): string[] {
  const chain: string[] = [locale];
  if (!chain.includes(DEFAULT_LOCALE)) chain.push(DEFAULT_LOCALE);
  if (!chain.includes(FALLBACK_LOCALE)) chain.push(FALLBACK_LOCALE);
  return chain;
}

function tryResolve(key: string, locale: string): string | null {
  for (const culture of buildFallbackChain(locale)) {
    const value = catalogs[culture]?.[key];
    if (value != null) return value;
  }
  return null;
}

function formatTemplate(
  template: string,
  params?: Record<string, string | number>
): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (_, name: string) => {
    const value = params[name];
    return value == null ? `{${name}}` : String(value);
  });
}

/** 解析本地化 key；未命中时返回 key 本身（与后端 TextLocalizer 行为一致）。 */
export function t(
  key: string,
  params?: Record<string, string | number>
): string {
  localeTick.value;
  const resolved = tryResolve(key, currentLocale);
  if (resolved == null) return key;
  return formatTemplate(resolved, params);
}

/** 枚举值 → 展示名；key 形如 `{prefix}.{Value}`。 */
export function enumLabel(
  prefix: string,
  value: string | null | undefined,
  fallback = "—"
): string {
  localeTick.value;
  if (!value?.trim()) return fallback;
  const key = `${prefix}.${value}`;
  const resolved = tryResolve(key, currentLocale);
  return resolved ?? value;
}
