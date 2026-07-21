import type { StrategyWorldState, StrategyLoadRequest } from "./strategyTypes";
import type {
  StrategyBattlePreview,
  StrategyInstantBattleResponse,
  StrategyPathPreview,
  MapPoint,
} from "./strategyTypes";
import {
  deriveBattleResult,
  isValidBattleResult,
  normalizeBattleResult,
} from "@/utils/battleResult";
import { normalizeStrategyMapMaster } from "@/utils/normalizeStrategyMapMaster";
import { buildMiniKantoMapMaster } from "@/utils/strategyMapDefaults";
import { normalizeStrategyWorldState } from "@/utils/normalizeStrategyWorldState";
import { normalizeStrategyEvent } from "@/utils/normalizeStrategyEvent";
import {
  getApiMode,
  recordDiagnostic,
  resolveRequestUrl,
  STRATEGY_API_PREFIX,
  type StrategyApiMode,
} from "./strategyDiagnostics";
import {
  mockAdvanceDay,
  mockExecuteInstantBattle,
  mockGetState,
  mockLoadScenario,
  mockMoveUnit,
  mockPreviewBattle,
  mockPreviewUnitPath,
  mockSetUnitDirective,
  mockOrderUnitAttack,
} from "./strategyMock";

const STRATEGY_SAVE_STORAGE_KEY = "sengoku_scroll_strategy_save_v1";

async function fetchLive<T>(
  method: string,
  pathSuffix: string,
  body?: unknown
): Promise<T> {
  const path = `${STRATEGY_API_PREFIX}${pathSuffix}`;
  const fullUrl = resolveRequestUrl(path);

  const headers: HeadersInit = { "Content-Type": "application/json" };
  const token = localStorage.getItem("token");
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const requestInit: RequestInit = { method, headers };
  if (body !== undefined) requestInit.body = JSON.stringify(body);

  let response: Response;
  try {
    response = await fetch(path, requestInit);
  } catch (err) {
    const message = err instanceof Error ? err.message : "NetworkError";
    recordDiagnostic({
      method,
      path,
      fullUrl,
      pageOrigin: window.location.origin,
      source: "live",
      ok: false,
      error: message,
    });
    throw new Error(`网络错误 [${method} ${fullUrl}]：${message}`);
  }

  if (!response.ok) {
    let detail = response.statusText;
    try {
      const errBody = await response.json();
      if (errBody?.errorCode) detail = String(errBody.errorCode);
      else if (errBody?.code) detail = String(errBody.code);
    } catch {
      /* ignore */
    }

    recordDiagnostic({
      method,
      path,
      fullUrl,
      pageOrigin: window.location.origin,
      source: "live",
      ok: false,
      status: response.status,
      error: detail,
    });
    throw new Error(`HTTP ${response.status} [${method} ${fullUrl}]：${detail}`);
  }

  const data = (await response.json()) as T;
  recordDiagnostic({
    method,
    path,
    fullUrl,
    pageOrigin: window.location.origin,
    source: "live",
    ok: true,
    status: response.status,
  });
  return data;
}

function isWorldState(value: unknown): value is StrategyWorldState {
  if (!value || typeof value !== "object") return false;
  const obj = value as Record<string, unknown>;
  const map = (obj.map ?? obj.Map) as Record<string, unknown> | undefined;
  return !!map && typeof map.width === "number" && typeof map.height === "number";
}

