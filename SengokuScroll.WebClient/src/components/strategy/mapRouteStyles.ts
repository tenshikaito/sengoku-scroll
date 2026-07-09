export type MapRouteVariant = "preview" | "committed" | "emphasized";

export interface MapRouteOverlay {
  unitId: number;
  points: { x: number; y: number }[];
  variant: MapRouteVariant;
}

/** 移动规划中的用户中继点（committed=已确认段终点，pending=当前待确认终点）。 */
export type MapMoveRelayMarkerKind = "committed" | "pending";

export interface MapMoveRelayMarker {
  x: number;
  y: number;
  kind: MapMoveRelayMarkerKind;
  /** 从 1 起的中继序号（pending 为最后一段）。 */
  order: number;
}

export const MOVE_RELAY_MARKER_STYLES: Record<
  MapMoveRelayMarkerKind,
  { fill: number; stroke: number; label: string | null }
> = {
  committed: { fill: 0xf97316, stroke: 0xfff7ed, label: null },
  pending: { fill: 0x22d3ee, stroke: 0xecfeff, label: "终" },
};

export const ROUTE_STYLES: Record<
  MapRouteVariant,
  { fill: number; fillAlpha: number; stroke: number; strokeAlpha: number; width: number }
> = {
  preview: { fill: 0x38bdf8, fillAlpha: 0.38, stroke: 0x7dd3fc, strokeAlpha: 0.95, width: 2 },
  committed: { fill: 0x64748b, fillAlpha: 0.28, stroke: 0x94a3b8, strokeAlpha: 0.75, width: 1.5 },
  emphasized: { fill: 0xfbbf24, fillAlpha: 0.42, stroke: 0xfde68a, strokeAlpha: 0.98, width: 2.5 },
};
