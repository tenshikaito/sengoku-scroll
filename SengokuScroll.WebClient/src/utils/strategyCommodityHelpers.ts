import type { StrategyMasterDataEntry, StrategyMasterDataSnapshot } from "@/api/strategyTypes";
import { GO_PER_KOKU } from "@/utils/strategyDisplayUnits";

export type MarketCommodityTab = "Food" | "Horse";

export interface MarketCommodityMeta {
  id: number;
  key: MarketCommodityTab;
  name: string;
  description: string;
  tradeEnabled: boolean;
  defaultPriceMoneyPerUnit: number;
  unitLabel: string;
  priceUnitLabel: string;
  volumeUnitLabel: string;
  quantityStepLabel: string;
  usesKokuVolume: boolean;
  goPerDisplayUnit: number;
  treasuryIcon: string;
}

const FALLBACK_COMMODITIES: MarketCommodityMeta[] = [
  {
    id: 0,
    key: "Food",
    name: "粮食",
    description: "可大宗撮合的粮秣。",
    tradeEnabled: true,
    defaultPriceMoneyPerUnit: 50,
    unitLabel: "石",
    priceUnitLabel: "贯/石",
    volumeUnitLabel: "石",
    quantityStepLabel: "石",
    usesKokuVolume: true,
    goPerDisplayUnit: GO_PER_KOKU,
    treasuryIcon: "🌾",
  },
  {
    id: 1,
    key: "Horse",
    name: "马匹",
    description: "军用与运输用马；库存为 Actor.Horse。",
    tradeEnabled: true,
    defaultPriceMoneyPerUnit: 120,
    unitLabel: "匹",
    priceUnitLabel: "贯/匹",
    volumeUnitLabel: "匹",
    quantityStepLabel: "匹",
    usesKokuVolume: false,
    goPerDisplayUnit: 1,
    treasuryIcon: "🐎",
  },
];

function readField(entry: StrategyMasterDataEntry, key: string): string {
  const fields = entry.fields ?? {};
  const camel = key.charAt(0).toLowerCase() + key.slice(1);
  return String(fields[camel] ?? fields[key] ?? "").trim();
}

/** 粮食市场 UI 一律用石；兼容旧 master 下发「合」。 */
function resolveDisplayUnitLabel(commodityType: MarketCommodityTab, rawUnitLabel: string, fallback: string): string {
  if (commodityType === "Food") {
    if (!rawUnitLabel || rawUnitLabel === "合")
      return "石";
  }
  return rawUnitLabel || fallback;
}

function mapMasterEntry(entry: StrategyMasterDataEntry): MarketCommodityMeta | null {
  const commodityType = readField(entry, "commodityType") as MarketCommodityTab;
  if (commodityType !== "Food" && commodityType !== "Horse")
    return null;

  const fallback = FALLBACK_COMMODITIES.find((item) => item.key === commodityType)!;
  const unitLabel = resolveDisplayUnitLabel(
    commodityType,
    readField(entry, "unitLabel"),
    fallback.unitLabel,
  );
  const tradeEnabledRaw = readField(entry, "tradeEnabled");
  const defaultPriceRaw = Number.parseInt(readField(entry, "defaultPriceMoneyPerUnit"), 10);

  return {
    id: entry.id,
    key: commodityType,
    name: entry.name?.trim() || fallback.name,
    description: entry.description?.trim() || fallback.description,
    tradeEnabled: tradeEnabledRaw ? tradeEnabledRaw.toLowerCase() === "true" : fallback.tradeEnabled,
    defaultPriceMoneyPerUnit:
      Number.isFinite(defaultPriceRaw) && defaultPriceRaw > 0
        ? defaultPriceRaw
        : fallback.defaultPriceMoneyPerUnit,
    unitLabel,
    priceUnitLabel: `贯/${unitLabel}`,
    volumeUnitLabel: unitLabel,
    quantityStepLabel: unitLabel,
    usesKokuVolume: commodityType === "Food",
    goPerDisplayUnit: commodityType === "Food" ? GO_PER_KOKU : 1,
    treasuryIcon: fallback.treasuryIcon,
  };
}

export function resolveMarketCommodityMetas(
  masterData?: StrategyMasterDataSnapshot | null,
): MarketCommodityMeta[] {
  const fromMaster = (masterData?.commodities ?? [])
    .map(mapMasterEntry)
    .filter((item): item is MarketCommodityMeta => item != null);

  if (fromMaster.length > 0)
    return fromMaster.sort((a, b) => a.id - b.id);

  return [...FALLBACK_COMMODITIES];
}

export function resolveMarketCommodityMeta(
  key: MarketCommodityTab,
  masterData?: StrategyMasterDataSnapshot | null,
): MarketCommodityMeta {
  return (
    resolveMarketCommodityMetas(masterData).find((item) => item.key === key)
    ?? FALLBACK_COMMODITIES.find((item) => item.key === key)!
  );
}

export function formatMarketQuantityFromGo(quantityGo: number, meta: MarketCommodityMeta): string {
  if (quantityGo <= 0) return "0";
  if (meta.usesKokuVolume) {
    const koku = Math.round(quantityGo / meta.goPerDisplayUnit);
    return koku.toLocaleString();
  }

  return quantityGo.toLocaleString();
}

export function resolveTradeQuantityGo(quantityUnits: number, meta: MarketCommodityMeta): number {
  if (quantityUnits <= 0) return 0;
  return Math.max(0, Math.round(quantityUnits * meta.goPerDisplayUnit));
}

export function resolveMaxTradeUnits(
  side: "buy" | "sell",
  meta: MarketCommodityMeta,
  treasuryMoney: number,
  treasuryStock: number,
  limitPrice: number,
): number {
  if (side === "buy") {
    if (limitPrice <= 0) return 0;
    const affordableGo = Math.floor(treasuryMoney / limitPrice);
    return Math.floor(affordableGo / meta.goPerDisplayUnit);
  }

  return Math.floor(treasuryStock / meta.goPerDisplayUnit);
}
