import type {
  StrategyAdvanceDayResponse,
  StrategyBattlePreview,
  StrategyEvent,
  StrategyInstantBattleResponse,
  StrategyPathPreview,
  StrategyStrongholdState,
  StrategyWorldState,
  MapPoint,
} from "./strategyTypes";
import { buildMiniKantoMapTiles } from "@/utils/strategyMapDefaults";
import { buildManhattanPath, concatPathSegments } from "@/utils/strategyPathUtils";

function enrichMockStrongholds(
  items: (Partial<StrategyStrongholdState> &
    Pick<StrategyStrongholdState, "id" | "name" | "forceId" | "x" | "y">)[],
  residenceName: string | null
): StrategyStrongholdState[] {
  return items.map((s) => {
    const lordId = s.lordId ?? 0;
    return {
      pollTaxRate: 10,
      agricultureTaxRate: 25,
      commerceTaxRate: 12,
      tariffTaxRate: 8,
      isHistorical: true,
      defense: 25,
      defenseFacilities: [],
      stability: 50,
      popularFeelings: 50,
      mayorName: null,
      morale: 80,
      training: 65,
      cultureName: "日本",
      religionName: "神道",
      money: 0,
      food: 0,
      population: 0,
      garrisonSoldiers: 800,
      lordName: "当主",
      ...s,
      lordId,
      isLordResidence: s.isLordResidence ?? s.name === residenceName,
      isDirectRule: s.isDirectRule ?? lordId === 0,
    };
  });
}

