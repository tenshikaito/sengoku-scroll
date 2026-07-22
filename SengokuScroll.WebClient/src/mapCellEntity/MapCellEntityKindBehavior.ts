import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";

export type MapCellEntityKind = MapCellEntityOption["kind"];

export interface MapCellEntityOpenPayload {
  id: number;
  screenX: number;
  screenY: number;
  panelAnchorRect?: DOMRect;
  anchorSide?: string;
}

export abstract class MapCellEntityKindBehavior {
  abstract readonly kind: MapCellEntityKind;
  abstract readonly icon: string;

  abstract emitSelect(
    emit: (event: string, payload: unknown) => void,
    payload: MapCellEntityOpenPayload,
  ): void;
}

class UnitEntityBehavior extends MapCellEntityKindBehavior {
  readonly kind = "unit" as const;
  readonly icon = "⚔";

  emitSelect(emit: (event: string, payload: unknown) => void, payload: MapCellEntityOpenPayload): void {
    emit("selectUnit", {
      unitId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
      panelAnchorRect: payload.panelAnchorRect,
      anchorSide: payload.anchorSide,
    });
  }
}

class CharacterEntityBehavior extends MapCellEntityKindBehavior {
  readonly kind = "character" as const;
  readonly icon = "👤";

  emitSelect(emit: (event: string, payload: unknown) => void, payload: MapCellEntityOpenPayload): void {
    emit("selectCharacter", {
      characterId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    });
  }
}

class StrongholdEntityBehavior extends MapCellEntityKindBehavior {
  readonly kind = "stronghold" as const;
  readonly icon = "🏯";

  emitSelect(emit: (event: string, payload: unknown) => void, payload: MapCellEntityOpenPayload): void {
    emit("selectStronghold", {
      strongholdId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    });
  }
}

class ConvoyEntityBehavior extends MapCellEntityKindBehavior {
  readonly kind = "convoy" as const;
  readonly icon = "🌾";

  emitSelect(emit: (event: string, payload: unknown) => void, payload: MapCellEntityOpenPayload): void {
    emit("selectConvoy", {
      convoyId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    });
  }
}

const BEHAVIORS: Record<MapCellEntityKind, MapCellEntityKindBehavior> = {
  unit: new UnitEntityBehavior(),
  character: new CharacterEntityBehavior(),
  stronghold: new StrongholdEntityBehavior(),
  convoy: new ConvoyEntityBehavior(),
};

export class MapCellEntityKindBehaviorFactory {
  static create(kind: MapCellEntityKind | string): MapCellEntityKindBehavior {
    return BEHAVIORS[kind as MapCellEntityKind] ?? BEHAVIORS.unit;
  }
}

export function mapCellEntityKindIcon(kind: MapCellEntityKind | string): string {
  return MapCellEntityKindBehaviorFactory.create(kind).icon;
}

export interface MapCellCanvasEmitters {
  selectUnit: (payload: {
    unitId: number;
    screenX: number;
    screenY: number;
    panelAnchorRect?: DOMRect;
    anchorSide?: string;
  }) => void;
  selectCharacter: (payload: { characterId: number; screenX: number; screenY: number }) => void;
  selectStronghold: (payload: { strongholdId: number; screenX: number; screenY: number }) => void;
  selectConvoy: (payload: { convoyId: number; screenX: number; screenY: number }) => void;
}

const CANVAS_EMITTERS: Record<
  MapCellEntityKind,
  (emitters: MapCellCanvasEmitters, payload: MapCellEntityOpenPayload) => void
> = {
  unit: (emitters, payload) =>
    emitters.selectUnit({
      unitId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
      panelAnchorRect: payload.panelAnchorRect,
      anchorSide: payload.anchorSide,
    }),
  character: (emitters, payload) =>
    emitters.selectCharacter({
      characterId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    }),
  stronghold: (emitters, payload) =>
    emitters.selectStronghold({
      strongholdId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    }),
  convoy: (emitters, payload) =>
    emitters.selectConvoy({
      convoyId: payload.id,
      screenX: payload.screenX,
      screenY: payload.screenY,
    }),
};

export function emitMapCellEntityToCanvas(
  kind: MapCellEntityKind,
  emitters: MapCellCanvasEmitters,
  payload: MapCellEntityOpenPayload,
): void {
  CANVAS_EMITTERS[kind]?.(emitters, payload);
}
