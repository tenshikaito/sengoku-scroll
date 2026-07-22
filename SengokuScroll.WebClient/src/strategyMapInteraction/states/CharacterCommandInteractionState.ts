import type { MapHoverCellPayload, StrategyMapInteractionContext } from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { CharacterMoveTargetSelectionInteractionState } from "./CharacterMoveTargetSelectionInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 玩家当主：出城、移动、入城、拜访、谍报等。 */
export class CharacterCommandInteractionState extends StrategyMapInteractionState {
  readonly id = "characterCommand";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "characterCommand" as const;

  override onBeginMove(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    ctx.transitionTo(new CharacterMoveTargetSelectionInteractionState());
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedCharacterId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.transitionTo(new NavigateInteractionState());
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }
}