/** mini_kanto 初始状态（与后端 JSON 对齐，供 Mock 使用）。 */
export function createMiniKantoState(): StrategyWorldState {
  const residenceName = "清洲";
  return {
    scenarioId: "mini_kanto",
    playerForceId: 1,
    lord: { name: "织田信长", unitId: null, x: 1, y: 4, residenceStrongholdName: residenceName },
    map: {
      name: "迷你关东试玩",
      width: 10,
      height: 10,
      ...buildMiniKantoMapTiles(10, 10),
    },
    date: { year: 1560, month: 1, day: 1 },
    forces: [
      {
        id: 1,
        name: "织田",
        food: 80000000,
        money: 8000000,
        status: "Independence",
        strongholdCount: 5,
        characterCount: 4,
        prestige: 72,
        orthodoxy: 65,
        lordResidenceStrongholdId: 1,
      },
      {
        id: 2,
        name: "今川",
        food: 70000000,
        money: 6000000,
        status: "Independence",
        strongholdCount: 1,
        characterCount: 2,
        prestige: 58,
        orthodoxy: 70,
        lordResidenceStrongholdId: 6,
      },
    ],
    diplomacies: [{ targetForceId: 2, relation: "Enemy" }],
    strongholds: enrichMockStrongholds(
      [
      {
        id: 1,
        name: "清洲",
        forceId: 1,
        x: 1,
        y: 4,
        food: 90000000,
        population: 48000,
        pollTaxRate: 12,
        agricultureTaxRate: 28,
        commerceTaxRate: 15,
        tariffTaxRate: 10,
        lordId: 0,
        isDirectRule: true,
        isLordResidence: true,
        lordName: "织田信长",
        mayorName: "林秀贞",
        stability: 72,
        popularFeelings: 68,
        morale: 85,
        training: 68,
        cultureName: "日本",
        religionName: "神道",
        money: 18000000,
      },
      {
        id: 2,
        name: "犬山",
        forceId: 3,
        x: 2,
        y: 2,
        lordId: 6,
        food: 72000000,
        population: 36000,
        lordName: "酒井忠次",
        mayorName: "酒井忠次",
        morale: 78,
        training: 62,
        cultureName: "日本",
        religionName: "神道",
        money: 9000000,
      },
      {
        id: 3,
        name: "冈崎",
        forceId: 1,
        x: 1,
        y: 7,
        food: 60000000,
        population: 42000,
        morale: 80,
        training: 65,
        cultureName: "日本",
        religionName: "神道",
        money: 7200000,
      },
      {
        id: 4,
        name: "小田原",
        forceId: 2,
        x: 8,
        y: 5,
        food: 108000000,
        population: 72000,
        pollTaxRate: 11,
        agricultureTaxRate: 26,
        commerceTaxRate: 14,
        tariffTaxRate: 9,
        lordId: 5,
        lordName: "北条氏康",
        morale: 82,
        training: 70,
        cultureName: "日本",
        religionName: "神道",
        money: 24000000,
      },
      {
        id: 5,
        name: "骏府",
        forceId: 4,
        x: 7,
        y: 3,
        lordId: 7,
        lordName: "北条纲成",
        food: 84000000,
        population: 54000,
        morale: 79,
        training: 64,
        cultureName: "日本",
        religionName: "神道",
        money: 15000000,
      },
      {
        id: 6,
        name: "挂川",
        forceId: 2,
        x: 6,
        y: 7,
        food: 66000000,
        population: 39000,
        morale: 76,
        training: 60,
        cultureName: "日本",
        religionName: "神道",
        money: 10800000,
      },
      {
        id: 7,
        name: "三河凑",
        forceId: 1,
        x: 3,
        y: 6,
        food: 48000000,
        population: 24000,
        morale: 74,
        training: 58,
        cultureName: "日本",
        religionName: "神道",
        money: 4800000,
      },
      {
        id: 8,
        name: "伊豆港",
        forceId: 2,
        x: 8,
        y: 8,
        food: 60000000,
        population: 30000,
        morale: 75,
        training: 55,
        cultureName: "日本",
        religionName: "神道",
        money: 5400000,
      },
      {
        id: 9,
        name: "足助",
        forceId: 1,
        x: 4,
        y: 1,
        food: 42000000,
        population: 21000,
        morale: 72,
        training: 56,
        cultureName: "日本",
        religionName: "神道",
        money: 3600000,
      },
      {
        id: 10,
        name: "沼津",
        forceId: 2,
        x: 5,
        y: 8,
        food: 36000000,
        population: 18000,
        morale: 70,
        training: 54,
        cultureName: "日本",
        religionName: "神道",
        money: 3000000,
      },
    ],
      residenceName
    ),
    units: [
      {
        id: 1,
        name: "织田先锋",
        forceId: 1,
        x: 4,
        y: 4,
        soldiers: 3000,
        food: 6000000,
        ap: 10,
        movement: 10,
        status: "Waiting",
        directive: "Move",
        stance: "Normal",
        siegeMode: "None",
        directiveTargetId: 0,
        targetUnitId: 0,
        route: [],
        commanderName: "柴田胜家",
        commanderId: 2,
        morale: 82,
        training: 72,
        cultureName: "日本",
        religionName: "神道",
        money: 3000000,
        composition: [
          { id: 1, typeId: 1, typeName: "足轻", soldiers: 1890, ratioPercent: 63 },
          { id: 2, typeId: 2, typeName: "弓兵", soldiers: 480, ratioPercent: 16 },
          { id: 3, typeId: 3, typeName: "骑兵", soldiers: 360, ratioPercent: 12 },
          { id: 4, typeId: 4, typeName: "铁炮", soldiers: 270, ratioPercent: 9 },
        ],
        supplyStatus: "Sufficient",
        foodDaysRemaining: 10,
        inTransitSupplies: [],
      },
      {
        id: 2,
        name: "今川先锋",
        forceId: 2,
        x: 5,
        y: 4,
        soldiers: 2400,
        food: 4800000,
        ap: 10,
        movement: 10,
        status: "Waiting",
        directive: "Move",
        stance: "Normal",
        siegeMode: "None",
        directiveTargetId: 0,
        targetUnitId: 0,
        route: [],
        commanderName: "今川氏真",
        commanderId: 3,
        morale: 78,
        training: 68,
        cultureName: "日本",
        religionName: "神道",
        money: 1800000,
        composition: [
          { id: 5, typeId: 1, typeName: "足轻", soldiers: 1500, ratioPercent: 63 },
          { id: 6, typeId: 2, typeName: "弓兵", soldiers: 360, ratioPercent: 15 },
          { id: 7, typeId: 3, typeName: "骑兵", soldiers: 300, ratioPercent: 13 },
          { id: 8, typeId: 4, typeName: "铁炮", soldiers: 240, ratioPercent: 10 },
        ],
        supplyStatus: "Sufficient",
        foodDaysRemaining: 8,
        inTransitSupplies: [],
      },
    ],
    supplyConvoys: [],
    messengers: [],
  };
}

const moveTargets = new Map<number, { x: number; y: number }>();
const pendingAttacks = new Map<number, { x: number; y: number }>();

let mockState = createMiniKantoState();

export function resetMockState(): StrategyWorldState {
  mockState = createMiniKantoState();
  moveTargets.clear();
  pendingAttacks.clear();
  return cloneState(mockState);
}

export function mockLoadScenario(_scenarioId: string): StrategyWorldState {
  return resetMockState();
}

export function mockGetState(): StrategyWorldState {
  return cloneState(mockState);
}

