export type {
  StrategyWorldState,
  StrategyForceState,
  StrategyStrongholdState,
  StrategyUnitState,
  StrategySupplyConvoyState,
  StrategyMessengerState,
} from "./strategyTypes";

export {
  loadScenario,
  getStrategyState,
  moveUnit,
  previewUnitPath,
  previewBattle,
  executeInstantBattle,
  setUnitDirective,
  orderUnitAttack,
  advanceDay,
  getMovementTrace,
  exportStrategySave,
  restoreStrategySave,
  hasLocalStrategySave,
  setStrategySessionRecoveryHandler,
} from "./strategyClient";

export type { StrategyMovementTraceEntry } from "./strategyClient";
export type { StrategyPathPreview, StrategyBattlePreview, StrategyBattleResult, StrategyBattleLogEntry, StrategyInstantBattleResponse, StrategyPolicyChangeResponse, StrategyAdvanceDayResponse, StrategyLordState, StrategyEvent, StrategyEconomySettlementDetail, StrategyEconomyMonthlyDetail, MapPoint } from "./strategyTypes";

export {
  strategyApiDiagnostics,
  setApiMode,
  resolveRequestUrl,
  STRATEGY_API_PREFIX,
  type StrategyApiMode,
} from "./strategyDiagnostics";