/** 兼容 { state, result } 包装与旧版仅返回世界状态的 instant-battle 响应。 */
export function normalizeInstantBattleResponse(
  raw: unknown,
  fallback?: {
    preview: StrategyBattlePreview;
    attackerId: number;
    stateBefore: StrategyWorldState;
  }
): StrategyInstantBattleResponse {
  if (!raw || typeof raw !== "object") {
    throw new Error("instant-battle 响应为空");
  }

  const payload = raw as Record<string, unknown>;
  const wrappedState = payload.state ?? payload.State;
  const wrappedResult = payload.result ?? payload.Result;

  if (isWorldState(wrappedState)) {
    const state = normalizeStrategyWorldState(wrappedState);
    let result = wrappedResult ? normalizeBattleResult(wrappedResult) : null;

    if (!result || !isValidBattleResult(result)) {
      if (!fallback) {
        throw new Error("instant-battle 响应缺少有效的 result，请重启 WebApi");
      }
      result = deriveBattleResult(
        fallback.preview,
        fallback.attackerId,
        fallback.stateBefore,
        state
      );
    }

    return { state, result };
  }

  if (isWorldState(payload)) {
    const state = normalizeStrategyWorldState(payload);
    if (!fallback) {
      throw new Error("instant-battle 响应缺少 result 包装，请重启 WebApi");
    }
    return {
      state,
      result: deriveBattleResult(
        fallback.preview,
        fallback.attackerId,
        fallback.stateBefore,
        state
      ),
    };
  }

  throw new Error("instant-battle 响应缺少有效的 state");
}

function runMock<T>(
  method: string,
  pathSuffix: string,
  fn: () => T
): T {
  const path = `${STRATEGY_API_PREFIX}${pathSuffix}`;
  const fullUrl = resolveRequestUrl(path);

  try {
    const data = fn();
    recordDiagnostic({
      method,
      path,
      fullUrl,
      pageOrigin: window.location.origin,
      source: "mock",
      ok: true,
      status: 200,
    });
    return data;
  } catch (err) {
    const message = err instanceof Error ? err.message : "MockError";
    recordDiagnostic({
      method,
      path,
      fullUrl,
      pageOrigin: window.location.origin,
      source: "mock",
      ok: false,
      error: message,
    });
    throw err;
  }
}

async function request<T>(
  method: string,
  pathSuffix: string,
  live: () => Promise<T>,
  mock: () => T
): Promise<T> {
  const mode = getApiMode();

  if (mode === "mock") {
    return runMock(method, pathSuffix, mock);
  }

  if (mode === "live") {
    return live();
  }

  // auto：先连真实 API，失败则回退 Mock
  try {
    return await live();
  } catch {
    console.warn(`[Strategy API] Live 失败，回退 Mock：${method} ${STRATEGY_API_PREFIX}${pathSuffix}`);
    return runMock(method, pathSuffix, mock);
  }
}

export const loadScenario = (loadRequest: StrategyLoadRequest | string) => {
  const body =
    typeof loadRequest === "string"
      ? { scenarioId: loadRequest }
      : {
          scenarioId: loadRequest.scenarioId,
          difficulty: loadRequest.difficulty,
          customStartOptions:
            loadRequest.difficulty === "Custom" ? loadRequest.customStartOptions : undefined,
        };

  return request(
    "POST",
    "/load",
    () => fetchLive<unknown>("POST", "/load", body).then(normalizeStrategyWorldState),
    () =>
      normalizeStrategyWorldState(
        mockLoadScenario(typeof loadRequest === "string" ? loadRequest : loadRequest.scenarioId)
      )
  );
};

export const getStrategyState = () =>
  request(
    "GET",
    "/state",
    () => fetchLive<unknown>("GET", "/state").then(normalizeStrategyWorldState),
    () => normalizeStrategyWorldState(mockGetState())
  );

export const getStrategyMapMaster = () =>
  request(
    "GET",
    "/map",
    () => fetchLive<unknown>("GET", "/map").then(normalizeStrategyMapMaster),
    () => buildMiniKantoMapMaster()
  );

export const orderUnitAttack = (
  unitId: number,
  x: number,
  y: number
) =>
  request(
    "POST",
    `/units/${unitId}/attack-order`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/attack-order`, { x, y }).then(
        normalizeStrategyWorldState
      ),
    () => normalizeStrategyWorldState(mockOrderUnitAttack(unitId, x, y))
  );

export const orderUnitSiege = (
  unitId: number,
  strongholdId: number,
  mode: "Assault" | "Encircle"
) =>
  request(
    "POST",
    `/units/${unitId}/siege-order`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/siege-order`, {
        strongholdId,
        mode,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持 siege-order");
    }
  );

