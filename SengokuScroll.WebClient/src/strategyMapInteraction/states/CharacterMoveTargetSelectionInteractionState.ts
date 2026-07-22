import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  StrategyMapInteractionContext,
  StrategyMoveTarget,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { CharacterCommandInteractionState } from "./CharacterCommandInteractionState";
import { NavigateInteractionState } from "./NavigateInteractionState";

/** 当主移动目标选择。 */
export class CharacterMoveTargetSelectionInteractionState extends StrategyMapInteractionState {
  readonly id = "characterMoveTargetSelect";
  readonly mapUnitSelectionEnabled = false;
  readonly mapStrongholdSelectionEnabled = false;
  readonly mapConvoySelectionEnabled = false;
  readonly mapCellSelectionEnabled = true;
  readonly mapRightClickEnabled = true;
  readonly popupMode = "moveSelect" as const;

  override onSelectCell(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCellPayload,
  ): StrategyMoveTarget | null {
    if (!ctx.isValidCharacterMovePathCell(payload.x, payload.y)) return null;

    ctx.setSelectedCell({ x: payload.x, y: payload.y });
    ctx.setMoveTarget({ x: payload.x, y: payload.y });
    return null;
  }

  override onMapRightClick(ctx: StrategyMapInteractionContext): void {
    this.onCancel(ctx);
  }

  override onCancel(ctx: StrategyMapInteractionContext): void {
    this.returnToCommand(ctx);
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
    if (cell && ctx.isValidCharacterMovePathCell(cell.x, cell.y)) {
      ctx.setMoveTarget({ x: cell.x, y: cell.y });
    } else {
      ctx.setMoveTarget(null);
    }
  }

  private returnToCommand(ctx: StrategyMapInteractionContext): void {
    ctx.setMoveTarget(null);
    const characterId = ctx.getSelectedCharacterId();
    const anchor = ctx.getMenuAnchor();
    if (characterId && anchor) {
      const loc = ctx.resolveCharacterLocation(characterId);
      if (loc) ctx.setSelectedCell(loc);
      ctx.transitionTo(new CharacterCommandInteractionState());
      return;
    }
    NavigateInteractionState.resetToNavigate(ctx);
  }
}
