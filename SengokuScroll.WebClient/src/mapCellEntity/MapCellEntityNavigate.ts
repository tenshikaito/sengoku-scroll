import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";
import type { StrategyMapInteractionContext } from "@/strategyMapInteraction/types";
import { NavigateInteractionState } from "@/strategyMapInteraction/states/NavigateInteractionState";
import type { MapCellEntityKind } from "@/mapCellEntity/MapCellEntityKindBehavior";

type NavigateEntityOpener = (
  ctx: StrategyMapInteractionContext,
  entity: MapCellEntityOption,
  anchor: NonNullable<ReturnType<StrategyMapInteractionContext["getMenuAnchor"]>>,
) => void;

const NAVIGATE_OPENERS: Record<MapCellEntityKind, NavigateEntityOpener> = {
  unit: (ctx, entity, anchor) => {
    NavigateInteractionState.openUnit(ctx, {
      unitId: entity.id,
      screenX: anchor.screenX,
      screenY: anchor.screenY,
      panelAnchorRect: anchor.panelAnchorRect,
      anchorSide: anchor.anchorSide,
    });
  },
  character: (ctx, entity, anchor) => {
    NavigateInteractionState.openCharacter(ctx, {
      characterId: entity.id,
      screenX: anchor.screenX,
      screenY: anchor.screenY,
    });
  },
  stronghold: (ctx, entity, anchor) => {
    NavigateInteractionState.openStronghold(ctx, {
      strongholdId: entity.id,
      screenX: anchor.screenX,
      screenY: anchor.screenY,
    });
  },
  convoy: (ctx, entity, anchor) => {
    NavigateInteractionState.openConvoy(ctx, {
      convoyId: entity.id,
      screenX: anchor.screenX,
      screenY: anchor.screenY,
    });
  },
};

export function openMapCellEntityInNavigate(
  ctx: StrategyMapInteractionContext,
  entity: MapCellEntityOption,
): void {
  const anchor = ctx.getMenuAnchor();
  if (!anchor) return;

  const opener = NAVIGATE_OPENERS[entity.kind];
  opener?.(ctx, entity, anchor);
}
