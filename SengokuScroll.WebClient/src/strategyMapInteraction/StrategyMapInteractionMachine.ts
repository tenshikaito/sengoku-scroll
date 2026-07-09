import type {
  MapHoverCellPayload,
  MapSelectCellPayload,
  MapSelectUnitPayload,
  StrategyMapInteractionContext,
  StrategyMapInteractionStateSnapshot,
  StrategyMoveTarget,
} from "./types";
import { StrategyMapInteractionState } from "./StrategyMapInteractionState";
import { ExecutingCommandInteractionState } from "./states/ExecutingCommandInteractionState";
import { NavigateInteractionState } from "./states/NavigateInteractionState";

export type StrategyMapInteractionListener = (snapshot: StrategyMapInteractionStateSnapshot) => void;

/** 策略地图交互状态机：根组件只调用公开方法，行为由当前状态子类决定。 */
export class StrategyMapInteractionMachine {
  private current: StrategyMapInteractionState;
  private readonly listeners = new Set<StrategyMapInteractionListener>();

  constructor(private readonly ctx: StrategyMapInteractionContext) {
    this.current = new NavigateInteractionState();
    this.emitSnapshot();
  }

  get snapshot(): StrategyMapInteractionStateSnapshot {
    return this.current.toSnapshot(this.ctx);
  }

  subscribe(listener: StrategyMapInteractionListener): () => void {
    this.listeners.add(listener);
    listener(this.snapshot);
    return () => this.listeners.delete(listener);
  }

  transitionTo(state: StrategyMapInteractionState): void {
    this.current = state;
    this.emitSnapshot();
  }

  reset(): void {
    this.current.onReset(this.ctx);
    this.current = new NavigateInteractionState();
    this.emitSnapshot();
  }

  onSelectUnit(payload: MapSelectUnitPayload): void {
    this.current.onSelectUnit(this.ctx, payload);
  }

  onSelectStronghold(payload: import("./types").MapSelectStrongholdPayload): void {
    this.current.onSelectStronghold(this.ctx, payload);
  }

  onSelectConvoy(payload: import("./types").MapSelectConvoyPayload): void {
    this.current.onSelectConvoy(this.ctx, payload);
  }

  /** @returns 非 null 时父组件应调用移动 API */
  onSelectCell(payload: MapSelectCellPayload): StrategyMoveTarget | null {
    return this.current.onSelectCell(this.ctx, payload);
  }

  onHoverCell(cell: MapHoverCellPayload): void {
    this.current.onHoverCell(this.ctx, cell);
  }

  onMapRightClick(): void {
    this.current.onMapRightClick(this.ctx);
  }

  onBeginMove(): void {
    this.current.onBeginMove(this.ctx);
  }

  onBeginAttack(): void {
    this.current.onBeginAttack(this.ctx);
  }

  onShowIntel(): void {
    this.current.onShowIntel(this.ctx);
  }

  onConfirmBattle(): StrategyMoveTarget | null {
    const target = this.ctx.getLockedCommand();
    this.current.onConfirmBattle(this.ctx);
    return target;
  }

  onBattlePreviewReady(): void {
    this.current.onBattlePreviewReady(this.ctx);
  }

  onCancel(): void {
    this.current.onCancel(this.ctx);
  }

  onMoveSucceeded(): void {
    this.current.onMoveSucceeded(this.ctx);
  }

  onMoveFailed(target: StrategyMoveTarget): void {
    this.current.onMoveFailed(this.ctx, target);
  }

  onBattleSucceeded(): void {
    this.current.onBattleSucceeded(this.ctx);
  }

  onBattleFailed(target: StrategyMoveTarget): void {
    this.current.onBattleFailed(this.ctx, target);
  }

  enterExecutingCommand(): void {
    this.transitionTo(new ExecutingCommandInteractionState());
  }

  private emitSnapshot(): void {
    const snap = this.snapshot;
    for (const listener of this.listeners) listener(snap);
  }
}

export { StrategyMapInteractionState } from "./StrategyMapInteractionState";
export { NavigateInteractionState } from "./states/NavigateInteractionState";
export { UnitCommandInteractionState } from "./states/UnitCommandInteractionState";
export { StrongholdCommandInteractionState } from "./states/StrongholdCommandInteractionState";
export { ForeignStrongholdCommandInteractionState } from "./states/ForeignStrongholdCommandInteractionState";
export { ForeignUnitCommandInteractionState } from "./states/ForeignUnitCommandInteractionState";
export { AttackTargetSelectionInteractionState } from "./states/AttackTargetSelectionInteractionState";
export { MoveTargetSelectionInteractionState } from "./states/MoveTargetSelectionInteractionState";
export { ExecutingCommandInteractionState } from "./states/ExecutingCommandInteractionState";
export type * from "./types";
