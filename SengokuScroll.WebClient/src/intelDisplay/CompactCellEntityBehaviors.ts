import type {
  StrategyMessengerState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import { formatSoldiers } from "@/utils/strategyDisplayUnits";
import { hoverSoldiersLabel } from "@/utils/strategyIntelDisplay";

export interface IntelFieldRow {
  label: string;
  value: string;
  dev?: boolean;
}

export type CompactCellEntityEntry =
  | { kind: "unit"; key: string; forceId: number; unit: StrategyUnitState }
  | { kind: "convoy"; key: string; forceId: number; convoy: StrategySupplyConvoyState }
  | { kind: "messenger"; key: string; forceId: number; messenger: StrategyMessengerState };

function forceName(worldState: StrategyWorldState, forceId: number): string {
  if (forceId === 0) return "—";
  return worldState.forces.find((f) => f.id === forceId)?.name ?? "未知势力";
}

function dash(value: string | null | undefined): string {
  return value?.trim() ? value : "—";
}

export abstract class CompactCellEntityKindBehavior {
  abstract readonly kind: CompactCellEntityEntry["kind"];
  abstract buildRows(worldState: StrategyWorldState, entry: CompactCellEntityEntry): IntelFieldRow[];
}

class UnitCompactCellEntityBehavior extends CompactCellEntityKindBehavior {
  readonly kind = "unit" as const;

  buildRows(worldState: StrategyWorldState, entry: CompactCellEntityEntry): IntelFieldRow[] {
    if (entry.kind !== "unit") return [];
    return [
      { label: "名称", value: entry.unit.name },
      { label: "势力", value: forceName(worldState, entry.unit.forceId) },
      { label: "将领", value: dash(entry.unit.commanderName) },
      { label: "兵数", value: hoverSoldiersLabel(worldState, entry.unit) },
    ];
  }
}

class ConvoyCompactCellEntityBehavior extends CompactCellEntityKindBehavior {
  readonly kind = "convoy" as const;

  buildRows(worldState: StrategyWorldState, entry: CompactCellEntityEntry): IntelFieldRow[] {
    if (entry.kind !== "convoy") return [];
    return [
      { label: "名称", value: entry.convoy.name },
      { label: "势力", value: forceName(worldState, entry.convoy.forceId) },
      { label: "将领", value: dash(entry.convoy.commanderName) },
      { label: "兵数", value: formatSoldiers(entry.convoy.soldiers) },
    ];
  }
}

class MessengerCompactCellEntityBehavior extends CompactCellEntityKindBehavior {
  readonly kind = "messenger" as const;

  buildRows(worldState: StrategyWorldState, entry: CompactCellEntityEntry): IntelFieldRow[] {
    if (entry.kind !== "messenger") return [];
    return [
      { label: "名称", value: entry.messenger.name },
      { label: "势力", value: forceName(worldState, entry.messenger.forceId) },
      { label: "将领", value: "—" },
      { label: "兵数", value: formatSoldiers(entry.messenger.soldiers) },
    ];
  }
}

const COMPACT_CELL_ENTITY_BEHAVIORS: CompactCellEntityKindBehavior[] = [
  new UnitCompactCellEntityBehavior(),
  new ConvoyCompactCellEntityBehavior(),
  new MessengerCompactCellEntityBehavior(),
];

export function compactCellEntityRows(
  worldState: StrategyWorldState,
  entry: CompactCellEntityEntry,
): IntelFieldRow[] {
  return COMPACT_CELL_ENTITY_BEHAVIORS.find((b) => b.kind === entry.kind)?.buildRows(worldState, entry) ?? [];
}