export const mergeUnits = (sourceUnitId: number, targetUnitId: number) =>
  request(
    "POST",
    `/units/${sourceUnitId}/merge`,
    () =>
      fetchLive<unknown>("POST", `/units/${sourceUnitId}/merge`, { targetUnitId }).then(
        normalizeStrategyWorldState
      ),
    () => {
      throw new Error("Mock 模式不支持 merge");
    }
  );

export const splitUnit = (
  unitId: number,
  subUnitIds: number[],
  spawnX: number,
  spawnY: number,
  name?: string
) =>
  request(
    "POST",
    `/units/${unitId}/split`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/split`, {
        subUnitIds,
        spawnX,
        spawnY,
        name,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持 split");
    }
  );

export const deployFromStronghold = (
  strongholdId: number,
  payload: import("./strategyTypes").StrategyDeployFromStrongholdRequest
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/deploy`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/deploy`, payload).then(
        normalizeStrategyWorldState
      ),
    () => {
      throw new Error("Mock 模式不支持 deploy");
    }
  );

export const moveUnit = (
  unitId: number,
  x: number,
  y: number,
  via?: MapPoint[]
) =>
  request(
    "POST",
    `/units/${unitId}/move`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/move`, {
        x,
        y,
        via: via?.map((p) => ({ x: p.x, y: p.y })),
      }).then(normalizeStrategyWorldState),
    () => normalizeStrategyWorldState(mockMoveUnit(unitId, x, y, via))
  );

export const previewUnitPath = (
  unitId: number,
  x: number,
  y: number,
  options?: { from?: MapPoint; via?: MapPoint[] }
) => {
  const body = {
    x,
    y,
    fromX: options?.from?.x,
    fromY: options?.from?.y,
    via: options?.via?.map((p) => ({ x: p.x, y: p.y })),
  };

  if (import.meta.env.DEV) {
    console.debug("[MovePath] previewUnitPath.body", { unitId, body });
  }

  return request(
    "POST",
    `/units/${unitId}/preview-path`,
    () => fetchLive<StrategyPathPreview>("POST", `/units/${unitId}/preview-path`, body),
    () => mockPreviewUnitPath(unitId, x, y, options)
  );
};

export const previewBattle = (unitId: number, x: number, y: number) =>
  request(
    "POST",
    `/units/${unitId}/preview-battle`,
    () => fetchLive<StrategyBattlePreview>("POST", `/units/${unitId}/preview-battle`, { x, y }),
    () => mockPreviewBattle(unitId, x, y)
  );

export const executeInstantBattle = async (
  unitId: number,
  x: number,
  y: number,
  fallback?: {
    preview: StrategyBattlePreview;
    attackerId: number;
    stateBefore: StrategyWorldState;
  }
) => {
  const raw = await request<unknown>(
    "POST",
    `/units/${unitId}/instant-battle`,
    () => fetchLive<unknown>("POST", `/units/${unitId}/instant-battle`, { x, y }),
    () => mockExecuteInstantBattle(unitId, x, y)
  );
  return normalizeInstantBattleResponse(raw, fallback);
};

