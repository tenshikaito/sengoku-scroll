/** 地图格坐标（路径、移动预览等）。 */
export interface MapPoint {
  x: number;
  y: number;
}

/** 与后端 StrategyWorldStateDto 对齐（M2-a）。 */
export interface StrategyWorldState {
  scenarioId: string;
  playerForceId: number;
  /** Easy | Normal | Hard | Custom */
  difficulty?: string;
  /** 本局固定随机种子 */
  simulationSeed?: number;
  lord: StrategyLordState;
  map: StrategyMapState;
  date: { year: number; month: number; day: number };
  forces: StrategyForceState[];
  strongholds: StrategyStrongholdState[];
  units: StrategyUnitState[];
  /** 迷雾外己方部队摘要（无坐标）。 */
  ownUnitRoster?: StrategyUnitRosterEntry[];
  /** 进行中地图交战格（折叠显示用）。 */
  battlefields?: StrategyBattlefieldState[];
  supplyConvoys: StrategySupplyConvoyState[];
  messengers: StrategyMessengerState[];
  /** 玩家势力视角外交（目标势力 Id + 关系）。 */
  diplomacies: StrategyDiplomacyState[];
  /** 将领摘要（id + 所属势力）；用于本势力将领数统计。 */
  characters?: StrategyCharacterSummaryState[];
  /** 地图上独立行动的将领（溃逃回城等）。 */
  mapCharacters?: StrategyMapCharacterState[];
  /** 谍报获得的情报条目。 */
  espionageIntel?: StrategyEspionageIntelEntry[];
  /** 剧本 Master Data 快照。 */
  masterData?: StrategyMasterDataSnapshot;
  /** 玩家战争迷雾（explored / visible / known）。 */
  visibility?: StrategyVisibilityState;
  /** 本局开局选项快照。 */
  startOptions?: GameStartOptionsState;
}

export interface StrategyLoadRequest {
  scenarioId: string;
  difficulty?: "Easy" | "Normal" | "Hard" | "Custom";
  customStartOptions?: GameStartOptionsState;
}

export interface StrategyVisibilityState {
  fogMode: string;
  intelMode: string;
  controlMode: string;
  instantEventMessages: boolean;
  allySharedVision: boolean;
  mapWidth: number;
  mapHeight: number;
  exploredBits: number[];
  visibleCells: MapPoint[];
  knownStrongholdIds: number[];
}

export interface GameStartOptionsState {
  fogMode: string;
  intelMode: string;
  controlMode: string;
  allySharedVision: boolean;
  instantEventMessages: boolean;
}

export interface StrategyCharacterSummaryState {
  id: number;
  forceId: number;
  name?: string;
  strongholdId?: number;
  /** 直属上司角色 Id。 */
  leaderId?: number;
  /** Map | Stronghold | Unit */
  locationType?: string;
  /** Idle | UnitAction | Task | Prisoner */
  forceStatus?: string;
  leadership?: number;
  power?: number;
  politics?: number;
  strategy?: number;
  charm?: number;
  cultureName?: string;
  religionName?: string;
  /** 在本家势力仕官年数。 */
  yearsInForce?: number;
  sex?: string;
  age?: number;
  personality?: StrategyCharacterPersonalityState;
  proficiency?: StrategyCharacterProficiencyState;
  isDead?: boolean;
  /** 是否生病。 */
  isSick?: boolean;
  /** RoyalFamily | Noble | Landlord | Normal | Slave */
  birthType?: string;
  /** 任务剩余天数。 */
  taskRemainingDays?: number | null;
  /** 忠诚度 0–100。 */
  loyalty?: number;
}

/** 地图上独立行动的将领。 */
export interface StrategyMapCharacterState {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  mapVisible?: boolean;
}

export interface StrategyEspionageIntelEntry {
  targetKind: string;
  targetId: number;
  scope: string;
  precision: string;
  expiresYear: number;
  expiresMonth: number;
  expiresDay: number;
}

export interface StrategyMasterDataEntry {
  id: number;
  name: string;
  group?: string | null;
  description?: string | null;
  extra?: string | null;
  /** 按字段展开的明细（情报 Master Data 表格列）。 */
  fields?: Record<string, string>;
}