export function mockPreviewUnitPath(
  unitId: number,
  x: number,
  y: number,
  options?: { from?: MapPoint; via?: MapPoint[] }
): StrategyPathPreview {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const start = options?.from ?? { x: unit.x, y: unit.y };
  const stops = [...(options?.via ?? []), { x, y }];
  if (stops.length > 0 && stops[0]!.x === start.x && stops[0]!.y === start.y) {
    stops.shift();
  }
  let from = start;
  const segments: MapPoint[][] = [];
  for (const stop of stops) {
    segments.push(buildManhattanPath(from.x, from.y, stop.x, stop.y));
    from = stop;
  }
  return { points: concatPathSegments(segments) };
}

export function mockMoveUnit(
  unitId: number,
  x: number,
  y: number,
  via?: MapPoint[]
): StrategyWorldState {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const preview = mockPreviewUnitPath(unitId, x, y, { via });
  moveTargets.set(unitId, { x, y });
  unit.status = "Moving";
  unit.route = preview.points;
  return cloneState(mockState);
}

function manhattanAdjacent(a: { x: number; y: number }, b: { x: number; y: number }) {
  return Math.abs(a.x - b.x) + Math.abs(a.y - b.y) === 1;
}

export function mockPreviewBattle(unitId: number, x: number, y: number): StrategyBattlePreview {
  const attacker = mockState.units.find((u) => u.id === unitId);
  if (!attacker) throw new Error(`UnitNotFound:${unitId}`);
  const defender = mockState.units.find((u) => u.x === x && u.y === y);
  if (!defender || defender.forceId === attacker.forceId) throw new Error("AttackTargetNotFound");
  if (!manhattanAdjacent(attacker, { x, y })) throw new Error("TargetLocationNotAdjacent");

  const atkPower = attacker.soldiers;
  const defPower = defender.soldiers;
  const winRate = Math.min(95, Math.max(5, Math.round((atkPower / (atkPower + defPower)) * 100)));

  return {
    attackerUnitId: attacker.id,
    defenderUnitId: defender.id,
    targetX: x,
    targetY: y,
    attackerWinRatePercent: winRate,
    attackerSoldiers: attacker.soldiers,
    defenderSoldiers: defender.soldiers,
    defenderName: defender.name,
    estimatedAttackerLossMin: Math.floor(attacker.soldiers * 0.1),
    estimatedAttackerLossMax: Math.floor(attacker.soldiers * 0.25),
    estimatedDefenderLossMin: Math.floor(defender.soldiers * 0.3),
    estimatedDefenderLossMax: Math.floor(defender.soldiers * 0.6),
    resolutionSeed: 1,
  };
}

export function mockExecuteInstantBattle(
  unitId: number,
  x: number,
  y: number
): StrategyInstantBattleResponse {
  const preview = mockPreviewBattle(unitId, x, y);
  const attacker = mockState.units.find((u) => u.id === unitId)!;
  const defender = mockState.units.find((u) => u.id === preview.defenderUnitId)!;

  const attackerWon = preview.attackerWinRatePercent >= 50;
  const attLoss = Math.floor(attacker.soldiers * (attackerWon ? 0.15 : 0.4));
  const defLoss = Math.floor(defender.soldiers * (attackerWon ? 0.45 : 0.15));

  attacker.soldiers = Math.max(0, attacker.soldiers - attLoss);
  defender.soldiers = Math.max(0, defender.soldiers - defLoss);
  attacker.ap = Math.max(0, attacker.ap - 5);
  attacker.route = [];

  const attBefore = preview.attackerSoldiers;
  const defBefore = preview.defenderSoldiers;

  return {
    state: cloneState(mockState),
    result: {
      attackerWon,
      attackerUnitId: unitId,
      defenderUnitId: preview.defenderUnitId,
      attackerForceId: attacker.forceId,
      defenderForceId: defender.forceId,
      attackerName: attacker.name,
      defenderName: defender.name,
      attackerSoldiersBefore: attBefore,
      defenderSoldiersBefore: defBefore,
      attackerCasualties: attLoss,
      defenderCasualties: defLoss,
      attackerSoldiersAfter: attacker.soldiers,
      defenderSoldiersAfter: defender.soldiers,
      attackerWinRatePercent: preview.attackerWinRatePercent,
      resolutionSeed: preview.resolutionSeed,
      resolutionRoll: preview.attackerWinRatePercent >= 50 ? 10 : 90,
      logEntries: [
        { order: 1, side: "system", phase: "接触", message: `${attacker.name} 与 ${defender.name} 在野外遭遇。` },
        { order: 2, side: "attacker", phase: "接敌", message: `${attacker.name} 发起进攻（${attBefore} 名）。` },
        { order: 3, side: "defender", phase: "接敌", message: `${defender.name} 列阵应战（${defBefore} 名）。` },
        {
          order: 4,
          side: "system",
          phase: "交锋",
          message: `战前评估：攻方胜率 ${preview.attackerWinRatePercent}%。`,
        },
        attackerWon
          ? { order: 5, side: "attacker", phase: "突破", message: `突破成功，己方 −${attLoss} → 剩余 ${attacker.soldiers}。` }
          : { order: 5, side: "attacker", phase: "受挫", message: `攻势受挫，己方 −${attLoss} → 剩余 ${attacker.soldiers}。` },
        attackerWon
          ? { order: 6, side: "defender", phase: "溃退", message: `敌军 −${defLoss} → 剩余 ${defender.soldiers}。` }
          : { order: 6, side: "defender", phase: "维持", message: `守军 −${defLoss} → 剩余 ${defender.soldiers}。` },
        {
          order: 7,
          side: "system",
          phase: "结束",
          message: attackerWon ? "攻方获胜，当日野战结束。" : "守方获胜，当日野战结束。",
        },
      ],
    },
  };
}

