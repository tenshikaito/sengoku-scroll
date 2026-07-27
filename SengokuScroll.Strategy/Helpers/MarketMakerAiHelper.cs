using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 订单簿做市共用算法：以昨收为中枢，买卖各向两侧延伸。
/// 深度约 20 档（随 Actor / 预算浮动，非固定）；单量按距中枢逆平方/平方衰减（非线性）。
/// </summary>
public static class MarketMakerAiHelper
{
    /// <summary>单档分配结果。</summary>
    public readonly record struct LevelAllocation(int PriceMoneyPerGo, int QuantityGo);

    /// <summary>量分布偏态：近中枢集中 vs 远档（耐心买）集中。</summary>
    public enum BookSkew
    {
        NearReference,
        FarReference,
    }

    public static int ResolveReferencePrice(Stronghold stronghold, MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var lastClose = stronghold.Market.ResolveLastClose(commodity);
        if (lastClose > 0)
            return lastClose;

        return CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);
    }

    /// <summary>估值中枢：昨收、默认价、当日 K 开盘较高者；低价砸单后仍用于捡漏判定。</summary>
    public static int ResolveFairReferencePrice(Stronghold stronghold, MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var lastClose = ResolveReferencePrice(stronghold, commodity);
        var defaultPrice = CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);
        var sessionOpen = 0;
        if (stronghold.Market.PriceHistory.Count > 0)
        {
            var bar = stronghold.Market.PriceHistory[^1];
            sessionOpen = bar.Open > 0 ? bar.Open : bar.Close;
        }

        return Math.Max(lastClose, Math.Max(defaultPrice, sessionOpen));
    }

    /// <summary>当前订单簿最低有效卖价；0 表示无卖盘。</summary>
    public static int ResolveBestAsk(Stronghold stronghold, MarketCommodityType commodity)
        => stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == commodity && o.QuantityGo > 0)
            .Select(o => o.PriceMoneyPerGo)
            .DefaultIfEmpty(0)
            .Min();

    public static int ResolveEffectiveBidPrice(int referencePrice, int level, int bestAsk)
    {
        var raw = BidPrice(referencePrice, level);
        return bestAsk > 0 ? Math.Max(raw, bestAsk) : raw;
    }

    public static int AskPrice(int referencePrice, int level)
        => referencePrice + level;

    public static int BidPrice(int referencePrice, int level)
        => Math.Max(1, referencePrice - level);

    /// <summary>非线性档权重（万分点整数，避免浮点）。</summary>
    public static int LevelWeightBp(int level, BookSkew skew)
    {
        if (level <= 0)
            return 0;

        return skew switch
        {
            // 卖盘 / 紧缺买盘：近价量大，~1/L²
            BookSkew.NearReference => 10_000 / (level * level),
            // 耐心买盘：远价量大，~L²（仍比线性陡，但可控）
            BookSkew.FarReference => level * level,
            _ => 10_000 / level,
        };
    }

    /// <summary>Actor 与预算共同决定本次尝试档数（约 8~24，推荐中枢 20）。</summary>
    public static int ResolveMaxDepthLevels(int actorId, int totalBudgetGo, int defaultMinGo)
    {
        var jitter = (actorId % 11) - 5;
        var targetDepth = Math.Clamp(
            MarketConstants.MarketRecommendedDepthLevels + jitter,
            MarketConstants.MarketMinDepthLevels,
            MarketConstants.MarketMaxDepthLevels);

        if (totalBudgetGo <= 0)
            return 1;

        // 规划深度时用更细粒度，避免资金/小余粮被 500 合下限锁死为 3~4 档。
        var planningMinGo = Math.Max(
            1,
            totalBudgetGo / (MarketConstants.MarketRecommendedDepthLevels * 2));
        var budgetDepth = totalBudgetGo / planningMinGo;
        return Math.Clamp(budgetDepth, 1, targetDepth);
    }

    public static int ResolveEffectiveMinOrderGo(int totalBudgetGo, int depthLevels, int defaultMinGo)
    {
        if (totalBudgetGo <= 0 || depthLevels <= 0)
            return defaultMinGo;

        var softMin = totalBudgetGo / Math.Max(1, depthLevels * 3);
        return Math.Clamp(softMin, 1, defaultMinGo);
    }

    public static int QuantityAffordableByMoney(int moneyBudget, int priceMoneyPerGo, int maxQuantityGo)
    {
        if (moneyBudget <= 0 || priceMoneyPerGo <= 0)
            return 0;

        return Math.Min(maxQuantityGo, moneyBudget / priceMoneyPerGo);
    }

    /// <summary>按合数预算生成卖盘或买盘（粮/马等）。</summary>
    public static IReadOnlyList<LevelAllocation> BuildGoAllocations(
        int referencePrice,
        int actorId,
        int totalGo,
        int maxPerLevel,
        int defaultMinGo,
        BookSkew skew,
        bool asksAboveReference,
        int bestAsk = 0)
    {
        if (totalGo <= 0)
            return [];

        var depth = ResolveMaxDepthLevels(actorId, totalGo, defaultMinGo);
        var minQty = ResolveEffectiveMinOrderGo(totalGo, depth, defaultMinGo);
        var weightSum = SumWeights(depth, skew);
        if (weightSum <= 0)
            return [];

        var reserveGo = minQty * depth;
        var useReserve = totalGo >= reserveGo;
        var distributable = useReserve ? totalGo - reserveGo : totalGo;

        var raw = new int[depth];
        for (var level = 1; level <= depth; level++)
        {
            var weighted = weightSum > 0
                ? (int)((long)distributable * LevelWeightBp(level, skew) / weightSum)
                : 0;
            raw[level - 1] = useReserve ? minQty + weighted : weighted;
        }

        var assigned = raw.Sum();
        if (assigned < totalGo && raw.Length > 0)
            raw[0] += Math.Min(maxPerLevel - raw[0], totalGo - assigned);

        var allocations = new List<LevelAllocation>();
        var placedGo = 0;
        for (var level = 1; level <= depth; level++)
        {
            var qty = Math.Min(raw[level - 1], maxPerLevel);
            if (qty < minQty)
                continue;

            var price = asksAboveReference
                ? AskPrice(referencePrice, level)
                : ResolveEffectiveBidPrice(referencePrice, level, bestAsk);

            allocations.Add(new LevelAllocation(price, qty));
            placedGo += qty;
        }

        if (placedGo < totalGo && allocations.Count > 0)
        {
            var head = allocations[0];
            var add = Math.Min(maxPerLevel - head.QuantityGo, totalGo - placedGo);
            if (add > 0)
                allocations[0] = new LevelAllocation(head.PriceMoneyPerGo, head.QuantityGo + add);
        }

        return allocations;
    }

    /// <summary>按资金预算生成买盘（粮）。</summary>
    public static IReadOnlyList<LevelAllocation> BuildMoneyBidAllocations(
        int referencePrice,
        int actorId,
        int moneyBudget,
        int maxPerLevel,
        int defaultMinGo,
        BookSkew skew,
        int bestAsk = 0)
    {
        if (moneyBudget <= 0 || referencePrice <= 0)
            return [];

        var estimatedGo = moneyBudget / Math.Max(1, bestAsk > 0 ? Math.Min(referencePrice, bestAsk) : referencePrice);
        var goAllocations = BuildGoAllocations(
            referencePrice,
            actorId,
            estimatedGo,
            maxPerLevel,
            defaultMinGo,
            skew,
            asksAboveReference: false,
            bestAsk);

        var remainingMoney = moneyBudget;
        var allocations = new List<LevelAllocation>();
        foreach (var level in goAllocations)
        {
            if (remainingMoney <= 0)
                break;

            var qty = Math.Min(
                level.QuantityGo,
                MarketMakerAiHelper.QuantityAffordableByMoney(
                    remainingMoney,
                    level.PriceMoneyPerGo,
                    maxPerLevel));
            if (qty <= 0)
                continue;

            allocations.Add(new LevelAllocation(level.PriceMoneyPerGo, qty));
            remainingMoney -= level.PriceMoneyPerGo * qty;
        }

        return allocations;
    }

    public static void SyncBookSide(
        Stronghold stronghold,
        int actorId,
        string side,
        MarketCommodityType commodity,
        bool taxExempt,
        IReadOnlyList<LevelAllocation> allocations)
    {
        var activePrices = new HashSet<int>();
        foreach (var level in allocations)
        {
            if (level.QuantityGo <= 0 || level.PriceMoneyPerGo <= 0)
                continue;

            MarketActions.SyncAiRestingOrder(
                stronghold,
                actorId,
                side,
                level.PriceMoneyPerGo,
                level.QuantityGo,
                commodity,
                taxExempt);
            activePrices.Add(level.PriceMoneyPerGo);
        }

        MarketActions.PruneAiRestingOrders(stronghold, actorId, side, commodity, activePrices);
    }

    /// <summary>市民口粮紧缺时，买盘偏近中枢。</summary>
    public static bool PreferNearReferenceBids(Stronghold stronghold)
    {
        var daily = LogisticsCalculator.CalculateCivilianDailyFoodConsumption(stronghold.Population);
        if (daily <= 0)
            return false;

        var daysRemaining = stronghold.CivilianActor.Food / daily;
        return daysRemaining < MarketConstants.CivilianBuyOrderThresholdDays;
    }

    private static long SumWeights(int depthLevels, BookSkew skew)
    {
        long sum = 0;
        for (var level = 1; level <= depthLevels; level++)
            sum += LevelWeightBp(level, skew);

        return sum;
    }
}
