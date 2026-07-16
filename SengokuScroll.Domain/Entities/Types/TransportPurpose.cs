namespace SengokuScroll.Domain.Entities.Types;

/// <summary>运输队任务类型（M4-b）。</summary>
public enum TransportPurpose : byte
{
    /// <summary>军事补给。</summary>
    Supply,

    /// <summary>税赋/贡赋上缴。</summary>
    Tribute,

    /// <summary>跨据点贸易。</summary>
    Trade,

    /// <summary>月度金钱税赋运输。</summary>
    TaxMoney
}
