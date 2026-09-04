import type { StrategyWorldState, StrategyLoadRequest, StrategySaveSlotSummary } from "./strategyTypes";
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
import { getAcceptLanguageHeader } from "@/i18n/localePreference";
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
const STRATEGY_SAVE_SLOT_KEY_PREFIX = "sengoku_scroll_strategy_save_slot_";
const STRATEGY_SAVE_SLOT_META_PREFIX = "sengoku_scroll_strategy_save_slot_meta_";
const SAVE_SLOT_COUNT = 10;

async function fetchLive<T>(
  method: string,
  pathSuffix: string,
  body?: unknown
): Promise<T> {
  const path = `${STRATEGY_API_PREFIX}${pathSuffix}`;
  const fullUrl = resolveRequestUrl(path);

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    "Accept-Language": getAcceptLanguageHeader(),
  };
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
      if (errBody?.message) detail = String(errBody.message);
      else if (errBody?.errorCode) detail = String(errBody.errorCode);
      else if (errBody?.code) detail = String(errBody.code);
    } catch {
      if (response.status === 500 || response.status === 502) {
        detail =
          "策略 API（5100）可能未启动。请运行：dotnet run --project SengokuScroll.WebApi --launch-profile http";
      }
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
          customStartOptions: loadRequest.customStartOptions,
          allForcesAiControlled: loadRequest.allForcesAiControlled,
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

export const enterUnitStronghold = (unitId: number, strongholdId: number) =>
  request(
    "POST",
    `/units/${unitId}/enter-stronghold/${strongholdId}`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/enter-stronghold/${strongholdId}`, {}).then(
        normalizeStrategyWorldState,
      ),
    () => {
      throw new Error("Mock 模式不支持单位入城");
    },
  );

export const exitUnitStronghold = (unitId: number, strongholdId: number) =>
  request(
    "POST",
    `/units/${unitId}/exit-stronghold/${strongholdId}`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/exit-stronghold/${strongholdId}`, {}).then(
        normalizeStrategyWorldState,
      ),
    () => {
      throw new Error("Mock 模式不支持单位出城");
    },
  );

