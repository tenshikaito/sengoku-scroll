import type { StrategyStrongholdState, StrategyUnitState, StrategyWorldState } from "@/api/strategy";

export interface IntelFieldRow {
  label: string;
  value: string;
}

export interface StrongholdGarrisonIntelContext {
  worldState: StrategyWorldState;
  stronghold: StrategyStrongholdState;
  cityGarrison: number;
  includeBattleIntel: boolean;
  formatSoldiers: (value: number) => string;
  strongholdHoverFieldValue: (
    worldState: StrategyWorldState,
    stronghold: StrategyStrongholdState,
    label: string,
    fallback: string,
  ) => string;
  cellEnemyUnitRows: (
    worldState: StrategyWorldState,
    x: number,
    y: number,
    ownForceId: number,
  ) => IntelFieldRow[];
  battlefieldIntelRows: (
    battlefield: NonNullable<StrategyWorldState["battlefields"]>[number],
    playerForceId: number,
  ) => IntelFieldRow[];
  findBattlefieldAtCell: (
    worldState: StrategyWorldState,
    x: number,
    y: number,
  ) => NonNullable<StrategyWorldState["battlefields"]>[number] | null;
}

export abstract class StrongholdGarrisonIntelBehavior {
  abstract readonly includeBattleIntel: boolean;

  abstract buildRows(ctx: StrongholdGarrisonIntelContext): IntelFieldRow[];
}

class DetailedStrongholdGarrisonIntelBehavior extends StrongholdGarrisonIntelBehavior {
  readonly includeBattleIntel = true;

  buildRows(ctx: StrongholdGarrisonIntelContext): IntelFieldRow[] {
    const { worldState, stronghold, cityGarrison } = ctx;
    const rows: IntelFieldRow[] = [];
    const fieldGarrison = worldState.units.filter(
      (u: StrategyUnitState) =>
        u.x === stronghold.x && u.y === stronghold.y && u.forceId === stronghold.forceId,
    );

    if (cityGarrison > 0) {
      rows.push({ label: "城内兵", value: ctx.formatSoldiers(cityGarrison) });
    }

    const garrisonParts = fieldGarrison.map(
      (u) => `${u.name}（${ctx.formatSoldiers(u.soldiers)}）`,
    );
    if (garrisonParts.length) {
      rows.push({ label: "地图驻军", value: garrisonParts.join("、") });
    } else if (cityGarrison <= 0) {
      rows.push({ label: "兵力", value: "无" });
    }

    rows.push(...ctx.cellEnemyUnitRows(worldState, stronghold.x, stronghold.y, stronghold.forceId));

    const battlefield = ctx.findBattlefieldAtCell(worldState, stronghold.x, stronghold.y);
    if (battlefield) {
      rows.push(...ctx.battlefieldIntelRows(battlefield, worldState.playerForceId));
    }

    return rows;
  }
}

class SummaryStrongholdGarrisonIntelBehavior extends StrongholdGarrisonIntelBehavior {
  readonly includeBattleIntel = false;

  buildRows(ctx: StrongholdGarrisonIntelContext): IntelFieldRow[] {
    const { worldState, stronghold, cityGarrison } = ctx;
    if (cityGarrison > 0) {
      return [
        {
          label: "兵力",
          value: ctx.strongholdHoverFieldValue(
            worldState,
            stronghold,
            "兵力",
            ctx.formatSoldiers(cityGarrison),
          ),
        },
      ];
    }

    return [
      {
        label: "兵力",
        value: ctx.strongholdHoverFieldValue(worldState, stronghold, "兵力", "无"),
      },
    ];
  }
}

const STRONGHOLD_GARRISON_INTEL_BEHAVIORS: StrongholdGarrisonIntelBehavior[] = [
  new DetailedStrongholdGarrisonIntelBehavior(),
  new SummaryStrongholdGarrisonIntelBehavior(),
];

export function buildStrongholdGarrisonIntelRows(
  ctx: StrongholdGarrisonIntelContext,
): IntelFieldRow[] {
  const behavior =
    STRONGHOLD_GARRISON_INTEL_BEHAVIORS.find((b) => b.includeBattleIntel === ctx.includeBattleIntel)
    ?? new SummaryStrongholdGarrisonIntelBehavior();
  return behavior.buildRows(ctx);
}
