namespace SengokuScroll.Domain.Entities.Types;

/// <summary>贸易 Unit 任务层策略：轮询行情，满足限价时市价砸单。</summary>
public enum UnitTradePolicy : byte
{
    None = 0,
    /// <summary>在限价以下买入粮食。</summary>
    WaitBuyFood = 1,
    /// <summary>在限价以上卖出粮食。</summary>
    WaitSellFood = 2,
}