export const disbandUnitOrganizationally = (unitId: number) =>
  request(
    "POST",
    `/units/${unitId}/disband`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/disband`, {}).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持建制解散");
    },
  );

export const createMerchantShop = (strongholdId: number, houseName?: string) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/shops`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/shops`, { houseName }).then(
        normalizeStrategyWorldState,
      ),
    () => {
      throw new Error("Mock 模式不支持创立商店");
    },
  );

export const unitSmashBuyFood = (
  unitId: number,
  payload: { maxPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/units/${unitId}/trade/smash-buy-food`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/trade/smash-buy-food`, {
        maxPriceMoneyPerGo: payload.maxPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持贸易购粮");
    },
  );

export const unitSmashSellFood = (
  unitId: number,
  payload: { minPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/units/${unitId}/trade/smash-sell-food`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/trade/smash-sell-food`, {
        minPriceMoneyPerGo: payload.minPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持贸易卖粮");
    },
  );

export const unitSmashBuyHorse = (
  unitId: number,
  payload: { maxPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/units/${unitId}/trade/smash-buy-horse`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/trade/smash-buy-horse`, {
        maxPriceMoneyPerGo: payload.maxPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持贸易购马");
    },
  );

export const unitSmashSellHorse = (
  unitId: number,
  payload: { minPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/units/${unitId}/trade/smash-sell-horse`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/trade/smash-sell-horse`, {
        minPriceMoneyPerGo: payload.minPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持贸易卖马");
    },
  );

export interface StrategyMarketDepthLevel {
  priceMoneyPerGo: number;
  quantityGo: number;
}

export interface StrategyMarketDailyBar {
  year: number;
  month: number;
  day: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volumeGo: number;
  turnoverMoney: number;
}

export interface StrategyMarketSnapshot {
  strongholdId: number;
  strongholdName: string;
  commodity: string;
  isOpen: boolean;
  lastClosePriceMoneyPerGo: number;
  /** 盘口中线报价（由挂单簿推导） */
  sessionPriceMoneyPerGo: number;
  /** Empty | Bid | Ask | Both */
  bookQuoteSide: string;
  bestBidPriceMoneyPerGo: number;
  bestAskPriceMoneyPerGo: number;
  closeLevelQuantityGo: number;
  bidLevels: StrategyMarketDepthLevel[];
  askLevels: StrategyMarketDepthLevel[];
  dailyBars: StrategyMarketDailyBar[];
  playerOpenOrders: StrategyMarketOpenOrder[];
}

export interface StrategyMarketOpenOrder {
  id: number;
  side: string;
  priceMoneyPerGo: number;
  quantityGo: number;
  originalQuantityGo: number;
  filledQuantityGo: number;
  /** Open | Partial */
  fillStatus: string;
  createdYear: number;
  createdMonth: number;
  createdDay: number;
}

function normalizeMarketSnapshot(raw: unknown): StrategyMarketSnapshot {
  const row = (raw ?? {}) as Record<string, unknown>;
  const num = (obj: Record<string, unknown>, camel: string, pascal: string, fallback = 0) => {
    const v = obj[camel] ?? obj[pascal];
    const n = Number(v);
    return Number.isFinite(n) ? n : fallback;
  };
  const str = (obj: Record<string, unknown>, camel: string, pascal: string, fallback: string) => {
    const v = obj[camel] ?? obj[pascal];
    return typeof v === "string" && v.length > 0 ? v : fallback;
  };
  const mapLevels = (levelsRaw: unknown): StrategyMarketDepthLevel[] => {
    if (!Array.isArray(levelsRaw)) return [];
    return levelsRaw.map((item) => {
      const l = item as Record<string, unknown>;
      return {
        priceMoneyPerGo: num(l, "priceMoneyPerGo", "PriceMoneyPerGo"),
        quantityGo: num(l, "quantityGo", "QuantityGo"),
      };
    });
  };
  const mapBars = (barsRaw: unknown): StrategyMarketDailyBar[] => {
    if (!Array.isArray(barsRaw)) return [];
    return barsRaw.map((item) => {
      const b = item as Record<string, unknown>;
      return {
        year: num(b, "year", "Year"),
        month: num(b, "month", "Month"),
        day: num(b, "day", "Day"),
        open: num(b, "open", "Open"),
        high: num(b, "high", "High"),
        low: num(b, "low", "Low"),
        close: num(b, "close", "Close"),
        volumeGo: num(b, "volumeGo", "VolumeGo"),
        turnoverMoney: num(b, "turnoverMoney", "TurnoverMoney"),
      };
    });
  };
  const mapOpenOrders = (ordersRaw: unknown): StrategyMarketOpenOrder[] => {
    if (!Array.isArray(ordersRaw)) return [];
    return ordersRaw.map((item) => {
      const o = item as Record<string, unknown>;
      return {
        id: num(o, "id", "Id"),
        side: str(o, "side", "Side", "Buy"),
        priceMoneyPerGo: num(o, "priceMoneyPerGo", "PriceMoneyPerGo"),
        quantityGo: num(o, "quantityGo", "QuantityGo"),
        originalQuantityGo: num(o, "originalQuantityGo", "OriginalQuantityGo"),
        filledQuantityGo: num(o, "filledQuantityGo", "FilledQuantityGo"),
        fillStatus: str(o, "fillStatus", "FillStatus", "Open"),
        createdYear: num(o, "createdYear", "CreatedYear"),
        createdMonth: num(o, "createdMonth", "CreatedMonth"),
        createdDay: num(o, "createdDay", "CreatedDay"),
      };
    });
  };
  return {
    strongholdId: num(row, "strongholdId", "StrongholdId"),
    strongholdName: str(row, "strongholdName", "StrongholdName", "市场"),
    commodity: str(row, "commodity", "Commodity", "Food"),
    isOpen: row.isOpen === true || row.IsOpen === true,
    lastClosePriceMoneyPerGo: num(row, "lastClosePriceMoneyPerGo", "LastClosePriceMoneyPerGo"),
    sessionPriceMoneyPerGo: num(
      row,
      "sessionPriceMoneyPerGo",
      "SessionPriceMoneyPerGo",
      num(row, "lastClosePriceMoneyPerGo", "LastClosePriceMoneyPerGo"),
    ),
    bookQuoteSide: str(row, "bookQuoteSide", "BookQuoteSide", "Empty"),
    bestBidPriceMoneyPerGo: num(row, "bestBidPriceMoneyPerGo", "BestBidPriceMoneyPerGo"),
    bestAskPriceMoneyPerGo: num(row, "bestAskPriceMoneyPerGo", "BestAskPriceMoneyPerGo"),
    closeLevelQuantityGo: num(row, "closeLevelQuantityGo", "CloseLevelQuantityGo"),
    bidLevels: mapLevels(row.bidLevels ?? row.BidLevels),
    askLevels: mapLevels(row.askLevels ?? row.AskLevels),
    dailyBars: mapBars(row.dailyBars ?? row.DailyBars),
    playerOpenOrders: mapOpenOrders(row.playerOpenOrders ?? row.PlayerOpenOrders),
  };
}

export const fetchMarketSnapshot = (strongholdId: number, commodity: "Food" | "Horse" = "Food") =>
  request(
    "GET",
    `/strongholds/${strongholdId}/market?commodity=${commodity}`,
    () =>
      fetchLive<unknown>("GET", `/strongholds/${strongholdId}/market?commodity=${commodity}`, undefined).then(
        normalizeMarketSnapshot,
      ),
    () => {
      throw new Error("Mock 模式不支持市场快照");
    },
  );

export const strongholdLordSmashBuyFood = (
  strongholdId: number,
  payload: { maxPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/trade/smash-buy-food`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/trade/smash-buy-food`, {
        maxPriceMoneyPerGo: payload.maxPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持官府购粮");
    },
  );

export const strongholdLordSmashSellFood = (
  strongholdId: number,
  payload: { minPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/trade/smash-sell-food`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/trade/smash-sell-food`, {
        minPriceMoneyPerGo: payload.minPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持官府卖粮");
    },
  );

export const strongholdLordSmashBuyHorse = (
  strongholdId: number,
  payload: { maxPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/trade/smash-buy-horse`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/trade/smash-buy-horse`, {
        maxPriceMoneyPerGo: payload.maxPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持官府购马");
    },
  );

export const strongholdLordSmashSellHorse = (
  strongholdId: number,
  payload: { minPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/trade/smash-sell-horse`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/trade/smash-sell-horse`, {
        minPriceMoneyPerGo: payload.minPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持官府卖马");
    },
  );

export const strongholdLordCancelMarketOrder = (
  strongholdId: number,
  payload: { orderId: number; commodity?: "Food" | "Horse" },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/trade/cancel-order`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/trade/cancel-order`, {
        orderId: payload.orderId,
        commodity: payload.commodity ?? "Food",
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持撤单");
    },
  );

export type UnitTradePolicyValue = "None" | "WaitBuyFood" | "WaitSellFood";

export const setUnitTradePolicy = (
  unitId: number,
  payload: { policy: UnitTradePolicyValue; limitPriceMoneyPerGo: number; quantityGo?: number },
) =>
  request(
    "POST",
    `/units/${unitId}/trade/policy`,
    () =>
      fetchLive<unknown>("POST", `/units/${unitId}/trade/policy`, {
        policy: payload.policy,
        limitPriceMoneyPerGo: payload.limitPriceMoneyPerGo,
        quantityGo: payload.quantityGo ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持贸易策略");
    },
  );

export const recordEspionageIntel = (payload: {
  targetKind: "Stronghold" | "Unit";
  targetId: number;
  scope?: "Military" | "Domestic" | "Both";
  precision?: "Fuzzy" | "Exact";
}) =>
  request(
    "POST",
    "/espionage-intel",
    () =>
      fetchLive<unknown>("POST", "/espionage-intel", {
        targetKind: payload.targetKind,
        targetId: payload.targetId,
        scope: payload.scope ?? "Both",
        precision: payload.precision ?? "Fuzzy",
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持谍报");
    }
  );

/** 预览外交使节任务成功率。 */
export const previewDiplomacyMission = (payload: {
  characterId: number;
  targetForceId: number;
  action: "Ally" | "War" | "Peace";
}) =>
  request(
    "POST",
    "/diplomacy/mission/preview",
    () =>
      fetchLive<{
        successChancePercent?: number;
        SuccessChancePercent?: number;
        travelDays?: number;
        TravelDays?: number;
        idleOfficers?: { characterId?: number; CharacterId?: number; name?: string; Name?: string }[];
        IdleOfficers?: { characterId?: number; CharacterId?: number; name?: string; Name?: string }[];
      }>("POST", "/diplomacy/mission/preview", payload).then((raw) => ({
        successChancePercent: raw.successChancePercent ?? raw.SuccessChancePercent ?? 0,
        travelDays: raw.travelDays ?? raw.TravelDays ?? 0,
        idleOfficers: (raw.idleOfficers ?? raw.IdleOfficers ?? []).map((o) => ({
          characterId: o.characterId ?? o.CharacterId ?? 0,
          name: o.name ?? o.Name ?? "",
        })),
      })),
    () => ({
      successChancePercent: 50,
      travelDays: 7,
      idleOfficers: [] as { characterId: number; name: string }[],
    })
  );

/** 派遣外交使节任务。 */
export const orderDiplomacyMission = (payload: {
  characterId: number;
  targetForceId: number;
  action: "Ally" | "War" | "Peace";
}) =>
  request(
    "POST",
    "/diplomacy/mission",
    () =>
      fetchLive<unknown>("POST", "/diplomacy/mission", payload).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持外交使节任务");
    }
  );

export interface StrategyPeaceTermsPayload {
  characterId: number;
  targetForceId: number;
  cededStrongholdIds: number[];
  reparationsMoney: number;
  demandOuterVassalage: boolean;
}

export interface StrategyPeaceSettlementPreview {
  warId: number;
  proposerWarScore: number;
  requiredWarScore: number;
  acceptanceChancePercent: number;
  canForceAcceptance: boolean;
  isWhitePeace: boolean;
  termCosts: { kind: string; label: string; warScoreCost: number }[];
}

/** 预览多条款和谈。 */
export const previewPeaceSettlement = (payload: StrategyPeaceTermsPayload) =>
  request(
    "POST",
    "/diplomacy/peace/preview",
    () =>
      fetchLive<Record<string, unknown>>("POST", "/diplomacy/peace/preview", payload).then((raw) => {
        const pick = (camel: string, pascal: string) => raw[camel] ?? raw[pascal];
        const costsRaw = pick("termCosts", "TermCosts");
        return {
          warId: Number(pick("warId", "WarId") ?? 0),
          proposerWarScore: Number(pick("proposerWarScore", "ProposerWarScore") ?? 0),
          requiredWarScore: Number(pick("requiredWarScore", "RequiredWarScore") ?? 0),
          acceptanceChancePercent: Number(
            pick("acceptanceChancePercent", "AcceptanceChancePercent") ?? 0,
          ),
          canForceAcceptance: Boolean(pick("canForceAcceptance", "CanForceAcceptance")),
          isWhitePeace: Boolean(pick("isWhitePeace", "IsWhitePeace")),
          termCosts: Array.isArray(costsRaw)
            ? costsRaw.map((entry) => {
                const row = entry as Record<string, unknown>;
                return {
                  kind: String(row.kind ?? row.Kind ?? ""),
                  label: String(row.label ?? row.Label ?? ""),
                  warScoreCost: Number(row.warScoreCost ?? row.WarScoreCost ?? 0),
                };
              })
            : [],
        } satisfies StrategyPeaceSettlementPreview;
      }),
    () => ({
      warId: 1,
      proposerWarScore: 0,
      requiredWarScore: 0,
      acceptanceChancePercent: 50,
      canForceAcceptance: false,
      isWhitePeace: true,
      termCosts: [],
    }),
  );

/** 派遣携带和谈条款的使节。 */
export const orderPeaceSettlement = (payload: StrategyPeaceTermsPayload) =>
  request(
    "POST",
    "/diplomacy/peace",
    () =>
      fetchLive<unknown>("POST", "/diplomacy/peace", payload).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持多条款和谈");
    },
  );

export const setStrongholdTaxRates = (
  strongholdId: number,
  payload: {
    pollTaxRate?: number;
    agricultureTaxRate?: number;
    commerceTaxRate?: number;
    tariffTaxRate?: number;
  }
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/set-tax-rate`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/set-tax-rate`, payload).then(
        (raw) => {
          const body = raw as Record<string, unknown>;
          return {
            state: normalizeStrategyWorldState(body.state ?? body.State),
            outcome: String(body.outcome ?? body.Outcome ?? ""),
          };
        }
      ),
    () => {
      throw new Error("Mock 模式不支持调整税率");
    }
  );

export const setStrongholdGovernancePriority = (
  strongholdId: number,
  priority: "Autonomous" | "Military" | "Domestic"
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/governance-priority`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/governance-priority`, {
        priority,
      }).then((raw) => {
        const body = raw as Record<string, unknown>;
        return {
          state: normalizeStrategyWorldState(body.state ?? body.State),
          outcome: String(body.outcome ?? body.Outcome ?? ""),
        };
      }),
    () => {
      throw new Error("Mock 模式不支持设置方针");
    }
  );

export const recruitAtStronghold = (strongholdId: number, characterId: number) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/recruit`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/recruit`, { characterId }).then(
        normalizeStrategyWorldState
      ),
    () => {
      throw new Error("Mock 模式不支持征兵");
    }
  );

export const mercenaryRecruitAtStronghold = (
  strongholdId: number,
  characterId: number,
  budgetMoney: number,
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/mercenary-recruit`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/mercenary-recruit`, {
        characterId,
        budgetMoney,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持募兵");
    }
  );

export const personalRecruit = (characterId: number) =>
  request(
    "POST",
    `/characters/${characterId}/personal-recruit`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/personal-recruit`, {}).then(
        normalizeStrategyWorldState,
      ),
    () => {
      throw new Error("Mock 模式不支持个人征兵");
    },
  );

