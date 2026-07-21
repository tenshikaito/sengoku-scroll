export const LOCALE_STORAGE_KEY = "sengoku_scroll_locale";
export const DEFAULT_LOCALE = "zh-CN";
export const FALLBACK_LOCALE = "en-US";

const SUPPORTED_LOCALES = new Set([DEFAULT_LOCALE, FALLBACK_LOCALE]);

export function normalizeLocale(value: string | null | undefined): string {
  if (!value?.trim()) return DEFAULT_LOCALE;
  const trimmed = value.trim();
  if (SUPPORTED_LOCALES.has(trimmed)) return trimmed;
  const primary = trimmed.split("-")[0]?.toLowerCase();
  if (primary === "zh") return DEFAULT_LOCALE;
  if (primary === "en") return FALLBACK_LOCALE;
  return DEFAULT_LOCALE;
}

export function readStoredLocale(): string {
  try {
    return normalizeLocale(localStorage.getItem(LOCALE_STORAGE_KEY));
  } catch {
    return DEFAULT_LOCALE;
  }
}

export function storeLocale(locale: string): void {
  try {
    localStorage.setItem(LOCALE_STORAGE_KEY, normalizeLocale(locale));
  } catch {
    /* ignore quota / private mode */
  }
}

export function getAcceptLanguageHeader(): string {
  return readStoredLocale();
}
