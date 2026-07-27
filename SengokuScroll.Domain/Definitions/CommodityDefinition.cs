using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>大宗物资 master 定义（M5+；Id 与 <see cref="MarketCommodityType"/> 对齐）。</summary>
public sealed class CommodityDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public MarketCommodityType CommodityType { get; set; }

    /// <summary>是否可在据点市场大宗撮合。</summary>
    public bool TradeEnabled { get; set; }

    /// <summary>无历史成交时的默认单价（文/单位）。</summary>
    public int DefaultPriceMoneyPerUnit { get; set; }

    /// <summary>显示单位（合/匹等）。</summary>
    public required string UnitLabel { get; set; }
}
