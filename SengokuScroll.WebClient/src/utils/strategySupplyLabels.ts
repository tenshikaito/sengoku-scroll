import type { StrategyInTransitSupply } from "@/api/strategyTypes";

const SUPPLY_STATUS_LABEL: Record<string, string> = {
  Sufficient: "充足",
  Strained: "紧张",
  CutOff: "断绝",
};

export function supplyStatusLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return SUPPLY_STATUS_LABEL[status] ?? status;
}

export function formatInTransitSupplies(
  supplies: StrategyInTransitSupply[] | null | undefined
): string {
  if (!supplies?.length) return "无";

  return supplies
    .map((s) => {
      const origin = s.originStrongholdName ? `自${s.originStrongholdName}` : "";
      const deceived = s.isDeceived ? "（迷惑中）" : "";
      return `🌾 ${s.cargoFoodGo} 合 · 约${s.estimatedDays}日${origin}${deceived}`;
    })
    .join("；");
}
