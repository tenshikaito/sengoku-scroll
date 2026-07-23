namespace SengokuScroll.Strategy.Constants;

/// <summary>驻城兵种池与专业队维护常数。</summary>
public static class GarrisonConstants
{
    /// <summary>驻城专业队基础维护（文/月/兵，再乘兵种系数）。</summary>
    public const int ProfessionalMaintenanceMoneyPerSoldier = 1;

    public const int CavalryMaintenanceMultiplier = 2;

    public const int MatchlockMaintenanceMultiplier = 2;

    /// <summary>弓兵维护系数（×1.5）。</summary>
    public const int ArcherMaintenanceMultiplierBp = 15_000;
}
