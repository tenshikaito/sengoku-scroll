/** 与后端 StrategyWorldStateDto 对齐（M2-a）。 */
export interface StrategyWorldState {
  scenarioId: string;
  playerForceId: number;
  lord: StrategyLordState;
  map: StrategyMapState;
  date: { year: number; month: number; day: number };
  forces: StrategyForceState[];
  strongholds: StrategyStrongholdState[];
  units: StrategyUnitState[];
  supplyConvoys: StrategySupplyConvoyState[];
  messengers: StrategyMessengerState[];
  /** 玩家势力视角外交（目标势力 Id + 关系）。 */
  diplomacies: StrategyDiplomacyState[];
}

export interface StrategyMapState {
  name: string;
  width: number;
  height: number;
  roadCells?: StrategyRoadCellState[];
  /** 逐格地形名（行优先）。 */
  tileTerrainNames?: string[];
  /** 逐格政治区域名（行优先；无区域为 null）。 */
  tileRegionNames?: (string | null)[];
  /** 地图地标（GameMapMasterData.StrongholdPoints）。 */
  landmarks?: StrategyMapLandmarkState[];
}

export interface StrategyMapLandmarkState {
  id: number;
  name: string;
  x: number;
  y: number;
}

/** 玩家视角外交摘要；内藩归属由前端沿宗主链归并。 */
export interface StrategyDiplomacyState {
  targetForceId: number;
  /** Neutral | Allied | Enemy */
  relation: string;
}

export interface StrategyRoadCellState {
  x: number;
  y: number;
  typeId: number;
  typeName: string;
  level: number;
  speedBonus: number;
  movementCost: number;
}

export interface StrategyForceState {
  id: number;
  name: string;
  food: number;
  money: number;
  /** Independence | InnerVassal | OuterVassal */
  status: string;
  suzerainForceId?: number | null;
}

export interface StrategyStrongholdState {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  food: number;
  population: number;
  lordId: number;
  isDirectRule: boolean;
  lordName: string;
  mayorName?: string | null;
  morale: number;
  training: number;
  cultureName: string;
  religionName: string;
  money: number;
  pollTaxRate: number;
  agricultureTaxRate: number;
  commerceTaxRate: number;
  tariffTaxRate: number;
}

export interface StrategyUnitState {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  soldiers: number;
  food: number;
  ap: number;
  movement: number;
  status: string;
  /** UnitDirective 枚举名。 */
  directive: string;
  /** 剩余移动路径（含当前格），与后端 Route 对齐。 */
  route: MapPoint[];
  /** 总将（出征编组时确定）。 */
  commanderName?: string | null;
  commanderId?: number | null;
  morale: number;
  training: number;
  cultureName: string;
  religionName: string;
  money: number;
  /** 兵种/备队构成；无则空数组。 */
  composition: StrategySubUnitState[];
  /** Sufficient | Strained | CutOff */
  supplyStatus: string;
  foodDaysRemaining: number;
  inTransitSupplies: StrategyInTransitSupply[];
}

export interface StrategyInTransitSupply {
  convoyId: number;
  cargoFoodGo: number;
  estimatedDays: number;
  isDeceived: boolean;
  originStrongholdName?: string | null;
}

/** 单位内子编制（兵种/备队）。 */
export interface StrategySubUnitState {
  id: number;
  typeId: number;
  typeName: string;
  soldiers: number;
  ratioPercent: number;
  commanderId?: number | null;
  commanderName?: string | null;
}

export interface MapPoint {
  x: number;
  y: number;
}

export interface StrategyPathPreview {
  points: MapPoint[];
}

/** 瞬间战战前预览（M3-a）。 */
export interface StrategyBattlePreview {
  attackerUnitId: number;
  defenderUnitId: number;
  targetX: number;
  targetY: number;
  attackerWinRatePercent: number;
  attackerSoldiers: number;
  defenderSoldiers: number;
  defenderName: string;
  estimatedAttackerLossMin: number;
  estimatedAttackerLossMax: number;
  estimatedDefenderLossMin: number;
  estimatedDefenderLossMax: number;
  resolutionSeed: number;
}

export interface StrategyBattleLogEntry {
  order: number;
  /** attacker | defender | system */
  side: string;
  phase: string;
  message: string;
}

export interface StrategyBattleResult {
  attackerWon: boolean;
  attackerUnitId: number;
  defenderUnitId: number;
  attackerName: string;
  defenderName: string;
  attackerSoldiersBefore: number;
  defenderSoldiersBefore: number;
  attackerCasualties: number;
  defenderCasualties: number;
  attackerSoldiersAfter: number;
  defenderSoldiersAfter: number;
  attackerWinRatePercent: number;
  resolutionSeed: number;
  resolutionRoll: number;
  logEntries: StrategyBattleLogEntry[];
}

export interface StrategyInstantBattleResponse {
  state: StrategyWorldState;
  result: StrategyBattleResult;
}

export interface StrategySupplyConvoyState {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  isMilitary: boolean;
  commanderName?: string | null;
  commanderId?: number | null;
  soldiers: number;
  porterCount: number;
  escortSoldierCount: number;
  /** 载粮（合），与 units.food 字段对齐。 */
  food: number;
  cargoFoodGo: number;
  ap: number;
  movement: number;
  status: string;
  directive: string;
  route: MapPoint[];
  morale: number;
  training: number;
  cultureName: string;
  religionName: string;
  money: number;
  targetUnitId: number;
  targetUnitName?: string | null;
  originStrongholdId: number;
  originStrongholdName?: string | null;
  isReturningToOrigin: boolean;
}

export interface StrategyMessengerState {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  isMilitary: boolean;
  soldiers: number;
  courierCount: number;
  escortSoldierCount: number;
  ap: number;
  movement: number;
  status: string;
  payloadType: string;
  directive: string;
  route: MapPoint[];
  morale: number;
  training: number;
  cultureName: string;
  religionName: string;
  money: number;
  targetUnitId: number;
  targetUnitName?: string | null;
  originStrongholdId: number;
  originStrongholdName?: string | null;
  pendingDirective?: string | null;
}

export interface StrategyLordState {
  name: string;
  unitId?: number | null;
  x: number;
  y: number;
}

export interface StrategyAdvanceDayResponse {
  state: StrategyWorldState;
  resolvedBattles: StrategyBattleResult[];
  events: StrategyEvent[];
}

export interface StrategyTributeLine {
  originName: string;
  food: number;
  money: number;
}

export interface StrategyEconomySettlementDetail {
  /** Monthly | Annual */
  period: "Monthly" | "Annual";
  reportingYear: number;
  /** 月度时为 1–12；年度时为 0。 */
  reportingMonth: number;
  totalFood: number;
  totalMoney: number;
  expenseMoney: number;
  armyMaintenanceMoney: number;
  treasuryMoney: number;
  treasuryFood: number;
  tributeLines: StrategyTributeLine[];
}

/** @deprecated 使用 StrategyEconomySettlementDetail */
export type StrategyEconomyMonthlyDetail = StrategyEconomySettlementDetail;

/** 日推进或信使投递产生的玩家可见事件。 */
export interface StrategyEvent {
  category: string;
  message: string;
  /** 大略信息（左上角消息栏）；省略时前端自行简化 Message。 */
  brief?: string;
  /** Category=EconomyMonthly | EconomyAnnual 时的结构化明细。 */
  economySettlement?: StrategyEconomySettlementDetail;
}

export interface StrategyPolicyChangeResponse {
  state: StrategyWorldState;
  /** AppliedImmediately | MessengerDispatched */
  outcome: string;
}