export const setUnitDirective = (unitId: number, directive: string) =>
  request(
    "POST",
    `/units/${unitId}/directive`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/directive`, {
        directive,
      }).then((raw) => {
        const payload = raw as Record<string, unknown>;
        return {
          state: normalizeStrategyWorldState(payload.state ?? payload.State),
          outcome: String(payload.outcome ?? payload.Outcome ?? ""),
        };
      }),
    () => {
      const raw = mockSetUnitDirective(unitId, directive);
      return {
        state: normalizeStrategyWorldState(raw.state),
        outcome: raw.outcome,
      };
    }
  );

export const advanceDay = () =>
  request(
    "POST",
    "/advance-day",
    () =>
      fetchLive<unknown>("POST", "/advance-day").then((raw) => {
        const payload = raw as Record<string, unknown>;
        const battlesRaw = payload.resolvedBattles ?? payload.ResolvedBattles;
        const eventsRaw = payload.events ?? payload.Events;
        return {
          state: normalizeStrategyWorldState(payload.state ?? payload.State),
          resolvedBattles: Array.isArray(battlesRaw)
            ? battlesRaw.map((b) => normalizeBattleResult(b))
            : [],
          events: Array.isArray(eventsRaw)
            ? eventsRaw.map((e) => normalizeStrategyEvent(e))
            : [],
        };
      }),
    () => {
      const raw = mockAdvanceDay();
      return {
        state: normalizeStrategyWorldState(raw.state),
        resolvedBattles: raw.resolvedBattles,
        events: raw.events,
      };
    }
  );

export interface StrategyMovementTraceEntry {
  sequence: number;
  at: string;
  phase: string;
  message: string;
  unitId?: number;
  fromX?: number;
  fromY?: number;
  toX?: number;
  toY?: number;
  detail?: string;
}

export interface StrategyAiDecisionTraceEntry {
  sequence: number;
  at: string;
  /** Directive | Action | Skip */
  phase: string;
  code: string;
  message: string;
  unitId: number;
  unitName: string;
  forceId: number;
  actedOrChanged: boolean;
  fromDirective?: string | null;
  toDirective?: string | null;
  currentDirective?: string | null;
  targetUnitId?: number | null;
  targetX?: number | null;
  targetY?: number | null;
    /** 思维链步骤 */
    steps: string[];
    targetStrongholdId?: number | null;
    stance?: string | null;
    siegeMode?: string | null;
    unitStatus?: string | null;
}

export const getMovementTrace = () =>
  request(
    "GET",
    "/debug/movement-trace",
    () => fetchLive<StrategyMovementTraceEntry[]>("GET", "/debug/movement-trace"),
    () => [] as StrategyMovementTraceEntry[]
  );

export const getAiDecisionTrace = () =>
  request(
    "GET",
    "/debug/ai-decision-trace",
    () => fetchLive<StrategyAiDecisionTraceEntry[]>("GET", "/debug/ai-decision-trace"),
    () => [] as StrategyAiDecisionTraceEntry[]
  );

/** 导出当前仿真 JSON 存档（Live）；Mock 时写入 localStorage 快照。 */
export async function exportStrategySave(): Promise<string> {
  const mode = getApiMode();
  if (mode === "mock") {
    const snapshot = normalizeStrategyWorldState(mockGetState());
    const json = JSON.stringify({ kind: "mock-state", savedAt: new Date().toISOString(), state: snapshot });
    localStorage.setItem(STRATEGY_SAVE_STORAGE_KEY, json);
    return json;
  }

  const payload = await fetchLive<{ json?: string; Json?: string }>("GET", "/save");
  const json = payload.json ?? payload.Json ?? "";
  if (json) localStorage.setItem(STRATEGY_SAVE_STORAGE_KEY, json);
  return json;
}

/** 从 JSON 恢复存档；优先使用参数，否则读 localStorage。 */
export const restoreStrategySave = (json?: string) =>
  request(
    "POST",
    "/restore-save",
    async () => {
      const raw = json ?? localStorage.getItem(STRATEGY_SAVE_STORAGE_KEY);
      if (!raw) throw new Error("无存档");

      const parsed = JSON.parse(raw) as Record<string, unknown>;
      if (parsed.kind === "mock-state" && parsed.state) {
        return normalizeStrategyWorldState(parsed.state);
      }

      return normalizeStrategyWorldState(
        await fetchLive<unknown>("POST", "/restore-save", { json: raw })
      );
    },
    () => {
      const raw = json ?? localStorage.getItem(STRATEGY_SAVE_STORAGE_KEY);
      if (!raw) throw new Error("无存档");
      const parsed = JSON.parse(raw) as Record<string, unknown>;
      if (parsed.kind === "mock-state" && parsed.state) {
        return normalizeStrategyWorldState(parsed.state);
      }
      throw new Error("Mock 模式仅支持 mock-state 存档");
    }
  );

export function hasLocalStrategySave(): boolean {
  return Boolean(localStorage.getItem(STRATEGY_SAVE_STORAGE_KEY));
}

export type { StrategyApiMode };
