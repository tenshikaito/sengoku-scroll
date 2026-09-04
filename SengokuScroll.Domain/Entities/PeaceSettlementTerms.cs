namespace SengokuScroll.Domain.Entities;

/// <summary>和谈条款；空条款表示白和平并维持当前疆界。</summary>
public sealed class PeaceSettlementTerms
{
    /// <summary>由受约方额外割让给提议方的据点。</summary>
    public List<int> CededStrongholdIds { get; set; } = [];

    /// <summary>受约方向提议方支付的赔款（文）。</summary>
    public int ReparationsMoney { get; set; }

    /// <summary>受约方成为提议方的外藩。</summary>
    public bool DemandOuterVassalage { get; set; }

    public bool IsWhitePeace
        => CededStrongholdIds.Count == 0
           && ReparationsMoney <= 0
           && !DemandOuterVassalage;
}
