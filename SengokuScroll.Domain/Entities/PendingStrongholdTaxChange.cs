namespace SengokuScroll.Domain.Entities;

/// <summary>信使待投递的据点税率变更。</summary>
public sealed class PendingStrongholdTaxChange
{
    public byte? PollTaxRate { get; set; }

    public byte? AgricultureTaxRate { get; set; }

    public byte? CommerceTaxRate { get; set; }

    public byte? TariffTaxRate { get; set; }
}
