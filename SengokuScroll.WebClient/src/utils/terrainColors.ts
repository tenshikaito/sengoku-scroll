/** 地形 Id → Pixi 填色（偶/奇格交替）。 */
export const TERRAIN_FILL_COLORS: Record<number, { even: number; odd: number; stroke: number }> = {
  1: { even: 0x6b8e23, odd: 0x5a7a1e, stroke: 0x3d5229 },
  2: { even: 0x2d5016, odd: 0x234012, stroke: 0x1a3310 },
  3: { even: 0x8b7355, odd: 0x7a6348, stroke: 0x5c4a38 },
  4: { even: 0x3b82c4, odd: 0x2563eb, stroke: 0x1d4ed8 },
  5: { even: 0x6b7280, odd: 0x4b5563, stroke: 0x374151 },
};

const DEFAULT_PALETTE = TERRAIN_FILL_COLORS[1];

export function terrainFillColor(terrainId: number, x: number, y: number): number {
  const palette = TERRAIN_FILL_COLORS[terrainId] ?? DEFAULT_PALETTE;
  return (x + y) % 2 === 0 ? palette.even : palette.odd;
}

export function terrainStrokeColor(terrainId: number): number {
  return (TERRAIN_FILL_COLORS[terrainId] ?? DEFAULT_PALETTE).stroke;
}
