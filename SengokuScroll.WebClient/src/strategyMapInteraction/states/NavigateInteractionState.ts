import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";
import type {
  MapHoverCellPayload,
  MapSelectCellEntitiesPayload,
  MapSelectCharacterPayload,
  MapSelectConvoyPayload,
  MapSelectStrongholdPayload,
  MapSelectUnitPayload,
  StrategyMapInteractionContext,
  StrategyMenuAnchor,
} from "../types";
import { StrategyMapInteractionState } from "../StrategyMapInteractionState";
import { CellEntityPickerInteractionState } from "./CellEntityPickerInteractionState";
import { CharacterCommandInteractionState } from "./CharacterCommandInteractionState";
import { ConvoyCommandInteractionState } from "./ConvoyCommandInteractionState";
import { ForeignStrongholdCommandInteractionState } from "./ForeignStrongholdCommandInteractionState";
import { ForeignUnitCommandInteractionState } from "./ForeignUnitCommandInteractionState";
import { StrongholdCommandInteractionState } from "./StrongholdCommandInteractionState";
import { UnitCommandInteractionState } from "./UnitCommandInteractionState";

/** 大地图浏览：点击单位/据点/角色打开对应菜单。 */
export class NavigateInteractionState extends StrategyMapInteractionState {
  readonly id = "navigate";
  readonly mapUnitSelectionEnabled = true;
  readonly mapStrongholdSelectionEnabled = true;
  readonly mapConvoySelectionEnabled = true;
  readonly mapCellSelectionEnabled = false;
  readonly mapRightClickEnabled = false;
  readonly popupMode = "none" as const;

  static resetToNavigate(ctx: StrategyMapInteractionContext): void {
    ctx.setSelectedUnitId(null);
    ctx.setSelectedCharacterId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedCell(null);
    ctx.setMenuAnchor(null);
    ctx.setMoveTarget(null);
    ctx.setCellEntityOptions([]);
    ctx.transitionTo(new NavigateInteractionState());
  }

  static setMenuAnchorFromScreen(
    ctx: StrategyMapInteractionContext,
    x: number,
    y: number,
    screenX: number,
    screenY: number,
    extra?: Partial<StrategyMenuAnchor>,
  ): void {
    ctx.setSelectedCell({ x, y });
    ctx.setMenuAnchor({
      x,
      y,
      screenX,
      screenY,
      ...extra,
    });
  }

  static openUnit(ctx: StrategyMapInteractionContext, payload: MapSelectUnitPayload): void {
    const location = ctx.resolveUnitLocation(payload.unitId);
    if (!location) return;

    ctx.setSelectedCharacterId(null);
    ctx.setSelectedUnitId(payload.unitId);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedStrongholdId(ctx.resolveStrongholdAtCell(location.x, location.y));
    ctx.setMoveTarget(null);
    NavigateInteractionState.setMenuAnchorFromScreen(
      ctx,
      location.x,
      location.y,
      payload.screenX,
      payload.screenY,
      {
        panelAnchorRect: payload.panelAnchorRect,
        anchorSide: payload.anchorSide,
      },
    );
    ctx.setCellEntityOptions([]);

    ctx.transitionTo(
      ctx.isPlayerUnit(payload.unitId)
        ? new UnitCommandInteractionState()
        : new ForeignUnitCommandInteractionState(),
    );
  }

  static openCharacter(ctx: StrategyMapInteractionContext, payload: MapSelectCharacterPayload): void {
    const location = ctx.resolveCharacterLocation(payload.characterId);
    if (!location) return;

    ctx.setSelectedCharacterId(payload.characterId);
    ctx.setSelectedUnitId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedStrongholdId(ctx.resolveStrongholdAtCell(location.x, location.y));
    ctx.setMoveTarget(null);
    NavigateInteractionState.setMenuAnchorFromScreen(
      ctx,
      location.x,
      location.y,
      payload.screenX,
      payload.screenY,
    );
    ctx.setCellEntityOptions([]);
    ctx.transitionTo(new CharacterCommandInteractionState());
  }