export interface StrategyMasterDataSnapshot {
  cultureGroups: StrategyMasterDataEntry[];
  cultures: StrategyMasterDataEntry[];
  religionGroups: StrategyMasterDataEntry[];
  religions: StrategyMasterDataEntry[];
  strongholdTypes: StrategyMasterDataEntry[];
  defenseFacilityTypes: StrategyMasterDataEntry[];
  unitTypes: StrategyMasterDataEntry[];
  characterDefinitions: StrategyMasterDataEntry[];
  terrains: StrategyMasterDataEntry[];
  climates: StrategyMasterDataEntry[];
  weathers: StrategyMasterDataEntry[];
  regions: StrategyMasterDataEntry[];
  roads: StrategyMasterDataEntry[];
  landmarks: StrategyMasterDataEntry[];
  terrainVegetationFeatures: StrategyMasterDataEntry[];
  terrainSurfaceFeatures: StrategyMasterDataEntry[];
  enums: StrategyMasterDataEntry[];
}

export interface StrategyCharacterPersonalityState {
  temper?: number;
  courage?: number;
  principle?: number;
  action?: number;
  friendship?: number;
  ambition?: number;
  hobby?: number;
  desire?: number;
  drinking?: number;
  fortune?: number;
}

export interface StrategyCharacterProficiencyState {
  infantry?: number;
  ride?: number;
  archery?: number;
  firelock?: number;
  sealing?: number;
  military?: number;
  fighting?: number;
  spy?: number;
  agriculture?: number;
  commerce?: number;
  construct?: number;
  smelt?: number;
  eloquence?: number;
  court?: number;
  sociality?: number;
  healing?: number;
}

export interface StrategyMapState {
  name: string;
  width: number;
  height: number;
}

export interface StrategyTerrainDef {
  id: number;
  key: string;
  name: string;
  movementCost: number;
}

export interface StrategyRegionDef {
  id: number;
  key: string;
  name: string;
}

export interface StrategyRoadTypeDef {
  id: number;
  key: string;
  name: string;
  speedBonus: number;
  movementCost?: number;
}

/** 地图静态主数据（启动时加载一次，不随日推进重复下发）。 */
export interface StrategyMapMasterState {
  scenarioId: string;
  name: string;
  width: number;
  height: number;
  terrains: StrategyTerrainDef[];
  regions: StrategyRegionDef[];
  roadTypes: StrategyRoadTypeDef[];
  /** 行优先地形 Id。 */
  terrainIds: number[];
  /** 行优先区域 Id（0 = 无）。 */
  regionIds: number[];
  roadCells: StrategyRoadCellState[];
  landmarks: StrategyMapLandmarkState[];
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
  /** 外交关系值 -100~100。 */
  relationship?: number;
  /** 信赖度 -100~100。 */
  trust?: number;
  arrearsFoodGo?: number;
  arrearsMoney?: number;
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
  strongholdCount?: number;
  characterCount?: number;
  prestige?: number;
  orthodoxy?: number;
  /** 当主角色驻留据点 Id；无则 0。 */
  lordResidenceStrongholdId?: number;
  /** 势力内贡赋欠粮（合）。 */
  internalArrearsFoodGo?: number;
  /** 势力内贡赋欠钱（最小货币单位）。 */
  internalArrearsMoney?: number;
  /** 继承人角色 Id；无则 null。 */
  successorId?: number | null;
}

export interface StrategyStrongholdState {
  id: number;
  name: string;
  /** 据点类型 Id（平城/平山城/山城）。 */
  typeId: number;
  /** 据点类型显示名。 */
  typeName: string;
  forceId: number;
  x: number;
  y: number;
  food: number;
  population: number;
  stability: number;
  popularFeelings: number;
  isLordResidence: boolean;
  lordId: number;
  isDirectRule: boolean;
  lordName: string;
  mayorName?: string | null;
  morale: number;
  training: number;
  cultureName: string;
  religionName: string;
  money: number;
  /** 城内驻军士兵数（非地图单位）。 */
  garrisonSoldiers: number;
  /** 城内伤兵数。 */
  garrisonWounded?: number;
  pollTaxRate: number;
  agricultureTaxRate: number;
  commerceTaxRate: number;
  tariffTaxRate: number;
  /** false = 虚构据点。 */
  isHistorical: boolean;
  /** 城防（城防设施防御值累加）。 */
  defense: number;
  /** 已建城防设施。 */
  defenseFacilities: StrategyDefenseFacilityState[];
  /** 当前被进攻状态：Assault=强攻，Encircle=围城。 */
  siegeThreat?: string | null;
  /** Visible | Known | Hidden */
  visibilityTier?: string | null;
  espionageSoldiersBand?: string | null;
  espionageMoraleBand?: string | null;
  espionageTrainingBand?: string | null;
  espionagePopulationBand?: string | null;
  espionageFoodBand?: string | null;
  espionageMoneyBand?: string | null;
}