export function mockOrderUnitAttack(unitId: number, x: number, y: number): StrategyWorldState {
  mockPreviewBattle(unitId, x, y);
  pendingAttacks.set(unitId, { x, y });
  return cloneState(mockState);
}

export function mockAdvanceDay(): StrategyAdvanceDayResponse {
  const events: StrategyEvent[] = [];

  for (const [unitId, target] of [...pendingAttacks.entries()]) {
    pendingAttacks.delete(unitId);
    const battle = mockExecuteInstantBattle(unitId, target.x, target.y);
    mockState = battle.state;
    events.push({
      category: "BattleReportArrived",
      message: `📨 战报信使抵达：${battle.result.attackerName} vs ${battle.result.defenderName}`,
      brief: `${battle.result.attackerName} vs ${battle.result.defenderName}`,
      battleResult: battle.result,
    });
  }

  const d = mockState.date;
  mockState = {
    ...mockState,
    date: advanceDate(d.year, d.month, d.day),
    units: mockState.units.map((u) => {
      const target = moveTargets.get(u.id);
      if (!target || u.status !== "Moving") return { ...u };

      let nx = u.x;
      let ny = u.y;
      if (u.x !== target.x) nx += Math.sign(target.x - u.x);
      else if (u.y !== target.y) ny += Math.sign(target.y - u.y);

      const arrived = nx === target.x && ny === target.y;
      if (arrived) moveTargets.delete(u.id);

      const route =
        u.route.length > 1
          ? u.route.slice(1)
          : arrived
            ? []
            : buildManhattanPath(nx, ny, target.x, target.y);

      return {
        ...u,
        x: nx,
        y: ny,
        ap: Math.max(0, u.ap - 2),
        status: arrived ? "Waiting" : "Moving",
        route: arrived ? [] : route.length > 0 ? route : buildManhattanPath(nx, ny, target.x, target.y),
      };
    }),
  };
  return { state: cloneState(mockState), resolvedBattles: [], events };
}

function advanceDate(year: number, month: number, day: number) {
  day += 1;
  if (day > 30) {
    day = 1;
    month += 1;
  }
  if (month > 12) {
    month = 1;
    year += 1;
  }
  return { year, month, day };
}

export function mockSetUnitDirective(
  unitId: number,
  directive: string
): import("./strategyTypes").StrategyPolicyChangeResponse {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const lord = mockState.lord;
  const issuerX = unit.forceId === mockState.playerForceId ? lord.x : unit.x;
  const issuerY = unit.forceId === mockState.playerForceId ? lord.y : unit.y;

  if (issuerX === unit.x && issuerY === unit.y) {
    unit.directive = directive;
    return { state: cloneState(mockState), outcome: "AppliedImmediately" };
  }

  const nextId = mockState.messengers.reduce((max, m) => Math.max(max, m.id), 0) + 1;
  mockState.messengers.push({
    id: nextId,
    name: `信使 #${nextId}`,
    forceId: unit.forceId,
    x: issuerX,
    y: issuerY,
    isMilitary: false,
    soldiers: 10,
    courierCount: 2,
    escortSoldierCount: 8,
    ap: 6,
    movement: 6,
    status: "Moving",
    payloadType: "PolicyChange",
    directive: "PolicyChange",
    route: buildManhattanPath(issuerX, issuerY, unit.x, unit.y),
    morale: 80,
    training: 70,
    cultureName: "日本",
    religionName: "神道",
    money: 0,
    targetUnitId: unitId,
    targetUnitName: unit.name,
    originStrongholdId: 0,
    originStrongholdName: null,
    pendingDirective: directive,
  });

  return { state: cloneState(mockState), outcome: "MessengerDispatched" };
}

function cloneState(state: StrategyWorldState): StrategyWorldState {
  return structuredClone(state);
}