export const personalMercenaryRecruit = (characterId: number, budgetMoney: number) =>
  request(
    "POST",
    `/characters/${characterId}/personal-mercenary-recruit`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/personal-mercenary-recruit`, {
        budgetMoney,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持个人募兵");
    },
  );

export type AppointOfficialKind = "Lord" | "Mayor";

export const appointStrongholdLord = (
  strongholdId: number,
  characterId: number,
  appointType: AppointOfficialKind = "Lord",
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/appoint-lord`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/appoint-lord`, {
        characterId,
        appointType,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持任命");
    }
  );

export const transferCharacterToStronghold = (
  strongholdId: number,
  payload: {
    characterId: number;
    mode?: "dispatch" | "summon";
    destinationStrongholdId?: number;
  },
) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/transfer-character`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/transfer-character`, {
        characterId: payload.characterId,
        mode: payload.mode === "dispatch" ? "Dispatch" : "Summon",
        destinationStrongholdId: payload.destinationStrongholdId ?? 0,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持调动");
    }
  );

export const recallCharacter = (strongholdId: number, characterId: number) =>
  request(
    "POST",
    `/strongholds/${strongholdId}/recall-character`,
    () =>
      fetchLive<unknown>("POST", `/strongholds/${strongholdId}/recall-character`, {
        characterId,
      }).then((raw) => {
        const body = raw as Record<string, unknown>;
        return {
          state: normalizeStrategyWorldState(body.state ?? body.State),
          outcome: String(body.outcome ?? body.Outcome ?? ""),
        };
      }),
    () => {
      throw new Error("Mock 模式不支持召回");
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

export const leaveStrongholdAsCharacter = (characterId: number, force = false) =>
  request(
    "POST",
    `/characters/${characterId}/leave-stronghold`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/leave-stronghold`, { force }).then(
        normalizeStrategyWorldState,
      ),
    () => {
      throw new Error("Mock 模式不支持出城");
    },
  );

