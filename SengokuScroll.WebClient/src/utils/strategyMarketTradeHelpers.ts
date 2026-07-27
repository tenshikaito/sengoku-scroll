/** 贸易数量滑块关键点（单位：石；0=尽可能多）。 */
export function buildTradeQuantityKokuStops(maxKoku: number): number[] {
  if (maxKoku <= 0) return [0];

  const maxMarks = 5;
  const raw: number[] = [0];
  for (let index = 1; index < maxMarks - 1; index++) {
    const ratio = index / (maxMarks - 1);
    raw.push(Math.max(1, Math.round(ratio * maxKoku)));
  }
  raw.push(maxKoku);

  const stops: number[] = [];
  for (const value of raw) {
    if (stops.length === 0 || value > stops[stops.length - 1]) {
      stops.push(value);
    }
  }
  return stops;
}

/** 连续滑块刻度（键=石数）。 */
export function buildTradeQuantityKokuMarks(stops: number[]): Record<number, string> {
  return Object.fromEntries(
    stops.map((koku) => [koku, koku === 0 ? "全" : String(koku)]),
  );
}
