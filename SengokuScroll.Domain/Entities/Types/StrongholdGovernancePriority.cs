namespace SengokuScroll.Domain.Entities.Types;

/// <summary>据点政务方针：影响每月初对城内待命将领的自动任务安排。</summary>
public enum StrongholdGovernancePriority : byte
{
    /// <summary>自由决策：代官/领主据据点状况与个人能力性格评估后自行安排。</summary>
    Autonomous = 0,

    /// <summary>军事优先：向领主以外待命将领发布征兵/募兵任务令。</summary>
    Military = 1,

    /// <summary>内政优先（预留；暂无自动内政任务）。</summary>
    Domestic = 2,
}
