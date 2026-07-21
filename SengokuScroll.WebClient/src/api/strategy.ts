export type {
  StrategyWorldState,
  StrategyLoadRequest,
  StrategyMapMasterState,
  StrategyForceState,
  StrategyStrongholdState,
  StrategyUnitState,
  StrategyUnitRosterEntry,
  StrategySupplyConvoyState,
  StrategyMessengerState,
  StrategyBattlefieldState,
  StrategyBattlefieldParticipant,
} from "./strategyTypes";

export {
  loadScenario,
  getStrategyState,
  getStrategyMapMaster,
  orderUnitAttack,
  orderUnitSiege,
  mergeUnits,
  splitUnit,
  deployFromStronghold,
  recordEspionageIntel,
  moveUnit,
  previewUnitPath,
  previewBattle,
  executeInstantBattle,
  setUnitDirective,
  advanceDay,
  getMovementTrace,
  getAiDecisionTrace,
  exportStrategySave,
  restoreStrategySave,
  hasLocalStrategySave,
} from "./strategyClient";

export type { StrategyMovementTraceEntry, StrategyAiDecisionTraceEntry } from "./strategyClient";
export type { StrategyPathPreview, StrategyBattlePreview, StrategyBattleResult, StrategyBattleLogEntry, StrategyInstantBattleResponse, StrategyPolicyChangeResponse, StrategyAdvanceDayResponse, StrategyLordState, StrategyEvent, StrategyEconomySettlementDetail, StrategyEconomyMonthlyDetail, MapPoint } from "./strategyTypes";

export {
  strategyApiDiagnostics,
  setApiMode,
  resolveRequestUrl,
  STRATEGY_API_PREFIX,
  type StrategyApiMode,
} from "./strategyDiagnostics";
