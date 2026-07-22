import type { AnchorSide, PanelRect, PopupSize } from "@/mapCellAnchor/types";

export abstract class AnchorSidePlacementBehavior {
  abstract readonly side: AnchorSide;
  abstract placement(cell: PanelRect, popup: PopupSize, gap: number): { left: number; top: number };
}

class RightAnchorSidePlacementBehavior extends AnchorSidePlacementBehavior {
  readonly side = "right" as const;

  placement(cell: PanelRect, _popup: PopupSize, gap: number) {
    return { left: cell.left + cell.width + gap, top: cell.top };
  }
}

class LeftAnchorSidePlacementBehavior extends AnchorSidePlacementBehavior {
  readonly side = "left" as const;

  placement(cell: PanelRect, popup: PopupSize, gap: number) {
    return { left: cell.left - popup.width - gap, top: cell.top };
  }
}

class BottomAnchorSidePlacementBehavior extends AnchorSidePlacementBehavior {
  readonly side = "bottom" as const;

  placement(cell: PanelRect, _popup: PopupSize, gap: number) {
    return { left: cell.left, top: cell.top + cell.height + gap };
  }
}

class TopAnchorSidePlacementBehavior extends AnchorSidePlacementBehavior {
  readonly side = "top" as const;

  placement(cell: PanelRect, popup: PopupSize, gap: number) {
    return { left: cell.left, top: cell.top - popup.height - gap };
  }
}

const ANCHOR_SIDE_PLACEMENT_BEHAVIORS: AnchorSidePlacementBehavior[] = [
  new RightAnchorSidePlacementBehavior(),
  new LeftAnchorSidePlacementBehavior(),
  new BottomAnchorSidePlacementBehavior(),
  new TopAnchorSidePlacementBehavior(),
];

export function placementForAnchorSide(
  cell: PanelRect,
  popup: PopupSize,
  gap: number,
  side: AnchorSide,
): { left: number; top: number } {
  return (
    ANCHOR_SIDE_PLACEMENT_BEHAVIORS.find((b) => b.side === side)?.placement(cell, popup, gap) ?? {
      left: cell.left,
      top: cell.top,
    }
  );
}
