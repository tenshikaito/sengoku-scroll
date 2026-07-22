import type { StrategyBattlefieldState } from "@/api/strategy";
import { formatSiegeSoldiers, formatSoldiers } from "@/utils/strategyDisplayUnits";
import { siegeThreatLabel } from "@/intelDisplay/IntelDisplayBehaviors";

export interface IntelFieldRow {
  label: string;
  value: string;
}

export abstract class BattlefieldKindPresentationBehavior {
  abstract readonly kind: string;
  abstract readonly mapMarkerLabel: string;

  abstract buildStatusRows(battlefield: StrategyBattlefieldState): IntelFieldRow[];

  abstract formatParticipantSoldiers(
    soldiers: number,
    forceId: number,
    forceName: string,
    playerForceId: number,
  ): string;

  aggressorSoldierDisplayTotal(_battlefield: StrategyBattlefieldState): number {
    return 0;
  }
}

class SiegeBattlefieldPresentationBehavior extends BattlefieldKindPresentationBehavior {
  readonly kind = "Siege";
  readonly mapMarkerLabel = "围";

  buildStatusRows(battlefield: StrategyBattlefieldState): IntelFieldRow[] {
    const rows: IntelFieldRow[] = [
      { label: "攻城", value: siegeThreatLabel(battlefield.siegeThreat) },
    ];
    if (battlefield.standoffDays > 0) {
      rows.push({ label: "持续", value: `${battlefield.standoffDays} 日` });
    }
    return rows;
  }

  formatParticipantSoldiers(
    soldiers: number,
    forceId: number,
    forceName: string,
    playerForceId: number,
  ): string {
    return `${forceName} ${formatSiegeSoldiers(soldiers, forceId, playerForceId)}`;
  }

  override aggressorSoldierDisplayTotal(battlefield: StrategyBattlefieldState): number {
    return battlefield.aggressorSoldierTotal;
  }
}

class FieldBattlefieldPresentationBehavior extends BattlefieldKindPresentationBehavior {
  readonly kind = "Field";
  readonly mapMarkerLabel = "战";

  buildStatusRows(battlefield: StrategyBattlefieldState): IntelFieldRow[] {
    return [
      {
        label: "对峙",
        value: battlefield.standoffDays > 0 ? `${battlefield.standoffDays} 日` : "当日",
      },
    ];
  }

  formatParticipantSoldiers(
    soldiers: number,
    _forceId: number,
    forceName: string,
    _playerForceId: number,
  ): string {
    return `${forceName} ${formatSoldiers(soldiers)}`;
  }
}

const BATTLEFIELD_PRESENTATION_BEHAVIORS: BattlefieldKindPresentationBehavior[] = [
  new SiegeBattlefieldPresentationBehavior(),
  new FieldBattlefieldPresentationBehavior(),
];

const DEFAULT_BATTLEFIELD_PRESENTATION = new FieldBattlefieldPresentationBehavior();

export function resolveBattlefieldPresentation(
  kind: string | undefined | null,
): BattlefieldKindPresentationBehavior {
  return BATTLEFIELD_PRESENTATION_BEHAVIORS.find((b) => b.kind === kind) ?? DEFAULT_BATTLEFIELD_PRESENTATION;
}

export function battlefieldMapMarkerLabel(kind: string | undefined | null): string {
  return resolveBattlefieldPresentation(kind).mapMarkerLabel;
}

export function battlefieldAggressorSoldierDisplayTotal(
  battlefield: StrategyBattlefieldState,
): number {
  return resolveBattlefieldPresentation(battlefield.kind).aggressorSoldierDisplayTotal(battlefield);
}

export function buildBattlefieldStatusRows(
  battlefield: StrategyBattlefieldState,
): IntelFieldRow[] {
  return resolveBattlefieldPresentation(battlefield.kind).buildStatusRows(battlefield);
}

export function formatBattlefieldParticipantSoldiers(
  battlefield: StrategyBattlefieldState,
  forceName: string,
  soldiers: number,
  forceId: number,
  playerForceId: number,
): string {
  return resolveBattlefieldPresentation(battlefield.kind).formatParticipantSoldiers(
    soldiers,
    forceId,
    forceName,
    playerForceId,
  );
}
