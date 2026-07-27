import type {
  StrategyStrongholdCityActorState,
  StrategyStrongholdState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategyTypes";

/** 后端 EconomyFacilityConstants.MarketFacilityTypeId */
export const MARKET_FACILITY_TYPE_ID = 101;

export function strongholdHasMarketFacility(stronghold: StrategyStrongholdState | null | undefined): boolean {
  if (!stronghold) return false;
  return (stronghold.economyFacilities ?? []).some(
    (f) => f.typeId === MARKET_FACILITY_TYPE_ID || f.name === "市场",
  );
}

export function isStrongholdMarketClosed(stronghold: StrategyStrongholdState | null | undefined): boolean {
  if (!stronghold) return true;
  return Boolean(stronghold.siegeThreat);
}

export function isStrongholdMarketOpen(stronghold: StrategyStrongholdState | null | undefined): boolean {
  return strongholdHasMarketFacility(stronghold) && !isStrongholdMarketClosed(stronghold);
}

export function strongholdMerchantActors(
  stronghold: StrategyStrongholdState | null | undefined,
): StrategyStrongholdCityActorState[] {
  if (!stronghold) return [];
  return (stronghold.cityActors ?? []).filter((a) => a.kind === "Merchant");
}

export function isMerchantTradeUnit(unit: StrategyUnitState | null | undefined): boolean {
  if (!unit) return false;
  return (unit.unitKind ?? "Military") === "Merchant";
}

export function merchantUnitsInStronghold(
  worldState: StrategyWorldState,
  strongholdId: number,
  forceId?: number,
): StrategyUnitState[] {
  return worldState.units.filter((u) => {
    if (!isMerchantTradeUnit(u) || !u.inStronghold) return false;
    if (u.locationStrongholdId !== strongholdId) return false;
    if (forceId != null && u.forceId !== forceId) return false;
    return true;
  });
}

export type MarketCommodityTab = "Food" | "Horse";

export {
  resolveMarketCommodityMeta,
  resolveMarketCommodityMetas,
  type MarketCommodityMeta,
} from "@/utils/strategyCommodityHelpers";