export const moveCharacter = (
  characterId: number,
  x: number,
  y: number,
  via?: MapPoint[],
) =>
  request(
    "POST",
    `/characters/${characterId}/move`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/move`, {
        x,
        y,
        via: via?.map((p) => ({ x: p.x, y: p.y })),
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持角色移动");
    },
  );

export const enterStrongholdAsCharacter = (characterId: number, strongholdId: number, force = false) =>
  request(
    "POST",
    `/characters/${characterId}/enter-stronghold`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/enter-stronghold`, {
        strongholdId,
        force,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持入城");
    },
  );

export const interactWithCharacter = (
  characterId: number,
  targetCharacterId: number,
  interaction: "Talk" | "Gift",
) =>
  request(
    "POST",
    `/characters/${characterId}/interact`,
    () =>
      fetchLive<unknown>("POST", `/characters/${characterId}/interact`, {
        targetCharacterId,
        interaction,
      }).then(normalizeStrategyWorldState),
    () => {
      throw new Error("Mock 模式不支持人物互动");
    },
  );

export const previewCharacterPath = (
  characterId: number,
  x: number,
  y: number,
  options?: { from?: MapPoint; via?: MapPoint[] },
) => {
  const body = {
    x,
    y,
    fromX: options?.from?.x,
    fromY: options?.from?.y,
    via: options?.via?.map((p) => ({ x: p.x, y: p.y })),
  };

  return request(
    "POST",
    `/characters/${characterId}/preview-path`,
    () => fetchLive<StrategyPathPreview>("POST", `/characters/${characterId}/preview-path`, body),
    () => {
      throw new Error("Mock 模式不支持角色路径预览");
    },
  );
};

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
          daysAdvanced: Number(payload.daysAdvanced ?? payload.DaysAdvanced ?? 1),
        };
      }),
    () => {
      const raw = mockAdvanceDay();
      return {
        state: normalizeStrategyWorldState(raw.state),
        resolvedBattles: raw.resolvedBattles,
        events: raw.events,
        daysAdvanced: 1,
      };
    }
  );

