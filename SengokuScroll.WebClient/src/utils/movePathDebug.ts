import { ref } from "vue";

export interface MovePathDebugEntry {
  at: string;
  event: string;
  detail: Record<string, unknown>;
}

const MAX_ENTRIES = 80;
export const movePathDebugEntries = ref<MovePathDebugEntry[]>([]);

export function formatPathPoints(
  points: readonly { x: number; y: number }[] | null | undefined
): string {
  if (!points?.length) return "(empty)";
  return points.map((p) => `(${p.x},${p.y})`).join("→");
}

export function logMovePath(event: string, detail: Record<string, unknown>): void {
  const entry: MovePathDebugEntry = {
    at: new Date().toISOString().slice(11, 23),
    event,
    detail,
  };

  movePathDebugEntries.value.unshift(entry);
  if (movePathDebugEntries.value.length > MAX_ENTRIES) {
    movePathDebugEntries.value.length = MAX_ENTRIES;
  }

  if (import.meta.env.DEV) {
    console.debug("[MovePath]", event, detail);
  }
}

export function clearMovePathDebug(): void {
  movePathDebugEntries.value = [];
}

/** 段首是否与期望起点一致（用于检测 API 未 respect from 的情况）。 */
export function pathSegmentStartMismatch(
  segment: readonly { x: number; y: number }[],
  expectedFrom: { x: number; y: number }
): boolean {
  if (!segment.length) return false;
  const first = segment[0]!;
  return first.x !== expectedFrom.x || first.y !== expectedFrom.y;
}
