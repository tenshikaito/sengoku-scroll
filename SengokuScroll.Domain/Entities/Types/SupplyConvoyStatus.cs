namespace SengokuScroll.Domain.Entities.Types;

/// <summary>运输队在战略地图上的生命周期状态。</summary>
public enum SupplyConvoyStatus
{
    /// <summary>正在据点集结，尚未出发。</summary>
    Forming,

    /// <summary>沿路径向目标单位移动。</summary>
    Moving,

    /// <summary>已抵达目标并完成卸粮。</summary>
    Arrived,

    /// <summary>被敌军消灭或载粮耗尽而失效。</summary>
    Destroyed,

    /// <summary>受假情报迷惑，暂停或改道（见 <see cref="Entities.SupplyConvoy.DeceivedHoldDaysRemaining"/>）。</summary>
    Deceived
}