export const advanceDays = (days: number) =>
  request(
    "POST",
    "/advance-days",
    () =>
      fetchLive<unknown>("POST", "/advance-days", { days }).then((raw) => {
        const payload = raw as Record<string, unknown>;
        const battlesRaw = payload.resolvedBattles ?? payload.ResolvedBattles;
        const eventsRaw = payload.events ?? payload.Events;
        return {
          state: normalizeStrategyWorldState(payload.state ?? payload.State),
          resolvedBattles: Array.isArray(battlesRaw)
            ? battlesRaw.map((battle) => normalizeBattleResult(battle))
            : [],
          events: Array.isArray(eventsRaw)
            ? eventsRaw.map((event) => normalizeStrategyEvent(event))
            : [],
          daysAdvanced: Number(payload.daysAdvanced ?? payload.DaysAdvanced ?? days),
        };
      }),
    () => {
      throw new Error("Mock 模式不支持批量推进");
    },
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

function saveSlotStorageKey(slot: number): string {
  return `${STRATEGY_SAVE_SLOT_KEY_PREFIX}${String(slot).padStart(2, "0")}`;
}

function saveSlotMetaStorageKey(slot: number): string {
  return `${STRATEGY_SAVE_SLOT_META_PREFIX}${String(slot).padStart(2, "0")}`;
}

function normalizeSaveSlotSummary(raw: Record<string, unknown>): StrategySaveSlotSummary {
  return {
    slot: Number(raw.slot ?? raw.Slot ?? 0),
    occupied: Boolean(raw.occupied ?? raw.Occupied),
    savedAtUtc: (raw.savedAtUtc ?? raw.SavedAtUtc ?? null) as string | null,
    scenarioId: (raw.scenarioId ?? raw.ScenarioId ?? null) as string | null,
    lordName: (raw.lordName ?? raw.LordName ?? null) as string | null,
    dateLabel: (raw.dateLabel ?? raw.DateLabel ?? null) as string | null,
  };
}

function readMockSaveSlots(): StrategySaveSlotSummary[] {
  const slots: StrategySaveSlotSummary[] = [];
  for (let slot = 1; slot <= SAVE_SLOT_COUNT; slot++) {
    const metaRaw = localStorage.getItem(saveSlotMetaStorageKey(slot));
    if (!metaRaw) {
      slots.push({ slot, occupied: false });
      continue;
    }
    try {
      slots.push(normalizeSaveSlotSummary(JSON.parse(metaRaw) as Record<string, unknown>));
    } catch {
      slots.push({ slot, occupied: false });
    }
  }
  return slots;
}

function writeMockSaveSlot(slot: number, json: string, summary: StrategySaveSlotSummary): void {
  localStorage.setItem(saveSlotStorageKey(slot), json);
  localStorage.setItem(saveSlotMetaStorageKey(slot), JSON.stringify(summary));
}

/** 列出 10 个存档位摘要。 */
export async function listStrategySaveSlots(): Promise<StrategySaveSlotSummary[]> {
  const mode = getApiMode();
  if (mode === "mock") {
    return readMockSaveSlots();
  }

  const payload = await fetchLive<{ slots?: unknown[]; Slots?: unknown[] }>("GET", "/save-slots");
  const rows = payload.slots ?? payload.Slots ?? [];
  return rows.map((row) => normalizeSaveSlotSummary(row as Record<string, unknown>));
}

/** 将当前仿真写入指定存档位。 */
export async function saveStrategyToSlot(slot: number): Promise<StrategySaveSlotSummary> {
  const mode = getApiMode();
  if (mode === "mock") {
    const json = await exportStrategySave();
    const state = normalizeStrategyWorldState(mockGetState());
    const summary: StrategySaveSlotSummary = {
      slot,
      occupied: true,
      savedAtUtc: new Date().toISOString(),
      scenarioId: state.scenarioId,
      lordName: state.lord?.name ?? "当主",
      dateLabel: `${state.date.year}年${state.date.month}月${state.date.day}日`,
    };
    writeMockSaveSlot(slot, json, summary);
    return summary;
  }

  const payload = await fetchLive<{ slot?: Record<string, unknown>; Slot?: Record<string, unknown> }>(
    "PUT",
    `/save-slots/${slot}`
  );
  const row = payload.slot ?? payload.Slot ?? {};
  return normalizeSaveSlotSummary(row);
}

/** 从指定存档位恢复仿真。 */
export async function loadStrategyFromSlot(slot: number): Promise<StrategyWorldState> {
  const mode = getApiMode();
  if (mode === "mock") {
    const raw = localStorage.getItem(saveSlotStorageKey(slot));
    if (!raw) throw new Error("该档位无存档");
    return restoreStrategySave(raw);
  }

  return normalizeStrategyWorldState(
    await fetchLive<unknown>("POST", `/save-slots/${slot}/load`)
  );
}

export type { StrategyApiMode };
