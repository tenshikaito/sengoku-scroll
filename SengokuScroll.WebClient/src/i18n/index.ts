import type { App } from "vue";
import { computed } from "vue";
import {
  enumLabel as enumLabelInternal,
  getLocale as readLocale,
  setLocale as setLocaleInternal,
  t as translate,
} from "@/i18n/textLocalizer";
import { localeTick } from "@/i18n/localeSignal";

export {
  DEFAULT_LOCALE,
  FALLBACK_LOCALE,
  getAcceptLanguageHeader,
  readStoredLocale,
} from "@/i18n/localePreference";
export { resolveIntelColumnLabel, resolveIntelTabLabel } from "@/i18n/intelColumns";

export function getLocale(): string {
  localeTick.value;
  return readLocale();
}

export function setLocale(locale: string): void {
  setLocaleInternal(locale);
}

export function t(key: string, params?: Record<string, string | number>): string {
  localeTick.value;
  return translate(key, params);
}

export function enumLabel(
  prefix: string,
  value: string | null | undefined,
  fallback?: string
): string {
  localeTick.value;
  return enumLabelInternal(prefix, value, fallback);
}

/** Vue 组合式 API：locale 变更时触发重渲染。 */
export function useI18n() {
  const locale = computed(() => {
    localeTick.value;
    return readLocale();
  });

  return {
    locale,
    t,
    enumLabel,
    setLocale,
  };
}

export function installI18n(app: App): void {
  app.config.globalProperties.$t = t;
}

declare module "vue" {
  interface ComponentCustomProperties {
    $t: typeof t;
  }
}
