import type { StrategyWorldState } from "@/api/strategy";

export interface IntelFieldRow {
  label: string;
  value: string;
}

export abstract class IntelCultureReligionFollowRowBehavior {
  abstract readonly sourceLabel: string;
  abstract readonly followLabel: string;

  abstract resolveFollowValue(worldState: StrategyWorldState, value: string): string;
}

class CultureFollowRowBehavior extends IntelCultureReligionFollowRowBehavior {
  readonly sourceLabel = "文化";
  readonly followLabel = "文化圈";

  resolveFollowValue(worldState: StrategyWorldState, value: string): string {
    const entry = worldState.masterData?.cultures?.find((c) => c.name === value);
    return entry?.group?.trim() || "—";
  }
}

class ReligionFollowRowBehavior extends IntelCultureReligionFollowRowBehavior {
  readonly sourceLabel = "信仰";
  readonly followLabel = "宗教";

  resolveFollowValue(worldState: StrategyWorldState, value: string): string {
    const entry = worldState.masterData?.religions?.find((r) => r.name === value);
    return entry?.group?.trim() || "—";
  }
}

const FOLLOW_ROW_BEHAVIORS: IntelCultureReligionFollowRowBehavior[] = [
  new CultureFollowRowBehavior(),
  new ReligionFollowRowBehavior(),
];

export function enrichCultureReligionGroupRows(
  worldState: StrategyWorldState,
  rows: IntelFieldRow[],
): IntelFieldRow[] {
  const result: IntelFieldRow[] = [];
  for (const row of rows) {
    result.push(row);
    const behavior = FOLLOW_ROW_BEHAVIORS.find((b) => b.sourceLabel === row.label);
    if (behavior) {
      result.push({
        label: behavior.followLabel,
        value: behavior.resolveFollowValue(worldState, row.value),
      });
    }
  }
  return result;
}
