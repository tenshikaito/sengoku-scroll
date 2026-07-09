import type { MapPoint } from "@/api/strategy";
import {
  formatPathPoints,
  logMovePath,
  pathSegmentStartMismatch,
} from "@/utils/movePathDebug";

export function pointsEqual(a: MapPoint, b: MapPoint): boolean {
  return a.x === b.x && a.y === b.y;
}

export function findPointOnPath(path: readonly MapPoint[], point: MapPoint): number {
  return path.findIndex((p) => pointsEqual(p, point));
}

/** 用户点击过的中继序列（不含单位起点）。 */
export function listMoveRelays(committed: readonly MapPoint[], pending: MapPoint | null): MapPoint[] {
  return pending ? [...committed, pending] : [...committed];
}

/**
 * 点击已在预览路径上的格子：截断路径，并将该格设为新的 pending 终点。
 * committed 仅保留仍位于截断后路径上、且排在 clicked 之前的用户中继。
 */
export function truncateMovePathAtCell(
  committed: readonly MapPoint[],
  pending: MapPoint | null,
  previewPath: readonly MapPoint[],
  clicked: MapPoint
): { committed: MapPoint[]; pending: MapPoint; previewPath: MapPoint[] } | null {
  const clickedIdx = findPointOnPath(previewPath, clicked);
  if (clickedIdx < 0) return null;

  logMovePath("truncate.enter", {
    clicked,
    clickedIdx,
    committed: [...committed],
    pending,
    previewPath: formatPathPoints(previewPath),
  });

  const truncated = previewPath.slice(0, clickedIdx + 1).map((p) => ({ ...p }));
  const relays = listMoveRelays(committed, pending);

  const newCommitted: MapPoint[] = [];
  for (const relay of relays) {
    if (pointsEqual(relay, clicked)) break;
    const relayIdx = findPointOnPath(truncated, relay);
    if (relayIdx >= 0 && relayIdx < clickedIdx) {
      newCommitted.push({ ...relay });
    }
  }

  const result = {
    committed: newCommitted,
    pending: { ...clicked },
    previewPath: truncated,
  };

  logMovePath("truncate.result", {
    committed: result.committed,
    pending: result.pending,
    previewPath: formatPathPoints(result.previewPath),
  });

  return result;
}

/** 将新段拼接到已有预览路径之后（from 为上一中继或单位位置）。 */
export function appendPathSegment(
  basePath: readonly MapPoint[],
  from: MapPoint,
  segment: readonly MapPoint[]
): MapPoint[] {
  if (!basePath.length) {
    const out = segment.map((p) => ({ ...p }));
    logMovePath("append.firstSegment", {
      from,
      segment: formatPathPoints(segment),
      result: formatPathPoints(out),
    });
    return out;
  }

  const fromIdx = findPointOnPath(basePath, from);
  const prefix = fromIdx >= 0 ? basePath.slice(0, fromIdx + 1) : [...basePath];

  let segmentStart = 0;
  if (segment.length > 0) {
    const last = prefix[prefix.length - 1]!;
    const joinIdx = segment.findIndex((p) => pointsEqual(p, last));
    segmentStart = joinIdx >= 0 ? joinIdx + 1 : 0;
    if (segmentStart === 0 && pointsEqual(segment[0]!, last)) {
      segmentStart = 1;
    }
  }

  const out = [...prefix.map((p) => ({ ...p })), ...segment.slice(segmentStart).map((p) => ({ ...p }))];

  const prefixLast = prefix[prefix.length - 1];
  const appendedFirst = segment.slice(segmentStart)[0];
  logMovePath("append.concat", {
    from,
    fromIdx,
    basePath: formatPathPoints(basePath),
    prefix: formatPathPoints(prefix),
    segment: formatPathPoints(segment),
    segmentStart,
    prefixLast,
    appendedFirst: appendedFirst ?? null,
    segmentStartMismatch: appendedFirst && prefixLast
      ? pathSegmentStartMismatch([appendedFirst], prefixLast)
      : false,
    jumpBackToUnit:
      appendedFirst &&
      basePath[0] &&
      pointsEqual(appendedFirst, basePath[0]!) &&
      prefixLast &&
      !pointsEqual(appendedFirst, prefixLast),
    result: formatPathPoints(out),
  });

  return out;
}
