import type { MapPoint } from "@/api/strategyTypes";

export type { MapPoint };

export function concatPathSegments(segments: MapPoint[][]): MapPoint[] {
  const merged: MapPoint[] = [];
  for (const segment of segments) {
    if (!segment.length) continue;
    if (!merged.length) {
      merged.push(...segment);
      continue;
    }
    const last = merged[merged.length - 1]!;
    const joinIdx = segment.findIndex((p) => p.x === last.x && p.y === last.y);
    if (joinIdx >= 0) {
      merged.push(...segment.slice(joinIdx + 1));
      continue;
    }
    const first = segment[0]!;
    merged.push(...(last.x === first.x && last.y === first.y ? segment.slice(1) : segment));
  }
  return merged;
}

/** 丢弃段首直到出现指定起点（API 忽略 from 时的兜底）。 */
export function trimPathSegmentFrom(path: MapPoint[], from: MapPoint): MapPoint[] {
  if (!path.length) return path;
  const startIdx = path.findIndex((p) => p.x === from.x && p.y === from.y);
  return startIdx >= 0 ? path.slice(startIdx) : path;
}

/** 4 方向曼哈顿路径（Mock 与离线预览回退）。 */
export function buildManhattanPath(fromX: number, fromY: number, toX: number, toY: number): MapPoint[] {
  if (fromX === toX && fromY === toY) return [{ x: fromX, y: fromY }];

  const points: MapPoint[] = [{ x: fromX, y: fromY }];
  let x = fromX;
  let y = fromY;

  while (x !== toX || y !== toY) {
    if (x !== toX) x += Math.sign(toX - x);
    else if (y !== toY) y += Math.sign(toY - y);
    points.push({ x, y });
  }

  return points;
}

export function mapPointsEqual(a: MapPoint[], b: MapPoint[]): boolean {
  if (a.length !== b.length) return false;
  return a.every((p, i) => p.x === b[i]!.x && p.y === b[i]!.y);
}
