import type {
  StrategyBattleFactorNote,
  StrategyBattleLogEntry,
  StrategyBattlePreview,
  StrategyBattleResult,
  StrategyWorldState,
} from "@/api/strategyTypes";

function safeInt(value: unknown, fallback = 0): number {
  const n = Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function normalizeLogEntry(raw: unknown, index: number): StrategyBattleLogEntry {
  const entry = raw as Record<string, unknown>;
  return {
    order: safeInt(entry.order ?? entry.Order, index + 1),
    side: String(entry.side ?? entry.Side ?? "system"),
    phase: String(entry.phase ?? entry.Phase ?? ""),
    message: String(entry.message ?? entry.Message ?? ""),
  };
}

function normalizeFactorNote(raw: unknown): StrategyBattleFactorNote {
  const n = raw as Record<string, unknown>;
  return {
    factorId: String(n.factorId ?? n.FactorId ?? ""),
    label: String(n.label ?? n.Label ?? ""),
    attackerWinRateDelta: safeInt(n.attackerWinRateDelta ?? n.AttackerWinRateDelta),
    defenderWinRateDelta: safeInt(n.defenderWinRateDelta ?? n.DefenderWinRateDelta),
    detail: (n.detail ?? n.Detail) != null ? String(n.detail ?? n.Detail) : null,
  };
}

export function normalizeBattleResult(raw: unknown): StrategyBattleResult {
  const r = raw as Record<string, unknown>;
  const logRaw = (r.logEntries ?? r.LogEntries) as unknown[] | undefined;
  const factorRaw = (r.factorNotes ?? r.FactorNotes) as unknown[] | undefined;

  return {
    attackerWon: Boolean(r.attackerWon ?? r.AttackerWon),
    attackerUnitId: safeInt(r.attackerUnitId ?? r.AttackerUnitId),
    defenderUnitId: safeInt(r.defenderUnitId ?? r.DefenderUnitId),
    attackerForceId: safeInt(r.attackerForceId ?? r.AttackerForceId, 0) || undefined,
    defenderForceId: safeInt(r.defenderForceId ?? r.DefenderForceId, 0) || undefined,
    attackerName: String(r.attackerName ?? r.AttackerName ?? ""),
    defenderName: String(r.defenderName ?? r.DefenderName ?? ""),
    attackerSoldiersBefore: safeInt(r.attackerSoldiersBefore ?? r.AttackerSoldiersBefore),
    defenderSoldiersBefore: safeInt(r.defenderSoldiersBefore ?? r.DefenderSoldiersBefore),
    attackerCasualties: safeInt(r.attackerCasualties ?? r.AttackerCasualties),
    defenderCasualties: safeInt(r.defenderCasualties ?? r.DefenderCasualties),
    attackerSoldiersAfter: safeInt(r.attackerSoldiersAfter ?? r.AttackerSoldiersAfter),
    defenderSoldiersAfter: safeInt(r.defenderSoldiersAfter ?? r.DefenderSoldiersAfter),
    attackerWinRatePercent: safeInt(r.attackerWinRatePercent ?? r.AttackerWinRatePercent),
    resolutionSeed: safeInt(r.resolutionSeed ?? r.ResolutionSeed),
    resolutionRoll: safeInt(r.resolutionRoll ?? r.ResolutionRoll),
    engagementKind: String(r.engagementKind ?? r.EngagementKind ?? "FieldBattle"),
    logEntries: Array.isArray(logRaw)
      ? logRaw.map((entry, index) => normalizeLogEntry(entry, index))
      : [],
    factorNotes: Array.isArray(factorRaw) ? factorRaw.map(normalizeFactorNote) : [],
    isSurrendered: Boolean(r.isSurrendered ?? r.IsSurrendered),
  };
}

export function isValidBattleResult(result: StrategyBattleResult): boolean {
  return (
    result.attackerUnitId > 0 &&
    result.defenderUnitId > 0 &&
    (result.attackerSoldiersBefore > 0 ||
      result.attackerSoldiersAfter >= 0 ||
      result.attackerCasualties >= 0)
  );
}

/** 旧版 API 仅返回世界状态时，由战前预览与前后状态推导战斗结果。 */
export function deriveBattleResult(
  preview: StrategyBattlePreview,
  attackerId: number,
  stateBefore: StrategyWorldState,
  stateAfter: StrategyWorldState
): StrategyBattleResult {
  const attackerBefore = stateBefore.units.find((u) => u.id === attackerId);
  const defenderBefore = stateBefore.units.find((u) => u.id === preview.defenderUnitId);
  const attackerAfter = stateAfter.units.find((u) => u.id === attackerId);
  const defenderAfter = stateAfter.units.find((u) => u.id === preview.defenderUnitId);

  const attBefore = attackerBefore?.soldiers ?? preview.attackerSoldiers;
  const defBefore = defenderBefore?.soldiers ?? preview.defenderSoldiers;
  const attAfter = attackerAfter?.soldiers ?? attBefore;
  const defAfter = defenderAfter?.soldiers ?? defBefore;
  const attLoss = Math.max(0, attBefore - attAfter);
  const defLoss = Math.max(0, defBefore - defAfter);
  let attackerWon = defLoss > attLoss || (defAfter === 0 && attAfter > 0);
  if (attLoss === 0 && defLoss === 0) {
    attackerWon = preview.attackerWinRatePercent >= 50;
  }

  const attackerName = attackerBefore?.name ?? "攻方";
  const defenderName = defenderBefore?.name ?? preview.defenderName;

  const result: StrategyBattleResult = {
    attackerWon,
    attackerUnitId: attackerId,
    defenderUnitId: preview.defenderUnitId,
    attackerForceId: attackerBefore?.forceId,
    defenderForceId: defenderBefore?.forceId,
    attackerName,
    defenderName,
    attackerSoldiersBefore: attBefore,
    defenderSoldiersBefore: defBefore,
    attackerCasualties: attLoss,
    defenderCasualties: defLoss,
    attackerSoldiersAfter: attAfter,
    defenderSoldiersAfter: defAfter,
    attackerWinRatePercent: preview.attackerWinRatePercent,
    resolutionSeed: preview.resolutionSeed,
    resolutionRoll: -1,
    engagementKind: "FieldBattle",
    logEntries: buildFallbackBattleLog(
      attackerName,
      defenderName,
      attBefore,
      defBefore,
      attLoss,
      defLoss,
      attAfter,
      defAfter,
      attackerWon,
      preview.attackerWinRatePercent
    ),
  };

  return result;
}

function buildFallbackBattleLog(
  attackerName: string,
  defenderName: string,
  attBefore: number,
  defBefore: number,
  attLoss: number,
  defLoss: number,
  attAfter: number,
  defAfter: number,
  attackerWon: boolean,
  winRate: number
): StrategyBattleLogEntry[] {
  let order = 0;
  const add = (side: StrategyBattleLogEntry["side"], phase: string, message: string) => ({
    order: ++order,
    side,
    phase,
    message,
  });

  return [
    add("system", "接触", `${attackerName} 与 ${defenderName} 在野外遭遇。`),
    add("attacker", "接敌", `${attackerName} 发起进攻（${attBefore} 名）。`),
    add("defender", "接敌", `${defenderName} 列阵应战（${defBefore} 名）。`),
    add("system", "交锋", `战前评估：攻方胜率 ${winRate}%。`),
    attackerWon
      ? add("attacker", "突破", `突破成功，己方 −${attLoss} → 剩余 ${attAfter}。`)
      : add("attacker", "受挫", `攻势受挫，己方 −${attLoss} → 剩余 ${attAfter}。`),
    attackerWon
      ? add("defender", "溃退", `敌军 −${defLoss} → 剩余 ${defAfter}。`)
      : add("defender", "维持", `守军 −${defLoss} → 剩余 ${defAfter}。`),
    add("system", "结束", attackerWon ? "攻方获胜，当日野战结束。" : "守方获胜，当日野战结束。"),
  ];
}

export function displayUnitName(result: StrategyBattleResult, side: "attacker" | "defender"): string {
  if (side === "attacker") {
    return result.attackerName || `#${result.attackerUnitId}`;
  }
  return result.defenderName || `#${result.defenderUnitId}`;
}

function resolveBattleForceIds(
  result: StrategyBattleResult,
  worldState?: StrategyWorldState
): { attackerForceId?: number; defenderForceId?: number } {
  let attackerForceId = result.attackerForceId;
  let defenderForceId = result.defenderForceId;

  if (worldState) {
    if (!attackerForceId) {
      attackerForceId = worldState.units.find((u) => u.id === result.attackerUnitId)?.forceId;
    }
    if (!defenderForceId) {
      defenderForceId = worldState.units.find((u) => u.id === result.defenderUnitId)?.forceId;
    }
  }

  return { attackerForceId, defenderForceId };
}

/** 从当前玩家势力视角判定是否获胜。 */
export function playerWonBattle(
  result: StrategyBattleResult,
  playerForceId: number,
  worldState?: StrategyWorldState
): boolean {
  const { attackerForceId, defenderForceId } = resolveBattleForceIds(result, worldState);

  if (attackerForceId === playerForceId) return result.attackerWon;
  if (defenderForceId === playerForceId) return !result.attackerWon;

  return result.attackerWon;
}

export function battleOutcomeHeadline(
  result: StrategyBattleResult,
  playerForceId: number,
  worldState?: StrategyWorldState
): { text: string; won: boolean } {
  const won = playerWonBattle(result, playerForceId, worldState);
  if (result.isSurrendered) {
    return {
      won,
      text: won ? "🏳 敌军降伏" : "🏳 我军降伏",
    };
  }
  return {
    won,
    text: won ? "⚔ 战斗胜利" : "✖ 战斗失利",
  };
}

export function battleOutcomeBrief(
  result: StrategyBattleResult,
  playerForceId: number,
  worldState?: StrategyWorldState
): string {
  const won = playerWonBattle(result, playerForceId, worldState);
  if (result.isSurrendered) return won ? "降伏" : "投降";
  return won ? "胜利" : "失利";
}