export interface StrategyDefenseFacilityState {
  typeId: number;
  name: string;
  /** Castle | Wall | Gate | Moat | Defender */
  category: string;
  /** 设施等级（1–3）。 */
  level: number;
  /** 该设施城防加成。 */
  defense: number;
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
  /** UnitStance 枚举名。 */
  stance: string;
  /** UnitSiegeMode 枚举名。 */
  siegeMode: string;
  directiveTargetId: number;
  targetStrongholdName?: string | null;
  targetUnitId: number;
  targetUnitName?: string | null;
  /** 所属地图战场 Id；0 表示未入战。 */
  battlefieldId?: number;
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
  /** 是否在地图层绘制（迷雾外己方单位可在侧栏列出）。 */
  mapVisible?: boolean;
  /** 情报模糊兵数（**** / 3***）。 */
  soldiersDisplay?: string | null;
  moraleBand?: string | null;
  trainingBand?: string | null;
}

/** 迷雾外己方部队摘要（侧栏列表）。 */
export interface StrategyUnitRosterEntry {
  id: number;
  name: string;
  forceId: number;
  x: number;
  y: number;
  soldiers: number;
  status: string;
  directive: string;
  ap: number;
  supplyStatus: string;
  commanderName?: string | null;
  offMap: boolean;
}

/** 战场内按势力汇总的参战摘要。 */
export interface StrategyBattlefieldParticipant {
  forceId: number;
  forceName: string;
  soldiers: number;
  morale: number;
  money: number;
  food: number;
}

/** 地图交战战场（交战格用白底红字图标替代双军）。 */
export interface StrategyBattlefieldState {
  id: number;
  x: number;
  y: number;
  /** Field | Siege */
  kind: string;
  standoffDays: number;
  /** 围城格：Assault | Encircle */
  siegeThreat?: string | null;
  soldierTotal: number;
  /** 攻方合计兵力；围城格「围」下仅显示此项。 */
  aggressorSoldierTotal: number;
  participants: StrategyBattlefieldParticipant[];
  unitIds: number[];
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

export interface StrategyDeployCompositionEntry {
  typeId: number;
  typeName?: string;
  soldiers: number;
  commanderId?: number | null;
}

export interface StrategyDeployFromStrongholdRequest {
  unitName?: string;
  commanderId: number;
  composition: StrategyDeployCompositionEntry[];
  food?: number;
  money?: number;
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

export interface StrategyBattleFactorNote {
  factorId: string;
  label: string;
  attackerWinRateDelta: number;
  defenderWinRateDelta: number;
  detail?: string | null;
}

export interface StrategyBattleResult {
  attackerWon: boolean;
  attackerUnitId: number;
  defenderUnitId: number;
  attackerForceId?: number;
  defenderForceId?: number;
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
  engagementKind?: string;
  logEntries: StrategyBattleLogEntry[];
  factorNotes?: StrategyBattleFactorNote[];
  /** 劝降成功（零伤亡）。 */
  isSurrendered?: boolean;
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
  residenceStrongholdName?: string | null;
}

export interface StrategyAdvanceDayResponse {
  state: StrategyWorldState;
  resolvedBattles: StrategyBattleResult[];
  events: StrategyEvent[];
}

export interface StrategyTributeLine {
  originName: string;
  forceName: string;
  lordName: string;
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
  convoyCount: number;
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
  /** Category=BattleReportArrived 时附带的完整战报。 */
  battleResult?: StrategyBattleResult;
  /** Category=StrategicReportArrived 时附带的原始事件分类。 */
  detailCategory?: string;
  /** Category=StrategicReportArrived 时附带的完整详情文案。 */
  detailMessage?: string;
}

export interface StrategyPolicyChangeResponse {
  state: StrategyWorldState;
  /** AppliedImmediately | MessengerDispatched */
  outcome: string;
}
