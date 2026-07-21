import { enumLabel, t } from "@/i18n/textLocalizer";
import type { StrategyInTransitSupply } from "@/api/strategyTypes";

export function supplyStatusLabel(status: string | null | undefined): string {
  return enumLabel("enum.supply.status", status, t("common.emDash"));
}

export function formatInTransitSupplies(
  supplies: StrategyInTransitSupply[] | null | undefined
): string {
  if (!supplies?.length) return t("common.none");

  return supplies
    .map((s) => {
      const origin = s.originStrongholdName
        ? t("logistics.supply.fromOrigin", { origin: s.originStrongholdName })
        : "";
      const deceived = s.isDeceived ? t("logistics.supply.deceived") : "";
      return t("logistics.supply.inTransit", {
        cargo: s.cargoFoodGo,
        days: s.estimatedDays,
        origin,
        deceived,
      });
    })
    .join("；");
}
