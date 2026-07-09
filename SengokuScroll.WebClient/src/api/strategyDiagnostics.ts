import { reactive } from "vue";

export type StrategyApiMode = "live" | "mock" | "auto";

export type StrategyRequestSource = "live" | "mock";

export interface StrategyApiDiagnostic {
  method: string;
  /** 传给 fetch 的路径，如 /api/strategy/load */
  path: string;
  /** 浏览器实际请求的完整 URL */
  fullUrl: string;
  /** 页面 origin，便于判断端口/协议 */
  pageOrigin: string;
  source: StrategyRequestSource;
  ok: boolean;
  status?: number;
  error?: string;
  at: string;
}

export const strategyApiDiagnostics = reactive({
  mode: (import.meta.env.VITE_STRATEGY_API_MODE as StrategyApiMode | undefined) ??
    (import.meta.env.DEV ? "auto" : "live"),
  last: null as StrategyApiDiagnostic | null,
  usingMockFallback: false,
});

export function resolveRequestUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path;
  const base = typeof window !== "undefined" ? window.location.origin : "http://localhost:5173";
  return new URL(path, base).href;
}

export function recordDiagnostic(partial: Omit<StrategyApiDiagnostic, "at">) {
  strategyApiDiagnostics.last = {
    ...partial,
    at: new Date().toLocaleTimeString(),
  };
  strategyApiDiagnostics.usingMockFallback =
    partial.source === "mock" && strategyApiDiagnostics.mode === "auto";
}

export function getApiMode(): StrategyApiMode {
  return strategyApiDiagnostics.mode;
}

export function setApiMode(mode: StrategyApiMode) {
  strategyApiDiagnostics.mode = mode;
  strategyApiDiagnostics.usingMockFallback = false;
}

export const STRATEGY_API_PREFIX =
  import.meta.env.VITE_STRATEGY_API_BASE ?? "/api/strategy";