  static openStronghold(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectStrongholdPayload,
  ): void {
    const location = ctx.resolveStrongholdLocation(payload.strongholdId);
    if (!location) return;

    ctx.setSelectedUnitId(null);
    ctx.setSelectedCharacterId(null);
    ctx.setSelectedConvoyId(null);
    ctx.setSelectedStrongholdId(payload.strongholdId);
    ctx.setMoveTarget(null);
    NavigateInteractionState.setMenuAnchorFromScreen(
      ctx,
      location.x,
      location.y,
      payload.screenX,
      payload.screenY,
    );
    ctx.setCellEntityOptions([]);

    ctx.transitionTo(
      ctx.isPlayerStronghold(payload.strongholdId)
        ? new StrongholdCommandInteractionState()
        : new ForeignStrongholdCommandInteractionState(),
    );
  }

  static openConvoy(ctx: StrategyMapInteractionContext, payload: MapSelectConvoyPayload): void {
    if (!ctx.isPlayerConvoy(payload.convoyId)) return;

    const location = ctx.resolveConvoyLocation(payload.convoyId);
    if (!location) return;

    ctx.setSelectedUnitId(null);
    ctx.setSelectedCharacterId(null);
    ctx.setSelectedStrongholdId(null);
    ctx.setSelectedConvoyId(payload.convoyId);
    ctx.setMoveTarget(null);
    NavigateInteractionState.setMenuAnchorFromScreen(
      ctx,
      location.x,
      location.y,
      payload.screenX,
      payload.screenY,
    );
    ctx.setCellEntityOptions([]);
    ctx.transitionTo(new ConvoyCommandInteractionState());
  }

  static openCellEntity(ctx: StrategyMapInteractionContext, entity: MapCellEntityOption): void {
    const anchor = ctx.getMenuAnchor();
    if (!anchor) return;

    switch (entity.kind) {
      case "unit":
        NavigateInteractionState.openUnit(ctx, {
          unitId: entity.id,
          screenX: anchor.screenX,
          screenY: anchor.screenY,
          panelAnchorRect: anchor.panelAnchorRect,
          anchorSide: anchor.anchorSide,
        });
        break;
      case "character":
        NavigateInteractionState.openCharacter(ctx, {
          characterId: entity.id,
          screenX: anchor.screenX,
          screenY: anchor.screenY,
        });
        break;
      case "stronghold":
        NavigateInteractionState.openStronghold(ctx, {
          strongholdId: entity.id,
          screenX: anchor.screenX,
          screenY: anchor.screenY,
        });
        break;
      case "convoy":
        NavigateInteractionState.openConvoy(ctx, {
          convoyId: entity.id,
          screenX: anchor.screenX,
          screenY: anchor.screenY,
        });
        break;
    }
  }

  override onSelectUnit(ctx: StrategyMapInteractionContext, payload: MapSelectUnitPayload): void {
    NavigateInteractionState.openUnit(ctx, payload);
  }

  override onSelectCharacter(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCharacterPayload,
  ): void {
    NavigateInteractionState.openCharacter(ctx, payload);
  }

  override onSelectStronghold(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectStrongholdPayload,
  ): void {
    NavigateInteractionState.openStronghold(ctx, payload);
  }

  override onSelectConvoy(ctx: StrategyMapInteractionContext, payload: MapSelectConvoyPayload): void {
    NavigateInteractionState.openConvoy(ctx, payload);
  }

  override onSelectCellEntities(
    ctx: StrategyMapInteractionContext,
    payload: MapSelectCellEntitiesPayload,
    entities: readonly MapCellEntityOption[],
  ): void {
    NavigateInteractionState.setMenuAnchorFromScreen(
      ctx,
      payload.x,
      payload.y,
      payload.screenX,
      payload.screenY,
    );
    ctx.setCellEntityOptions(entities);
    ctx.transitionTo(new CellEntityPickerInteractionState());
  }

  override onHoverCell(ctx: StrategyMapInteractionContext, cell: MapHoverCellPayload): void {
    ctx.setHoverCell(cell);
  }

  override onReset(ctx: StrategyMapInteractionContext): void {
    NavigateInteractionState.resetToNavigate(ctx);
  }
}
