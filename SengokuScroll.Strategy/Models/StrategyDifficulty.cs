namespace SengokuScroll.Strategy.Models;

/// <summary>
/// 策略模式难度：仅影响情报/迷雾/消息获取方式；不影响战斗成功率等数值。
/// </summary>
public enum StrategyDifficulty : byte
{
    /// <summary>简易：无雾、全情报、即时事件摘要。</summary>
    Easy = 0,

    /// <summary>标准：势力迷雾、模糊情报、信使/Message 权威。</summary>
    Normal = 1,

    /// <summary>困难：角色视野（魔塔式）、模糊情报。</summary>
    Hard = 2,

    /// <summary>自定义：开局选项可逐项配置。</summary>
    Custom = 3
}
