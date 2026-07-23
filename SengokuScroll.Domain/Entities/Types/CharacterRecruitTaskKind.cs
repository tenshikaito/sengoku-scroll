namespace SengokuScroll.Domain.Entities.Types;

/// <summary>将领募兵/征兵任务类型。</summary>
public enum CharacterRecruitTaskKind
{
    /// <summary>募兵：消耗资金，按一贯钱换算人数。</summary>
    Mercenary,

    /// <summary>征兵：不耗钱，按比例消耗民心与治安。</summary>
    Conscript,
}
