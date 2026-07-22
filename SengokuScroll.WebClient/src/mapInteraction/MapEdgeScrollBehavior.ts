export interface EdgeScrollVelocityInput {
  clientX: number;
  clientY: number;
  rectLeft: number;
  rectTop: number;
  rectRight: number;
  rectBottom: number;
  rectWidth: number;
  rectHeight: number;
  edgeZonePx: number;
  baseSpeed: number;
}

export abstract class MapEdgeScrollAxisBehavior {
  abstract readonly axis: "x" | "y";

  abstract resolveVelocity(input: EdgeScrollVelocityInput): number;
}

class LeftEdgeScrollBehavior extends MapEdgeScrollAxisBehavior {
  readonly axis = "x" as const;

  resolveVelocity(input: EdgeScrollVelocityInput): number {
    if (input.clientX < input.rectLeft || input.clientX - input.rectLeft < input.edgeZonePx) {
      return input.baseSpeed;
    }
    if (input.clientX > input.rectRight || input.clientX - input.rectLeft >= input.rectWidth - input.edgeZonePx) {
      return -input.baseSpeed;
    }
    return 0;
  }
}

class TopEdgeScrollBehavior extends MapEdgeScrollAxisBehavior {
  readonly axis = "y" as const;

  resolveVelocity(input: EdgeScrollVelocityInput): number {
    if (input.clientY < input.rectTop || input.clientY - input.rectTop < input.edgeZonePx) {
      return input.baseSpeed;
    }
    if (input.clientY > input.rectBottom || input.clientY - input.rectTop >= input.rectHeight - input.edgeZonePx) {
      return -input.baseSpeed;
    }
    return 0;
  }
}

const EDGE_SCROLL_AXIS_BEHAVIORS: MapEdgeScrollAxisBehavior[] = [
  new LeftEdgeScrollBehavior(),
  new TopEdgeScrollBehavior(),
];

export function resolveMapEdgeScrollVelocity(
  input: EdgeScrollVelocityInput,
): { dx: number; dy: number } {
  const xBehavior = EDGE_SCROLL_AXIS_BEHAVIORS.find((b) => b.axis === "x");
  const yBehavior = EDGE_SCROLL_AXIS_BEHAVIORS.find((b) => b.axis === "y");
  return {
    dx: xBehavior?.resolveVelocity(input) ?? 0,
    dy: yBehavior?.resolveVelocity(input) ?? 0,
  };
}
