namespace SengokuScroll.Domain.Entities.Types;

/// <summary>信使在途状态。</summary>
public enum MessengerStatus
{
    /// <summary>沿路径向目标移动。</summary>
    Moving,

    /// <summary>已抵达并完成投递。</summary>
    Arrived,

    /// <summary>途中失踪（自然损耗或截获简化结果）。</summary>
    Lost,

    /// <summary>被敌方截获（M4 完整玩法）。</summary>
    Intercepted
}
